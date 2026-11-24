// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The item a line invoices (BG-31).</summary>
public sealed record ItemInformation
{
    /// <summary>Item name (BT-153).</summary>
    [BusinessTerm("BT-153")]
    public string? Name { get; init; }

    /// <summary>Item description (BT-154).</summary>
    [BusinessTerm("BT-154")]
    public string? Description { get; init; }

    /// <summary>Item Sellers identifier (BT-155).</summary>
    [BusinessTerm("BT-155")]
    public Identifier? SellersIdentifier { get; init; }

    /// <summary>Item Buyers identifier (BT-156).</summary>
    [BusinessTerm("BT-156")]
    public Identifier? BuyersIdentifier { get; init; }

    /// <summary>Item standard identifier (BT-157).</summary>
    [BusinessTerm("BT-157")]
    public Identifier? StandardIdentifier { get; init; }

    /// <summary>Every item classification identifier (BT-158); empty when there is none.</summary>
    [BusinessTerm("BT-158")]
    public IReadOnlyList<Identifier> ClassificationIdentifiers { get; init; } = [];

    /// <summary>Item country of origin (BT-159).</summary>
    [BusinessTerm("BT-159")]
    public string? CountryOfOrigin { get; init; }

    /// <summary>Every item attributes (BG-32); empty when there is none.</summary>
    [BusinessTerm("BG-32")]
    public IReadOnlyList<ItemAttribute> Attributes { get; init; } = [];
}
