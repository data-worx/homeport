// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The price of the item of a line (BG-29).</summary>
public sealed record PriceDetails
{
    /// <summary>Item net price (BT-146).</summary>
    public string? NetPrice { get; init; }

    /// <summary>Item price discount (BT-147).</summary>
    public string? PriceDiscount { get; init; }

    /// <summary>Item gross price (BT-148).</summary>
    public string? GrossPrice { get; init; }

    /// <summary>Item price base quantity (BT-149).</summary>
    public string? BaseQuantity { get; init; }

    /// <summary>Item price base quantity unit of measure (BT-150).</summary>
    public string? BaseQuantityUnitCode { get; init; }
}
