// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The period a line covers (BG-26).</summary>
public sealed record InvoiceLinePeriod
{
    /// <summary>Invoice line period start date (BT-134).</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Invoice line period end date (BT-135).</summary>
    public DateOnly? EndDate { get; init; }
}
