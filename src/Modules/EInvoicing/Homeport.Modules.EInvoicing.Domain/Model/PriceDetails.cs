// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The price of the item of a line (BG-29).</summary>
public sealed record PriceDetails
{
    /// <summary>Item net price (BT-146).</summary>
    [BusinessTerm("BT-146")]
    public string? NetPrice { get; init; }

    /// <summary>Item price discount (BT-147).</summary>
    [BusinessTerm("BT-147")]
    public string? PriceDiscount { get; init; }

    /// <summary>Item gross price (BT-148).</summary>
    [BusinessTerm("BT-148")]
    public string? GrossPrice { get; init; }

    /// <summary>Item price base quantity (BT-149).</summary>
    [BusinessTerm("BT-149")]
    public string? BaseQuantity { get; init; }

    /// <summary>Item price base quantity unit of measure (BT-150).</summary>
    [BusinessTerm("BT-150")]
    public string? BaseQuantityUnitCode { get; init; }
}
