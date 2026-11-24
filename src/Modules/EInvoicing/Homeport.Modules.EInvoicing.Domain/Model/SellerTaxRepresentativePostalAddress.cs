// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The postal address of the tax representative of the seller (BG-12).</summary>
public sealed record SellerTaxRepresentativePostalAddress
{
    /// <summary>Tax representative address line 1 (BT-64).</summary>
    [BusinessTerm("BT-64")]
    public string? AddressLine1 { get; init; }

    /// <summary>Tax representative address line 2 (BT-65).</summary>
    [BusinessTerm("BT-65")]
    public string? AddressLine2 { get; init; }

    /// <summary>Tax representative address line 3 (BT-164).</summary>
    [BusinessTerm("BT-164")]
    public string? AddressLine3 { get; init; }

    /// <summary>Tax representative city (BT-66).</summary>
    [BusinessTerm("BT-66")]
    public string? City { get; init; }

    /// <summary>Tax representative post code (BT-67).</summary>
    [BusinessTerm("BT-67")]
    public string? PostCode { get; init; }

    /// <summary>Tax representative country subdivision (BT-68).</summary>
    [BusinessTerm("BT-68")]
    public string? CountrySubdivision { get; init; }

    /// <summary>Tax representative country code (BT-69).</summary>
    [BusinessTerm("BT-69")]
    public string? CountryCode { get; init; }
}
