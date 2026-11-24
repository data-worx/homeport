// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Documents;

public sealed class VerdictRulesTests
{
    public sealed class VerdictOf
    {
        [Fact]
        public void Is_Valid_Without_A_Fatal_Finding()
        {
            Finding[] findings =
            [
                new(FindingSeverity.Warning, "EINV-RULE-SET-WINDOW", "Outside the window."),
                new(FindingSeverity.Information, "EINV-SCHEMA-LIMIT", "Stopped."),
            ];

            Assert.Equal(Verdict.Valid, VerdictRules.VerdictOf(findings));
        }

        [Fact]
        public void Is_Invalid_With_A_Fatal_Finding()
        {
            Finding[] findings =
            [
                new(FindingSeverity.Warning, "EINV-RULE-SET-WINDOW", "Outside the window."),
                new(FindingSeverity.Fatal, "EINV-SCHEMA", "Missing."),
            ];

            Assert.Equal(Verdict.Invalid, VerdictRules.VerdictOf(findings));
        }
    }
}
