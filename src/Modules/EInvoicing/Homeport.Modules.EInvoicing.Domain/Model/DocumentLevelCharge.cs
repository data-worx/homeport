// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A charge on the whole invoice (BG-21).</summary>
public sealed record DocumentLevelCharge
{
    /// <summary>Document level charge amount (BT-99).</summary>
    [BusinessTerm("BT-99")]
    public string? Amount { get; init; }

    /// <summary>Document level charge base amount (BT-100).</summary>
    [BusinessTerm("BT-100")]
    public string? BaseAmount { get; init; }

    /// <summary>Document level charge percentage (BT-101).</summary>
    [BusinessTerm("BT-101")]
    public string? Percentage { get; init; }

    /// <summary>Document level charge VAT category code (BT-102).</summary>
    [BusinessTerm("BT-102")]
    public string? VatCategoryCode { get; init; }

    /// <summary>Document level charge VAT rate (BT-103).</summary>
    [BusinessTerm("BT-103")]
    public string? VatRate { get; init; }

    /// <summary>Document level charge reason (BT-104).</summary>
    [BusinessTerm("BT-104")]
    public string? Reason { get; init; }

    /// <summary>Document level charge reason code (BT-105).</summary>
    [BusinessTerm("BT-105")]
    public string? ReasonCode { get; init; }
}
