// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Metering.V1;

namespace Homeport.Platform.Portal.Components.Pages.Usage;

/// <summary>
/// The usage of a period as the Usage page shows it: one row per tenant, one
/// column per module and meter, each cell summed over the period, and a row
/// of totals. Meters count different units, so a row has no total.
/// </summary>
/// <param name="Columns">The modules and meters with usage, ordered by module, then meter.</param>
/// <param name="Rows">The tenants with usage, ordered by name.</param>
/// <param name="Totals">The total of each column, in the order of <paramref name="Columns"/>.</param>
internal sealed record UsageTable(IReadOnlyList<UsageColumn> Columns, IReadOnlyList<UsageRow> Rows, IReadOnlyList<long> Totals)
{
    /// <summary>
    /// The table of <paramref name="usage"/>; a tenant <paramref name="tenants"/>
    /// does not name, such as one deleted since, is shown by its id.
    /// </summary>
    internal static UsageTable Of(AdminUsageResponse usage, IReadOnlyList<TenantResponse> tenants)
    {
        var columns = usage.Usage
            .Select(entry => new UsageColumn(entry.Module, entry.Meter))
            .Distinct()
            .OrderBy(column => column.Module, StringComparer.Ordinal)
            .ThenBy(column => column.Meter, StringComparer.Ordinal)
            .ToList();
        var names = tenants.ToDictionary(tenant => tenant.Id, tenant => tenant.Name);

        var rows = usage.Usage
            .GroupBy(entry => entry.Tenant)
            .Select(entries => new UsageRow(
                entries.Key,
                names.GetValueOrDefault(entries.Key, entries.Key.ToString()),
                columns.Select(column => entries
                    .Where(entry => entry.Module == column.Module && entry.Meter == column.Meter)
                    .Sum(entry => entry.Quantity)).ToList()))
            .OrderBy(row => row.TenantName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.TenantId)
            .ToList();

        var totals = columns.Select((_, index) => rows.Sum(row => row.Quantities[index])).ToList();
        return new UsageTable(columns, rows, totals);
    }
}

/// <summary>One column of the <see cref="UsageTable"/>: a meter of a module.</summary>
/// <param name="Module">The module id.</param>
/// <param name="Meter">The meter.</param>
internal sealed record UsageColumn(string Module, string Meter);

/// <summary>One row of the <see cref="UsageTable"/>: a tenant's usage.</summary>
/// <param name="TenantId">The tenant's id.</param>
/// <param name="TenantName">The tenant's name, or its id where the tenant is unknown.</param>
/// <param name="Quantities">The quantity of each column, in the order of the table's columns.</param>
internal sealed record UsageRow(Guid TenantId, string TenantName, IReadOnlyList<long> Quantities);
