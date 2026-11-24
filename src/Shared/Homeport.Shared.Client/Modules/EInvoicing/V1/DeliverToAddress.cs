// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The address the goods and services were delivered to (BG-15).</summary>
public sealed record DeliverToAddress
{
    /// <summary>Deliver to address line 1 (BT-75).</summary>
    public string? AddressLine1 { get; init; }

    /// <summary>Deliver to address line 2 (BT-76).</summary>
    public string? AddressLine2 { get; init; }

    /// <summary>Deliver to address line 3 (BT-165).</summary>
    public string? AddressLine3 { get; init; }

    /// <summary>Deliver to city (BT-77).</summary>
    public string? City { get; init; }

    /// <summary>Deliver to post code (BT-78).</summary>
    public string? PostCode { get; init; }

    /// <summary>Deliver to country subdivision (BT-79).</summary>
    public string? CountrySubdivision { get; init; }

    /// <summary>Deliver to country code (BT-80).</summary>
    public string? CountryCode { get; init; }
}
