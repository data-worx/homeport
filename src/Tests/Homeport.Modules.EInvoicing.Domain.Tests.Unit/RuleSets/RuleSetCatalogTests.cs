// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Modules.EInvoicing.Domain.RuleSets;
using Homeport.Testing;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.RuleSets;

public sealed class RuleSetCatalogTests
{
    public sealed class LoadEmbedded
    {
        private readonly RuleSetCatalog catalog = RuleSetCatalog.LoadEmbedded();

        [Theory]
        [InlineData("urn:cen.eu:en16931:2017", RuleSet.En16931, "2017")]
        [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0", RuleSet.XRechnung, "3.0")]
        [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3", RuleSet.XRechnung, "2.3")]
        [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0", RuleSet.XRechnungExtension, "3.0")]
        [InlineData("urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3#conformant#urn:xoev-de:kosit:extension:xrechnung_2.3", RuleSet.XRechnungExtension, "2.3")]
        [InlineData("urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic", RuleSet.FacturXBasic, "1.0")]
        [InlineData("urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended", RuleSet.FacturXExtended, "1.0")]
        public void Lists_Each_Rule_Set_Of_M3_By_Its_Specification_Identifier(string customizationId, RuleSet ruleSet, string version)
        {
            Assert.True(catalog.TryGetRuleSet(customizationId, out var found));
            Assert.Equal((ruleSet, version), (found.RuleSet, found.Version));
        }

        [Theory]
        [InlineData("urn:factur-x.eu:1p0:minimum", "MINIMUM")]
        [InlineData("urn:factur-x.eu:1p0:basicwl", "BASIC WL")]
        public void Lists_Minimum_And_Basic_Wl_As_No_E_Invoice(string customizationId, string profile)
        {
            Assert.True(catalog.TryGetNotAnEInvoice(customizationId, out var found));
            Assert.Equal(profile, found.Profile);
        }

        [Fact]
        public void Keeps_The_Window_Of_Xrechnung_2_3_As_Its_Source_States_It()
        {
            catalog.TryGetRuleSet("urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3", out var version);

            Assert.Equal((new DateOnly(2023, 8, 1), new DateOnly(2024, 1, 31)), (version!.ValidFrom, version.ValidUntil));
        }

        [Fact]
        public void Names_Only_Standards_The_Module_Declares()
        {
            var declared = Standards().Select(standard => standard.Name).ToHashSet();

            Assert.All(StandardsOfCatalog(), name => Assert.Contains(name, declared));
        }

        [Fact]
        public void Agrees_With_The_Standards_File_On_The_Window_Of_Each_Version_It_Declares()
        {
            var declared = Standards();

            var disagreeing = catalog.RuleSets
                .SelectMany(version => declared
                    .Where(standard => standard.Name == version.Standard && standard.Version == version.Version)
                    .Where(standard => (standard.ValidFrom, standard.ValidUntil) != (version.ValidFrom, version.ValidUntil))
                    .Select(_ => version.CustomizationId))
                .ToList();

            Assert.Empty(disagreeing);
        }

        [Fact]
        public void Declares_The_Window_Of_The_Current_Xrechnung_Version_In_The_Standards_File()
        {
            Assert.Contains(Standards(), standard => standard is { Name: "XRechnung", Version: "3.0" });
        }

        private static List<Standard> Standards()
        {
            var path = Path.Combine(RepositoryRoot.Find(), "src", "Modules", "EInvoicing", "standards.json");
            var file = JsonSerializer.Deserialize<StandardsFile>(File.ReadAllText(path), JsonSerializerOptions.Web);

            return file!.Standards;
        }

        private IEnumerable<string> StandardsOfCatalog() =>
            catalog.RuleSets.Select(version => version.Standard).Concat(catalog.NotAnEInvoice.Select(specification => specification.Standard)).Distinct();

        private sealed record StandardsFile(List<Standard> Standards);

        private sealed record Standard(string Name, string Version, DateOnly? ValidFrom, DateOnly? ValidUntil);
    }

    public sealed class Constructor
    {
        [Fact]
        public void Throws_When_An_Identifier_Is_Both_A_Rule_Set_And_No_E_Invoice()
        {
            const string Identifier = "urn:example";

            Assert.Throws<ArgumentException>(() => new RuleSetCatalog(
                [new RuleSetVersion(Identifier, RuleSet.En16931, "1", null, null, null, "Example")],
                [new NotAnEInvoiceSpecification(Identifier, "MINIMUM", "Example")]));
        }
    }
}
