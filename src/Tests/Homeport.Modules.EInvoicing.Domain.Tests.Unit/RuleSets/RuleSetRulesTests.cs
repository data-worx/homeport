// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.RuleSets;

public sealed class RuleSetRulesTests
{
    private const string XRechnung30 = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0";
    private const string XRechnung23 = "urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3";
    private const string Minimum = "urn:factur-x.eu:1p0:minimum";

    private static readonly RuleSetCatalog Catalog = RuleSetCatalog.LoadEmbedded();

    public sealed class Identify
    {
        [Fact]
        public void Returns_The_Version_The_Identifier_Names_Without_Findings_Inside_Its_Window()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung30, new DateOnly(2026, 11, 23));

            Assert.Equal((RuleSet.XRechnung, "3.0"), (match.Value.RuleSet!.RuleSet, match.Value.RuleSet.Version));
            Assert.Empty(match.Value.Findings);
        }

        [Fact]
        public void Warns_When_The_Issue_Date_Lies_After_The_Window()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung23, new DateOnly(2024, 2, 1));

            Assert.Equal([RuleSetRules.WindowRule], match.Value.Findings.Select(finding => finding.Rule));
        }

        [Fact]
        public void Warns_When_The_Issue_Date_Lies_Before_The_Window()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung30, new DateOnly(2016, 4, 4));

            Assert.Equal(
                new Finding(
                    FindingSeverity.Warning,
                    RuleSetRules.WindowRule,
                    "The invoice was issued on 2016-04-04, outside the window of version 3.0 (valid from 2023-08-01)."),
                Assert.Single(match.Value.Findings));
        }

        [Fact]
        public void Checks_No_Window_Without_An_Issue_Date()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung23, issueDate: null);

            Assert.Empty(match.Value.Findings);
        }

        [Fact]
        public void Returns_No_Rule_Set_For_A_Specification_That_Is_No_E_Invoice()
        {
            var match = RuleSetRules.Identify(Catalog, Minimum, new DateOnly(2026, 11, 23));

            Assert.True(match.Value.IsNotAnEInvoice);
            Assert.Equal("MINIMUM", match.Value.ExpectedProfile);
        }

        [Fact]
        public void Checks_An_Unknown_Specification_Of_En16931_Against_En16931_Alone_With_A_Warning()
        {
            const string Unknown = XRechnung30 + "#compliant#urn:xeinkauf.de:kosit:xrechnung:cvd_0.9";

            var match = RuleSetRules.Identify(Catalog, Unknown, new DateOnly(2026, 11, 23));

            Assert.Equal(RuleSet.En16931, match.Value.RuleSet!.RuleSet);
            Assert.Equal([RuleSetRules.UnknownSpecificationRule], match.Value.Findings.Select(finding => finding.Rule));
        }

        [Fact]
        public void Expects_No_Profile_For_An_Unknown_Specification_Of_En16931()
        {
            var match = RuleSetRules.Identify(Catalog, RuleSetRules.En16931Identifier + "#compliant#urn:example", issueDate: null);

            Assert.Null(match.Value.ExpectedProfile);
        }

        [Theory]
        [InlineData("urn:fdc:peppol.eu:2017:poacc:billing:01:1.0")]
        [InlineData("urn:cen.eu:EN16931:2017")]
        [InlineData("")]
        [InlineData(null)]
        public void Returns_Unknown_Rule_Set_For_Any_Other_Identifier(string? customizationId)
        {
            var match = RuleSetRules.Identify(Catalog, customizationId, issueDate: null);

            Assert.Equal(RuleSetRules.UnknownRuleSetCode, match.Error.Code);
        }
    }

    public sealed class CheckProfile
    {
        [Fact]
        public void Returns_Nothing_When_The_Declared_Profile_Matches()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung30, issueDate: null).Value;

            Assert.Null(RuleSetRules.CheckProfile(match, "XRECHNUNG"));
        }

        [Fact]
        public void Ignores_Spaces_In_The_Declared_Profile()
        {
            var match = RuleSetRules.Identify(Catalog, RuleSetRules.En16931Identifier, issueDate: null).Value;

            Assert.Null(RuleSetRules.CheckProfile(match, "EN16931"));
        }

        [Fact]
        public void Warns_When_The_Declared_Profile_Differs()
        {
            var match = RuleSetRules.Identify(Catalog, XRechnung30, issueDate: null).Value;

            Assert.Equal(RuleSetRules.ProfileRule, RuleSetRules.CheckProfile(match, "EN 16931")?.Rule);
        }

        [Fact]
        public void Warns_When_A_Pdf_Declares_A_Profile_For_A_Document_That_Is_No_E_Invoice()
        {
            var match = RuleSetRules.Identify(Catalog, Minimum, issueDate: null).Value;

            Assert.Equal(RuleSetRules.ProfileRule, RuleSetRules.CheckProfile(match, "BASIC")?.Rule);
        }

        [Fact]
        public void Returns_Nothing_When_No_Profile_Is_Expected()
        {
            var match = RuleSetRules.Identify(Catalog, RuleSetRules.En16931Identifier + "#compliant#urn:example", issueDate: null).Value;

            Assert.Null(RuleSetRules.CheckProfile(match, "XRECHNUNG"));
        }
    }

    public sealed class CheckExtension
    {
        [Theory]
        [InlineData(XRechnung30 + "#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0")]
        [InlineData("urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended")]
        public void Warns_That_The_Extension_Of_A_Rule_Set_That_Extends_En_16931_Was_Ignored(string customizationId)
        {
            var match = RuleSetRules.Identify(Catalog, customizationId, issueDate: null).Value;

            Assert.Equal(RuleSetRules.ExtensionIgnoredRule, RuleSetRules.CheckExtension(match)?.Rule);
        }

        [Theory]
        [InlineData(XRechnung30)]
        [InlineData(RuleSetRules.En16931Identifier)]
        [InlineData(Minimum)]
        public void Returns_Nothing_For_A_Rule_Set_Without_An_Extension(string customizationId)
        {
            var match = RuleSetRules.Identify(Catalog, customizationId, issueDate: null).Value;

            Assert.Null(RuleSetRules.CheckExtension(match));
        }
    }
}
