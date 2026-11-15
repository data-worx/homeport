// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Sets <see cref="RowLevelSecurity.TenantSetting"/> to the
/// <see cref="CurrentTenant"/> on every connection the context opens, so
/// row-level security sees the same tenant as the query filters.
/// </summary>
/// <remarks>
/// Without a tenant the setting is empty, and the policies match no row:
/// reading returns nothing and writing fails.
/// </remarks>
internal sealed class TenantSessionInterceptor(CurrentTenant tenant) : DbConnectionInterceptor
{
    // set_config rather than SET: it takes the value as a parameter.
    private const string SetTenant = "SELECT set_config('" + RowLevelSecurity.TenantSetting + "', @tenant, false)";

    private const string TenantParameter = "tenant";

    /// <summary>Sets the current tenant on the opened connection.</summary>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    /// <summary>Sets the current tenant on the opened connection.</summary>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// The parameterised command that sets the tenant's session setting, empty without a tenant.
    /// </summary>
    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = SetTenant;

        var parameter = command.CreateParameter();
        parameter.ParameterName = TenantParameter;
        parameter.Value = tenant.Id?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);

        return command;
    }
}
