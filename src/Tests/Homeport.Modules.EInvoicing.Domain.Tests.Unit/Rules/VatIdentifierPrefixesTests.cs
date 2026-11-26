// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed class VatIdentifierPrefixesTests
{
    public sealed class All
    {
        // The number of prefixes in the test of BR-CO-09 of the EN 16931
        // validation artefacts, tag validation-1.3.17, in both syntaxes.
        private const int PrefixCount = 251;

        [Fact]
        public void Holds_Every_Prefix_Of_The_Validation_Artefacts() => Assert.Equal(PrefixCount, VatIdentifierPrefixes.All.Count);

        [Fact]
        public void Holds_The_Prefixes_Beyond_Iso_3166() => Assert.Superset(new HashSet<string> { "1A", "XI" }, VatIdentifierPrefixes.All.ToHashSet());

        [Fact]
        public void Leaves_Out_The_Greek_Vat_Prefix_As_The_Validation_Artefacts_Do() => Assert.DoesNotContain("EL", VatIdentifierPrefixes.All);
    }
}
