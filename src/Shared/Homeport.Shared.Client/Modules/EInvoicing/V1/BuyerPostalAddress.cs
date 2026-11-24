// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The postal address of the buyer (BG-8).</summary>
public sealed record BuyerPostalAddress
{
    /// <summary>Buyer address line 1 (BT-50).</summary>
    public string? AddressLine1 { get; init; }

    /// <summary>Buyer address line 2 (BT-51).</summary>
    public string? AddressLine2 { get; init; }

    /// <summary>Buyer address line 3 (BT-163).</summary>
    public string? AddressLine3 { get; init; }

    /// <summary>Buyer city (BT-52).</summary>
    public string? City { get; init; }

    /// <summary>Buyer post code (BT-53).</summary>
    public string? PostCode { get; init; }

    /// <summary>Buyer country subdivision (BT-54).</summary>
    public string? CountrySubdivision { get; init; }

    /// <summary>Buyer country code (BT-55).</summary>
    public string? CountryCode { get; init; }
}
