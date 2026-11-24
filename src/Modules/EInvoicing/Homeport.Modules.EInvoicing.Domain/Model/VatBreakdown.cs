// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The VAT of one category and rate (BG-23).</summary>
public sealed record VatBreakdown
{
    /// <summary>VAT category taxable amount (BT-116).</summary>
    [BusinessTerm("BT-116")]
    public string? TaxableAmount { get; init; }

    /// <summary>VAT category tax amount (BT-117).</summary>
    [BusinessTerm("BT-117")]
    public string? TaxAmount { get; init; }

    /// <summary>VAT category code (BT-118).</summary>
    [BusinessTerm("BT-118")]
    public string? CategoryCode { get; init; }

    /// <summary>VAT category rate (BT-119).</summary>
    [BusinessTerm("BT-119")]
    public string? Rate { get; init; }

    /// <summary>VAT exemption reason text (BT-120).</summary>
    [BusinessTerm("BT-120")]
    public string? ExemptionReason { get; init; }

    /// <summary>VAT exemption reason code (BT-121).</summary>
    [BusinessTerm("BT-121")]
    public string? ExemptionReasonCode { get; init; }
}
