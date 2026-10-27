// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;

namespace Homeport.Sdk.Tests.Unit;

public sealed class ModuleVersionTests
{
    public sealed class Of
    {
        [Fact]
        public void Returns_The_Build_Version_With_Optional_Build_Metadata()
        {
            var assembly = typeof(ModuleVersionTests).Assembly;
            var buildVersion = assembly.GetName().Version!.ToString(3);

            var version = ModuleVersion.Of(assembly);

            Assert.Matches($@"^{Regex.Escape(buildVersion)}(\+[0-9A-Za-z.-]+)?$", version);
        }
    }
}
