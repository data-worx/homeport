// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Modules.Echo.Domain.Batches;

/// <summary>The rules of a batch Echo returns: what it counts as.</summary>
public static class BatchRules
{
    /// <summary>The batch holds no item, so there is nothing to echo or to count.</summary>
    public const string EmptyCode = "batch.empty";

    /// <summary>How much a batch of <paramref name="itemCount"/> items counts: one per item.</summary>
    /// <param name="itemCount">The number of items in the batch.</param>
    public static Result<int> QuantityOf(int itemCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);

        return itemCount == 0
            ? Result<int>.Failure(new Error(EmptyCode, "A batch holds at least one item."))
            : Result<int>.Success(itemCount);
    }
}
