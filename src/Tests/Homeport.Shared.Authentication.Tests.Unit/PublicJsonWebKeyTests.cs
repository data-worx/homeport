// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text.Json;

namespace Homeport.Shared.Authentication.Tests.Unit;

public sealed class PublicJsonWebKeyTests
{
    public sealed class FromKey
    {
        [Fact]
        public void Publishes_The_Public_Point_Only()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var json = JsonSerializer.SerializeToElement(PublicJsonWebKey.FromKey(key, "current"));

            Assert.Equal(
                ["alg", "crv", "kid", "kty", "use", "x", "y"],
                json.EnumerateObject().Select(member => member.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public void Publishes_The_Key_Id()
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var published = PublicJsonWebKey.FromKey(key, "current");

            Assert.Equal("current", published.Kid);
        }
    }
}
