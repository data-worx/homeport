// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The period the invoice covers (BG-14).</summary>
public sealed record InvoicingPeriod
{
    /// <summary>Invoicing period start date (BT-73).</summary>
    public DateOnly? StartDate { get; init; }

    /// <summary>Invoicing period end date (BT-74).</summary>
    public DateOnly? EndDate { get; init; }
}
