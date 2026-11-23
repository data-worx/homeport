// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.RuleSets;

public sealed class RuleSetVersionTests
{
    public sealed class Covers
    {
        private static readonly RuleSetVersion Windowed = new(
            "urn:example", RuleSet.XRechnung, "2.3", new DateOnly(2023, 8, 1), new DateOnly(2024, 1, 31), "XRECHNUNG", "XRechnung");

        [Theory]
        [InlineData(2023, 8, 1)]
        [InlineData(2023, 12, 24)]
        [InlineData(2024, 1, 31)]
        public void Returns_True_For_A_Date_Inside_The_Window_Both_Ends_Included(int year, int month, int day)
        {
            Assert.True(Windowed.Covers(new DateOnly(year, month, day)));
        }

        [Theory]
        [InlineData(2023, 7, 31)]
        [InlineData(2024, 2, 1)]
        public void Returns_False_For_A_Date_Outside_The_Window(int year, int month, int day)
        {
            Assert.False(Windowed.Covers(new DateOnly(year, month, day)));
        }

        [Fact]
        public void Returns_True_For_Any_Date_Without_A_Window()
        {
            var open = Windowed with { ValidFrom = null, ValidUntil = null };

            Assert.True(open.Covers(new DateOnly(1999, 1, 1)));
        }
    }
}
