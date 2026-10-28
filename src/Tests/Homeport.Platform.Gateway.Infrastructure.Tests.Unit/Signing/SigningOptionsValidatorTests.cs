// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Signing;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

public sealed class SigningOptionsValidatorTests
{
    public sealed class Validate
    {
        private readonly SigningOptionsValidator validator = new();

        [Fact]
        public void Succeeds_For_A_P256_Private_Key_With_Its_Id()
        {
            var result = validator.Validate(Options.DefaultName, TestKeys.Options());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Succeeds_With_An_Additional_Public_Key_And_Its_Id()
        {
            var options = TestKeys.Options();
            options.AdditionalPublicKey = TestKeys.PublicP256();
            options.AdditionalKeyId = "previous";

            var result = validator.Validate(Options.DefaultName, options);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Fails_Without_A_Key()
        {
            var result = validator.Validate(Options.DefaultName, new SigningOptions { KeyId = "current" });

            Assert.Contains("Signing:Key is not set", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_Without_A_Key_Id()
        {
            var result = validator.Validate(Options.DefaultName, new SigningOptions { Key = TestKeys.PrivateP256() });

            Assert.Contains("Signing:KeyId is not set", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_A_Key_On_Another_Curve()
        {
            var result = validator.Validate(Options.DefaultName, new SigningOptions { Key = TestKeys.PrivateP384(), KeyId = "current" });

            Assert.Contains("Signing:Key is not a P-256 private key", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_A_Public_Key_As_Signing_Key()
        {
            var result = validator.Validate(Options.DefaultName, new SigningOptions { Key = TestKeys.PublicP256(), KeyId = "current" });

            Assert.Contains("Signing:Key is not a P-256 private key", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_Text_That_Is_No_Pem()
        {
            var result = validator.Validate(Options.DefaultName, new SigningOptions { Key = "not a key", KeyId = "current" });

            Assert.Contains("Signing:Key is not a P-256 private key", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_An_Additional_Key_Without_Its_Id()
        {
            var options = TestKeys.Options();
            options.AdditionalPublicKey = TestKeys.PublicP256();

            var result = validator.Validate(Options.DefaultName, options);

            Assert.Contains("are set together or not at all", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_A_Private_Key_As_Additional_Public_Key()
        {
            var options = TestKeys.Options();
            options.AdditionalPublicKey = TestKeys.PrivateP256();
            options.AdditionalKeyId = "previous";

            var result = validator.Validate(Options.DefaultName, options);

            Assert.Contains("Signing:AdditionalPublicKey is not a P-256 public key", result.FailureMessage, StringComparison.Ordinal);
        }

        [Fact]
        public void Fails_For_An_Additional_Key_Id_Equal_To_The_Key_Id()
        {
            var options = TestKeys.Options();
            options.AdditionalPublicKey = TestKeys.PublicP256();
            options.AdditionalKeyId = options.KeyId;

            var result = validator.Validate(Options.DefaultName, options);

            Assert.Contains("must differ", result.FailureMessage, StringComparison.Ordinal);
        }
    }
}
