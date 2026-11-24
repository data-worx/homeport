// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The contact of the seller (BG-6).</summary>
public sealed record SellerContact
{
    /// <summary>Seller contact point (BT-41).</summary>
    [BusinessTerm("BT-41")]
    public string? ContactPoint { get; init; }

    /// <summary>Seller contact telephone number (BT-42).</summary>
    [BusinessTerm("BT-42")]
    public string? TelephoneNumber { get; init; }

    /// <summary>Seller contact email address (BT-43).</summary>
    [BusinessTerm("BT-43")]
    public string? EmailAddress { get; init; }
}
