// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The item a line invoices (BG-31).</summary>
public sealed record ItemInformation
{
    /// <summary>Item name (BT-153).</summary>
    public string? Name { get; init; }

    /// <summary>Item description (BT-154).</summary>
    public string? Description { get; init; }

    /// <summary>Item Sellers identifier (BT-155).</summary>
    public Identifier? SellersIdentifier { get; init; }

    /// <summary>Item Buyers identifier (BT-156).</summary>
    public Identifier? BuyersIdentifier { get; init; }

    /// <summary>Item standard identifier (BT-157).</summary>
    public Identifier? StandardIdentifier { get; init; }

    /// <summary>Every item classification identifier (BT-158); empty when there is none.</summary>
    public IReadOnlyList<Identifier> ClassificationIdentifiers { get; init; } = [];

    /// <summary>Item country of origin (BT-159).</summary>
    public string? CountryOfOrigin { get; init; }

    /// <summary>Every item attributes (BG-32); empty when there is none.</summary>
    public IReadOnlyList<ItemAttribute> Attributes { get; init; } = [];
}
