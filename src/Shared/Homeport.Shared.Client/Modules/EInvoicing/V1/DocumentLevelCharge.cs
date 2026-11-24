// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A charge on the whole invoice (BG-21).</summary>
public sealed record DocumentLevelCharge
{
    /// <summary>Document level charge amount (BT-99).</summary>
    public string? Amount { get; init; }

    /// <summary>Document level charge base amount (BT-100).</summary>
    public string? BaseAmount { get; init; }

    /// <summary>Document level charge percentage (BT-101).</summary>
    public string? Percentage { get; init; }

    /// <summary>Document level charge VAT category code (BT-102).</summary>
    public string? VatCategoryCode { get; init; }

    /// <summary>Document level charge VAT rate (BT-103).</summary>
    public string? VatRate { get; init; }

    /// <summary>Document level charge reason (BT-104).</summary>
    public string? Reason { get; init; }

    /// <summary>Document level charge reason code (BT-105).</summary>
    public string? ReasonCode { get; init; }
}
