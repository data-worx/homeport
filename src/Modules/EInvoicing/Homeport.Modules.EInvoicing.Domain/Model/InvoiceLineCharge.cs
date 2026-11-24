// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A charge on a line (BG-28).</summary>
public sealed record InvoiceLineCharge
{
    /// <summary>Invoice line charge amount (BT-141).</summary>
    [BusinessTerm("BT-141")]
    public string? Amount { get; init; }

    /// <summary>Invoice line charge base amount (BT-142).</summary>
    [BusinessTerm("BT-142")]
    public string? BaseAmount { get; init; }

    /// <summary>Invoice line charge percentage (BT-143).</summary>
    [BusinessTerm("BT-143")]
    public string? Percentage { get; init; }

    /// <summary>Invoice line charge reason (BT-144).</summary>
    [BusinessTerm("BT-144")]
    public string? Reason { get; init; }

    /// <summary>Invoice line charge reason code (BT-145).</summary>
    [BusinessTerm("BT-145")]
    public string? ReasonCode { get; init; }
}
