// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Homeport.Testing.Containers;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration.Controllers;

/// <summary>
/// The module's validation compared with the KoSIT validator, the reference,
/// on every instance of the KoSIT test suite.
/// </summary>
public sealed class ValidationControllerKositTests
{
    public sealed class Validate(EInvoicingBehindGateway module, KositValidatorContainer kosit)
        : IClassFixture<EInvoicingBehindGateway>, IClassFixture<KositValidatorContainer>
    {
        private const string ValidatePath = "/v1/validate";

        // The module's own rules — the rule set's window, the XML schemas and
        // the like — have no counterpart among KoSIT's rule IDs.
        private const string ModuleRulePrefix = "EINV-";

        private const string Valid = "valid";

        public static TheoryData<string> TestSuite => new(TestSuiteInvoices.Names);

        [Theory]
        [MemberData(nameof(TestSuite))]
        public async Task Fires_The_Rules_KoSIT_Fires(string name)
        {
            var document = TestSuiteInvoices.Bytes(name);

            var expected = (await kosit.ValidateAsync(document)).Rules.Except(NotYetImplementedRules.All).Order(StringComparer.Ordinal);
            var actual = (await ModuleRulesAsync(document)).Where(rule => !rule.StartsWith(ModuleRulePrefix, StringComparison.Ordinal)).Order(StringComparer.Ordinal);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(TestSuite))]
        public async Task Fires_No_Rule_Listed_As_Not_Yet_Implemented(string name)
        {
            var fired = await ModuleRulesAsync(TestSuiteInvoices.Bytes(name));

            Assert.Empty(fired.Intersect(NotYetImplementedRules.All));
        }

        [Theory]
        [MemberData(nameof(TestSuite))]
        public async Task Reaches_The_Verdict_KoSIT_Reaches(string name)
        {
            var document = TestSuiteInvoices.Bytes(name);

            var accepted = (await kosit.ValidateAsync(document)).Accepted;
            var verdict = (await ReportAsync(document)).RootElement.GetProperty("verdict").GetString();

            Assert.Equal(accepted, verdict == Valid);
        }

        [Fact]
        public async Task Lists_Only_Rules_KoSIT_Still_Fires_As_Not_Yet_Implemented()
        {
            var fired = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in TestSuiteInvoices.Names)
            {
                fired.UnionWith((await kosit.ValidateAsync(TestSuiteInvoices.Bytes(name))).Rules);
            }

            Assert.Empty(NotYetImplementedRules.All.Except(fired));
        }

        private async Task<IReadOnlyList<string>> ModuleRulesAsync(byte[] document)
        {
            using var report = await ReportAsync(document);

            return report.RootElement.GetProperty("findings").EnumerateArray()
                .Select(finding => finding.GetProperty("rule").GetString()!)
                .ToList();
        }

        private async Task<JsonDocument> ReportAsync(byte[] document)
        {
            using var content = new ByteArrayContent(document);
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Xml);
            using var response = await module.CreateClient().PostAsync(ValidatePath, content);
            response.EnsureSuccessStatusCode();

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        }
    }
}
