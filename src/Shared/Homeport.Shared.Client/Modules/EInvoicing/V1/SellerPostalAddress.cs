// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The postal address of the seller (BG-5).</summary>
public sealed record SellerPostalAddress
{
    /// <summary>Seller address line 1 (BT-35).</summary>
    public string? AddressLine1 { get; init; }

    /// <summary>Seller address line 2 (BT-36).</summary>
    public string? AddressLine2 { get; init; }

    /// <summary>Seller address line 3 (BT-162).</summary>
    public string? AddressLine3 { get; init; }

    /// <summary>Seller city (BT-37).</summary>
    public string? City { get; init; }

    /// <summary>Seller post code (BT-38).</summary>
    public string? PostCode { get; init; }

    /// <summary>Seller country subdivision (BT-39).</summary>
    public string? CountrySubdivision { get; init; }

    /// <summary>Seller country code (BT-40).</summary>
    public string? CountryCode { get; init; }
}
