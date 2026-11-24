// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The postal address of the buyer (BG-8).</summary>
public sealed record BuyerPostalAddress
{
    /// <summary>Buyer address line 1 (BT-50).</summary>
    [BusinessTerm("BT-50")]
    public string? AddressLine1 { get; init; }

    /// <summary>Buyer address line 2 (BT-51).</summary>
    [BusinessTerm("BT-51")]
    public string? AddressLine2 { get; init; }

    /// <summary>Buyer address line 3 (BT-163).</summary>
    [BusinessTerm("BT-163")]
    public string? AddressLine3 { get; init; }

    /// <summary>Buyer city (BT-52).</summary>
    [BusinessTerm("BT-52")]
    public string? City { get; init; }

    /// <summary>Buyer post code (BT-53).</summary>
    [BusinessTerm("BT-53")]
    public string? PostCode { get; init; }

    /// <summary>Buyer country subdivision (BT-54).</summary>
    [BusinessTerm("BT-54")]
    public string? CountrySubdivision { get; init; }

    /// <summary>Buyer country code (BT-55).</summary>
    [BusinessTerm("BT-55")]
    public string? CountryCode { get; init; }
}
