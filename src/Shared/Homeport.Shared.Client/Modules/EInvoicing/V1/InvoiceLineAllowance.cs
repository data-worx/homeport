// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An allowance on a line (BG-27).</summary>
public sealed record InvoiceLineAllowance
{
    /// <summary>Invoice line allowance amount (BT-136).</summary>
    public string? Amount { get; init; }

    /// <summary>Invoice line allowance base amount (BT-137).</summary>
    public string? BaseAmount { get; init; }

    /// <summary>Invoice line allowance percentage (BT-138).</summary>
    public string? Percentage { get; init; }

    /// <summary>Invoice line allowance reason (BT-139).</summary>
    public string? Reason { get; init; }

    /// <summary>Invoice line allowance reason code (BT-140).</summary>
    public string? ReasonCode { get; init; }
}
