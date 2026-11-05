// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.Echo.Domain.Batches;

namespace Homeport.Modules.Echo.Domain.Tests.Unit.Batches;

public sealed class BatchRulesTests
{
    public sealed class QuantityOf
    {
        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        public void Counts_One_Per_Item(int itemCount)
        {
            var quantity = BatchRules.QuantityOf(itemCount);

            Assert.Equal(itemCount, quantity.Value);
        }

        [Fact]
        public void Rejects_A_Batch_Without_Items()
        {
            var quantity = BatchRules.QuantityOf(0);

            Assert.Equal(BatchRules.EmptyCode, quantity.Error.Code);
        }
    }
}
