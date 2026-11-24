// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A charge on a line (BG-28).</summary>
public sealed record InvoiceLineCharge
{
    /// <summary>Invoice line charge amount (BT-141).</summary>
    public string? Amount { get; init; }

    /// <summary>Invoice line charge base amount (BT-142).</summary>
    public string? BaseAmount { get; init; }

    /// <summary>Invoice line charge percentage (BT-143).</summary>
    public string? Percentage { get; init; }

    /// <summary>Invoice line charge reason (BT-144).</summary>
    public string? Reason { get; init; }

    /// <summary>Invoice line charge reason code (BT-145).</summary>
    public string? ReasonCode { get; init; }
}
