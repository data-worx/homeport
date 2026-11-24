// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The contact of the seller (BG-6).</summary>
public sealed record SellerContact
{
    /// <summary>Seller contact point (BT-41).</summary>
    public string? ContactPoint { get; init; }

    /// <summary>Seller contact telephone number (BT-42).</summary>
    public string? TelephoneNumber { get; init; }

    /// <summary>Seller contact email address (BT-43).</summary>
    public string? EmailAddress { get; init; }
}
