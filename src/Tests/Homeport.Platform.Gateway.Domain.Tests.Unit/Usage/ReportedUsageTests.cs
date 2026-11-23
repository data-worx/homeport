// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.Usage;

namespace Homeport.Platform.Gateway.Domain.Tests.Unit.Usage;

public sealed class ReportedUsageTests
{
    public sealed class QuantityOf
    {
        [Fact]
        public void Returns_The_Quantity_Reported_For_The_Route_Meter()
        {
            var quantity = ReportedUsage.QuantityOf("items=5", "items");

            Assert.Equal(5, quantity.Value);
        }

        [Theory]
        [InlineData("items=0")]
        [InlineData("items=00")]
        public void Returns_Zero_When_The_Module_Reports_That_The_Request_Counts_Nothing(string report)
        {
            var quantity = ReportedUsage.QuantityOf(report, "items");

            Assert.Equal(0, quantity.Value);
        }

        [Fact]
        public void Rejects_A_Meter_The_Route_Does_Not_Declare()
        {
            var quantity = ReportedUsage.QuantityOf("documents=5", "items");

            Assert.Equal(ReportedUsage.UndeclaredMeterCode, quantity.Error.Code);
        }

        [Fact]
        public void Rejects_A_Meter_That_Differs_Only_In_Case()
        {
            var quantity = ReportedUsage.QuantityOf("Items=5", "items");

            Assert.Equal(ReportedUsage.UndeclaredMeterCode, quantity.Error.Code);
        }

        [Theory]
        [InlineData("items=-5")]
        [InlineData("items=-0")]
        [InlineData("items=+0")]
        [InlineData("items=+5")]
        [InlineData("items=five")]
        [InlineData("items=2.5")]
        [InlineData("items= 5")]
        [InlineData("items=")]
        [InlineData("items=2147483648")]
        public void Rejects_A_Quantity_That_Is_Not_A_Non_Negative_Integer(string report)
        {
            var quantity = ReportedUsage.QuantityOf(report, "items");

            Assert.Equal(ReportedUsage.InvalidQuantityCode, quantity.Error.Code);
        }

        [Theory]
        [InlineData("items")]
        [InlineData("=5")]
        [InlineData("")]
        [InlineData("items=5,items=3")]
        public void Rejects_A_Report_That_Is_Not_One_Pair(string report)
        {
            var quantity = ReportedUsage.QuantityOf(report, "items");

            Assert.Equal(ReportedUsage.MalformedCode, quantity.Error.Code);
        }
    }
}
