// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.ModuleContract;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk.Tests.Unit;

public sealed class HomeportUsageTests
{
    public sealed class ReportUsage
    {
        [Fact]
        public void Sets_The_Usage_Header_To_Meter_And_Quantity()
        {
            var response = new DefaultHttpContext().Response;

            response.ReportUsage("items", 5);

            Assert.Equal("items=5", response.Headers[ModuleHeaders.Usage]);
        }

        [Fact]
        public void Replaces_An_Earlier_Report()
        {
            var response = new DefaultHttpContext().Response;
            response.ReportUsage("items", 5);

            response.ReportUsage("items", 3);

            Assert.Equal("items=3", response.Headers[ModuleHeaders.Usage]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Throws_When_The_Quantity_Is_Not_Positive(int quantity)
        {
            var response = new DefaultHttpContext().Response;

            Assert.Throws<ArgumentOutOfRangeException>(() => response.ReportUsage("items", quantity));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("items=")]
        [InlineData("items,documents")]
        [InlineData("line items")]
        public void Throws_When_The_Meter_Cannot_Be_Reported(string meter)
        {
            var response = new DefaultHttpContext().Response;

            Assert.ThrowsAny<ArgumentException>(() => response.ReportUsage(meter, 5));
        }
    }
}
