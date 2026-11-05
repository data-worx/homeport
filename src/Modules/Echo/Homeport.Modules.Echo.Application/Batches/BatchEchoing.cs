// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.Echo.Domain.Batches;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.Echo.Application.Batches;

/// <summary>Echo's batch use case: returns the items it is sent, and how much they count.</summary>
public sealed class BatchEchoing
{
    /// <summary>
    /// <paramref name="items"/> unchanged, with the quantity
    /// <see cref="BatchRules"/> counts them as.
    /// </summary>
    /// <typeparam name="TItem">The type of an item; Echo does not look inside it.</typeparam>
    public Result<EchoedBatch<TItem>> Echo<TItem>(IReadOnlyList<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var quantity = BatchRules.QuantityOf(items.Count);

        return quantity.IsSuccess
            ? Result<EchoedBatch<TItem>>.Success(new EchoedBatch<TItem>(items, quantity.Value))
            : Result<EchoedBatch<TItem>>.Failure(quantity.Error);
    }
}

/// <summary>A batch as Echo returns it.</summary>
/// <param name="Items">The items, in the order they were sent.</param>
/// <param name="Quantity">How much the batch counts.</param>
public sealed record EchoedBatch<TItem>(IReadOnlyList<TItem> Items, int Quantity);
