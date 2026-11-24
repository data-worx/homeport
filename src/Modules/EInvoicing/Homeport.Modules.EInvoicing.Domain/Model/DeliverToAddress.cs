// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The address the goods and services were delivered to (BG-15).</summary>
public sealed record DeliverToAddress
{
    /// <summary>Deliver to address line 1 (BT-75).</summary>
    [BusinessTerm("BT-75")]
    public string? AddressLine1 { get; init; }

    /// <summary>Deliver to address line 2 (BT-76).</summary>
    [BusinessTerm("BT-76")]
    public string? AddressLine2 { get; init; }

    /// <summary>Deliver to address line 3 (BT-165).</summary>
    [BusinessTerm("BT-165")]
    public string? AddressLine3 { get; init; }

    /// <summary>Deliver to city (BT-77).</summary>
    [BusinessTerm("BT-77")]
    public string? City { get; init; }

    /// <summary>Deliver to post code (BT-78).</summary>
    [BusinessTerm("BT-78")]
    public string? PostCode { get; init; }

    /// <summary>Deliver to country subdivision (BT-79).</summary>
    [BusinessTerm("BT-79")]
    public string? CountrySubdivision { get; init; }

    /// <summary>Deliver to country code (BT-80).</summary>
    [BusinessTerm("BT-80")]
    public string? CountryCode { get; init; }
}
