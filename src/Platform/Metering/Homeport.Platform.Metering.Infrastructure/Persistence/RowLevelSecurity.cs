// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Migrations;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// The row-level security of the tenant-owned tables (ADR-0005): the one
/// place metering writes PostgreSQL-specific SQL.
/// </summary>
/// <remarks>
/// The tenant policies bind the app role, not the owner role that runs the
/// migrations: PostgreSQL exempts a table's owner unless it is forced. The
/// admin role reads every tenant's rows of a table through a policy of its
/// own, never through <c>BYPASSRLS</c> (ADR-0020).
/// </remarks>
internal static class RowLevelSecurity
{
    /// <summary>The session setting that names the tenant a connection works for.</summary>
    internal const string TenantSetting = "homeport.tenant_id";

    /// <summary>The admin role the init script creates for metering's schema.</summary>
    internal const string AdminRole = MeteringDbContext.Schema + "_admin";

    private const string PolicyName = "tenant_isolation";

    private const string AdminPolicyName = "admin_read";

    // An empty or missing setting is no tenant: NULL, which no row matches.
    private const string TenantMatches =
        "\"TenantId\" = NULLIF(current_setting('" + TenantSetting + "', true), '')::uuid";

    /// <summary>Enables row-level security on <paramref name="table"/> and limits reads and writes to the session's tenant.</summary>
    internal static void Enable(MigrationBuilder migrationBuilder, string table)
    {
        var name = QualifiedName(table);

        migrationBuilder.Sql($"ALTER TABLE {name} ENABLE ROW LEVEL SECURITY;");
        migrationBuilder.Sql($"CREATE POLICY {PolicyName} ON {name} USING ({TenantMatches}) WITH CHECK ({TenantMatches});");
    }

    /// <summary>Removes what <see cref="Enable"/> created.</summary>
    internal static void Disable(MigrationBuilder migrationBuilder, string table)
    {
        var name = QualifiedName(table);

        migrationBuilder.Sql($"DROP POLICY {PolicyName} ON {name};");
        migrationBuilder.Sql($"ALTER TABLE {name} DISABLE ROW LEVEL SECURITY;");
    }

    /// <summary>
    /// Lets <see cref="AdminRole"/> read every row of <paramref name="table"/>,
    /// and nothing else: it gets <c>SELECT</c> and a policy for reading only.
    /// </summary>
    internal static void EnableAdminRead(MigrationBuilder migrationBuilder, string table)
    {
        var name = QualifiedName(table);

        migrationBuilder.Sql($"GRANT SELECT ON {name} TO {AdminRole};");
        migrationBuilder.Sql($"CREATE POLICY {AdminPolicyName} ON {name} FOR SELECT TO {AdminRole} USING (true);");
    }

    /// <summary>Removes what <see cref="EnableAdminRead"/> created.</summary>
    internal static void DisableAdminRead(MigrationBuilder migrationBuilder, string table)
    {
        var name = QualifiedName(table);

        migrationBuilder.Sql($"DROP POLICY {AdminPolicyName} ON {name};");
        migrationBuilder.Sql($"REVOKE SELECT ON {name} FROM {AdminRole};");
    }

    /// <summary>The table's name, qualified with metering's schema.</summary>
    internal static string QualifiedName(string table) => $"{MeteringDbContext.Schema}.\"{table}\"";
}
