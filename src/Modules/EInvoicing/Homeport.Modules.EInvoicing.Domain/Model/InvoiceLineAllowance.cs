// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>An allowance on a line (BG-27).</summary>
public sealed record InvoiceLineAllowance
{
    /// <summary>Invoice line allowance amount (BT-136).</summary>
    [BusinessTerm("BT-136")]
    public string? Amount { get; init; }

    /// <summary>Invoice line allowance base amount (BT-137).</summary>
    [BusinessTerm("BT-137")]
    public string? BaseAmount { get; init; }

    /// <summary>Invoice line allowance percentage (BT-138).</summary>
    [BusinessTerm("BT-138")]
    public string? Percentage { get; init; }

    /// <summary>Invoice line allowance reason (BT-139).</summary>
    [BusinessTerm("BT-139")]
    public string? Reason { get; init; }

    /// <summary>Invoice line allowance reason code (BT-140).</summary>
    [BusinessTerm("BT-140")]
    public string? ReasonCode { get; init; }
}
