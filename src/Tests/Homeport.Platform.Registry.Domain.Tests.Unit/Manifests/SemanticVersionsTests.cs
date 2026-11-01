// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;

namespace Homeport.Platform.Registry.Domain.Tests.Unit.Manifests;

public sealed class SemanticVersionsTests
{
    public sealed class IsValid
    {
        [Theory]
        [InlineData("0.0.0")]
        [InlineData("1.2.3")]
        [InlineData("1.2.3-alpha.1")]
        [InlineData("1.2.3+8a6645f")]
        [InlineData("1.2.3-rc.1+8a6645f")]
        public void Accepts_A_Semantic_Version(string version)
        {
            Assert.True(SemanticVersions.IsValid(version));
        }

        [Theory]
        [InlineData("")]
        [InlineData("1.2")]
        [InlineData("01.2.3")]
        [InlineData("1.2.3-01")]
        [InlineData("v1.2.3")]
        public void Rejects_Anything_Else(string version)
        {
            Assert.False(SemanticVersions.IsValid(version));
        }
    }

    public sealed class Precedence
    {
        // Each pair is lower, higher; most come from the examples on semver.org.
        [Theory]
        [InlineData("1.0.0", "2.0.0")]
        [InlineData("2.0.0", "2.1.0")]
        [InlineData("2.1.0", "2.1.1")]
        [InlineData("9.0.0", "10.0.0")]
        [InlineData("1.0.0-alpha", "1.0.0")]
        [InlineData("1.0.0-alpha", "1.0.0-alpha.1")]
        [InlineData("1.0.0-alpha.1", "1.0.0-alpha.beta")]
        [InlineData("1.0.0-alpha.beta", "1.0.0-beta")]
        [InlineData("1.0.0-beta.2", "1.0.0-beta.11")]
        [InlineData("1.0.0-beta.11", "1.0.0-rc.1")]
        public void Ranks_The_Higher_Version_Above_The_Lower(string lower, string higher)
        {
            Assert.True(SemanticVersions.Precedence.Compare(lower, higher) < 0);
            Assert.True(SemanticVersions.Precedence.Compare(higher, lower) > 0);
        }

        [Fact]
        public void Ignores_Build_Metadata()
        {
            var result = SemanticVersions.Precedence.Compare("1.0.0+aaa", "1.0.0+bbb");

            Assert.Equal(0, result);
        }

        [Fact]
        public void Throws_For_A_String_That_Is_Not_A_Version()
        {
            Assert.Throws<ArgumentException>(() => SemanticVersions.Precedence.Compare("1.0", "1.0.0"));
        }
    }
}
