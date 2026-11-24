// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The postal address of the seller (BG-5).</summary>
public sealed record SellerPostalAddress
{
    /// <summary>Seller address line 1 (BT-35).</summary>
    [BusinessTerm("BT-35")]
    public string? AddressLine1 { get; init; }

    /// <summary>Seller address line 2 (BT-36).</summary>
    [BusinessTerm("BT-36")]
    public string? AddressLine2 { get; init; }

    /// <summary>Seller address line 3 (BT-162).</summary>
    [BusinessTerm("BT-162")]
    public string? AddressLine3 { get; init; }

    /// <summary>Seller city (BT-37).</summary>
    [BusinessTerm("BT-37")]
    public string? City { get; init; }

    /// <summary>Seller post code (BT-38).</summary>
    [BusinessTerm("BT-38")]
    public string? PostCode { get; init; }

    /// <summary>Seller country subdivision (BT-39).</summary>
    [BusinessTerm("BT-39")]
    public string? CountrySubdivision { get; init; }

    /// <summary>Seller country code (BT-40).</summary>
    [BusinessTerm("BT-40")]
    public string? CountryCode { get; init; }
}
