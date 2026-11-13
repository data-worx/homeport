// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore.Migrations;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>
/// Keeps the audit entries append-only for the service (ADR-0020): the one
/// place the control plane writes PostgreSQL-specific SQL.
/// </summary>
/// <remarks>
/// The default privileges of <c>deploy/postgres/init/</c> give the app role
/// <c>SELECT</c>, <c>INSERT</c>, <c>UPDATE</c> and <c>DELETE</c> on every
/// table the owner role creates. The migration that creates the audit table
/// takes the last two back, as the owner role it runs as.
/// </remarks>
internal static class AuditPrivileges
{
    /// <summary>The app role the init script creates for the control plane's schema.</summary>
    internal const string AppRole = ControlPlaneDbContext.Schema + "_app";

    /// <summary>Leaves the app role <c>SELECT</c> and <c>INSERT</c> on <paramref name="table"/>, nothing else.</summary>
    internal static void Restrict(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql($"REVOKE UPDATE, DELETE ON {QualifiedName(table)} FROM {AppRole};");
    }

    private static string QualifiedName(string table) => $"{ControlPlaneDbContext.Schema}.\"{table}\"";
}
