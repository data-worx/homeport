// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.Echo.Application.Batches;
using Homeport.Modules.Echo.Domain.Batches;

namespace Homeport.Modules.Echo.Application.Tests.Unit.Batches;

public sealed class BatchEchoingTests
{
    public sealed class Echo
    {
        [Fact]
        public void Returns_The_Items_In_Their_Order()
        {
            var batch = new BatchEchoing().Echo<string>(["one", "two", "three"]);

            Assert.Equal(["one", "two", "three"], batch.Value.Items);
        }

        [Fact]
        public void Counts_One_Per_Item()
        {
            var batch = new BatchEchoing().Echo<string>(["one", "two", "three"]);

            Assert.Equal(3, batch.Value.Quantity);
        }

        [Fact]
        public void Fails_For_A_Batch_Without_Items()
        {
            var batch = new BatchEchoing().Echo<string>([]);

            Assert.Equal(BatchRules.EmptyCode, batch.Error.Code);
        }
    }
}
