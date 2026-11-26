// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed class CodeListsTests
{
    public sealed class All
    {
        // The number of codes of each list in the tests of BR-CL of the EN
        // 16931 validation artefacts, tag validation-1.3.17.
        public static TheoryData<string, int> Counts => new()
        {
            { CodeLists.Untdid1001UblInvoice, 50 },
            { CodeLists.Untdid1001UblCreditNote, 13 },
            { CodeLists.Untdid1001Cii, 62 },
            { CodeLists.Iso4217, 178 },
            { CodeLists.Untdid2005Ubl, 3 },
            { CodeLists.Untdid2005Cii, 3 },
            { CodeLists.Untdid1153, 818 },
            { CodeLists.Untdid4451, 401 },
            { CodeLists.Iso6523Icd, 246 },
            { CodeLists.Untdid7143, 185 },
            { CodeLists.Iso3166, 251 },
            { CodeLists.Untdid4461, 84 },
            { CodeLists.Untdid5305, 10 },
            { CodeLists.Untdid5189, 19 },
            { CodeLists.Untdid7161, 178 },
            { CodeLists.Vatex, 106 },
            { CodeLists.UnitsOfMeasure, 2162 },
            { CodeLists.MimeMediaTypes, 6 },
            { CodeLists.Eas, 103 },
        };

        [Theory]
        [MemberData(nameof(Counts))]
        public void Holds_Every_Code_Of_The_Validation_Artefacts(string name, int count) => Assert.Equal(count, CodeLists.Named(name).Codes.Count);

        [Fact]
        public void Holds_No_List_The_Rules_Do_Not_Name() => Assert.Equal(Counts.Count, CodeLists.All.Count);

        [Fact]
        public void Holds_The_Currency_Of_Sao_Tome_Of_Release_1_3_17() =>
            Assert.Equal((true, false), (CodeLists.Named(CodeLists.Iso4217).Contains("STN"), CodeLists.Named(CodeLists.Iso4217).Contains("STD")));
    }

    public sealed class Contains
    {
        private static readonly CodeList List = new("Test", ["AA", "BB"]);

        [Fact]
        public void Finds_A_Code() => Assert.True(List.Contains("AA"));

        [Fact]
        public void Finds_A_Code_Surrounded_By_Whitespace() => Assert.True(List.Contains(" BB\n"));

        [Fact]
        public void Misses_Two_Codes_In_One_Value() => Assert.False(List.Contains("AA BB"));

        [Fact]
        public void Misses_A_Part_Of_A_Code() => Assert.False(List.Contains("A"));

        [Fact]
        public void Misses_A_Blank_Value() => Assert.False(List.Contains(" "));

        [Fact]
        public void Misses_No_Value() => Assert.False(List.Contains(null));
    }

    public sealed class TextContains
    {
        private static readonly CodeList List = new("Test", ["AAA", "BBB"]);

        [Fact]
        public void Finds_Characters_Across_Two_Codes() => Assert.True(List.TextContains("A B"));

        [Fact]
        public void Misses_Characters_The_List_Does_Not_Hold() => Assert.False(List.TextContains("ABC"));
    }
}
