// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The VAT of one category and rate (BG-23).</summary>
public sealed record VatBreakdown
{
    /// <summary>VAT category taxable amount (BT-116).</summary>
    public string? TaxableAmount { get; init; }

    /// <summary>VAT category tax amount (BT-117).</summary>
    public string? TaxAmount { get; init; }

    /// <summary>VAT category code (BT-118).</summary>
    public string? CategoryCode { get; init; }

    /// <summary>VAT category rate (BT-119).</summary>
    public string? Rate { get; init; }

    /// <summary>VAT exemption reason text (BT-120).</summary>
    public string? ExemptionReason { get; init; }

    /// <summary>VAT exemption reason code (BT-121).</summary>
    public string? ExemptionReasonCode { get; init; }
}
