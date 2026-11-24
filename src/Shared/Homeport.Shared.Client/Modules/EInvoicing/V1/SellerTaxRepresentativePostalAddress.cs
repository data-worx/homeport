// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The postal address of the tax representative of the seller (BG-12).</summary>
public sealed record SellerTaxRepresentativePostalAddress
{
    /// <summary>Tax representative address line 1 (BT-64).</summary>
    public string? AddressLine1 { get; init; }

    /// <summary>Tax representative address line 2 (BT-65).</summary>
    public string? AddressLine2 { get; init; }

    /// <summary>Tax representative address line 3 (BT-164).</summary>
    public string? AddressLine3 { get; init; }

    /// <summary>Tax representative city (BT-66).</summary>
    public string? City { get; init; }

    /// <summary>Tax representative post code (BT-67).</summary>
    public string? PostCode { get; init; }

    /// <summary>Tax representative country subdivision (BT-68).</summary>
    public string? CountrySubdivision { get; init; }

    /// <summary>Tax representative country code (BT-69).</summary>
    public string? CountryCode { get; init; }
}
