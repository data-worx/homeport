// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Migrations;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Keeps the audit entries append-only (ADR-0020): next to
/// <see cref="RowLevelSecurity"/> the one place metering writes
/// PostgreSQL-specific SQL.
/// </summary>
/// <remarks>
/// The default privileges of <c>deploy/postgres/init/</c> give the app role
/// <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c> and <c>DELETE</c> on every
/// table the owner role creates, and the admin role nothing. The migration
/// that creates the audit table takes the app role's last two back and gives
/// the admin role the first two, as the owner role it runs as.
/// </remarks>
internal static class AuditPrivileges
{
    /// <summary>The app role the init script creates for metering's schema.</summary>
    internal const string AppRole = MeteringDbContext.Schema + "_app";

    /// <summary>
    /// Leaves the app role and the admin role <c>SELECT</c> and <c>INSERT</c>
    /// on <paramref name="table"/>, nothing else.
    /// </summary>
    internal static void Restrict(MigrationBuilder migrationBuilder, string table)
    {
        var name = RowLevelSecurity.QualifiedName(table);

        migrationBuilder.Sql($"REVOKE UPDATE, DELETE ON {name} FROM {AppRole};");
        migrationBuilder.Sql($"GRANT SELECT, INSERT ON {name} TO {RowLevelSecurity.AdminRole};");
    }
}
