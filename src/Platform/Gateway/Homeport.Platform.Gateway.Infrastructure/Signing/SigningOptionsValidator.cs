// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Refuses a gateway without a usable signing key. Runs at start-up, so a
/// misconfigured gateway never starts instead of failing on its first token.
/// </summary>
public sealed class SigningOptionsValidator : IValidateOptions<SigningOptions>
{
    private const string Section = SigningOptions.SectionName;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SigningOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add($"{Section}:{nameof(SigningOptions.Key)} is not set. The gateway needs a P-256 private key in PEM to sign tokens.");
        }
        else if (!IsPrivateKey(options.Key))
        {
            failures.Add($"{Section}:{nameof(SigningOptions.Key)} is not a P-256 private key in PEM.");
        }

        if (string.IsNullOrWhiteSpace(options.KeyId))
        {
            failures.Add($"{Section}:{nameof(SigningOptions.KeyId)} is not set.");
        }

        var hasAdditionalKey = !string.IsNullOrWhiteSpace(options.AdditionalPublicKey);
        var hasAdditionalKeyId = !string.IsNullOrWhiteSpace(options.AdditionalKeyId);
        if (hasAdditionalKey != hasAdditionalKeyId)
        {
            failures.Add($"{Section}:{nameof(SigningOptions.AdditionalPublicKey)} and {Section}:{nameof(SigningOptions.AdditionalKeyId)} are set together or not at all.");
        }
        else if (hasAdditionalKey && !IsPublicKey(options.AdditionalPublicKey!))
        {
            failures.Add($"{Section}:{nameof(SigningOptions.AdditionalPublicKey)} is not a P-256 public key in PEM.");
        }
        else if (hasAdditionalKey && options.AdditionalKeyId == options.KeyId)
        {
            failures.Add($"{Section}:{nameof(SigningOptions.AdditionalKeyId)} must differ from {Section}:{nameof(SigningOptions.KeyId)}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsPrivateKey(string pem)
    {
        if (!SigningKeyPem.TryReadPrivate(pem, out var key))
        {
            return false;
        }

        key.Dispose();
        return true;
    }

    private static bool IsPublicKey(string pem)
    {
        if (!SigningKeyPem.TryReadPublic(pem, out var key))
        {
            return false;
        }

        key.Dispose();
        return true;
    }
}
