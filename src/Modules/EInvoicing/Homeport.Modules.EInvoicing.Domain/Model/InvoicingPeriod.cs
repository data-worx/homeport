// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The period the invoice covers (BG-14).</summary>
public sealed record InvoicingPeriod
{
    /// <summary>Invoicing period start date (BT-73).</summary>
    [BusinessTerm("BT-73")]
    public DateOnly? StartDate { get; init; }

    /// <summary>Invoicing period end date (BT-74).</summary>
    [BusinessTerm("BT-74")]
    public DateOnly? EndDate { get; init; }
}
