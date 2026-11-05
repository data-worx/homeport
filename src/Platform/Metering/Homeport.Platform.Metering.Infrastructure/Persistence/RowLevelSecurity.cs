// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Migrations;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// The row-level security of the tenant-owned tables (ADR-0005): the one
/// place metering writes PostgreSQL-specific SQL.
/// </summary>
/// <remarks>
/// The policies bind the app role, not the owner role that runs the
/// migrations: PostgreSQL exempts a table's owner unless it is forced.
/// </remarks>
internal static class RowLevelSecurity
{
    /// <summary>The session setting that names the tenant a connection works for.</summary>
    internal const string TenantSetting = "homeport.tenant_id";

    private const string PolicyName = "tenant_isolation";

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

    private static string QualifiedName(string table) => $"{MeteringDbContext.Schema}.\"{table}\"";
}
