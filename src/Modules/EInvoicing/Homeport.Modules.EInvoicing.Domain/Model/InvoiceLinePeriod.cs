// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The period a line covers (BG-26).</summary>
public sealed record InvoiceLinePeriod
{
    /// <summary>Invoice line period start date (BT-134).</summary>
    [BusinessTerm("BT-134")]
    public DateOnly? StartDate { get; init; }

    /// <summary>Invoice line period end date (BT-135).</summary>
    [BusinessTerm("BT-135")]
    public DateOnly? EndDate { get; init; }
}
