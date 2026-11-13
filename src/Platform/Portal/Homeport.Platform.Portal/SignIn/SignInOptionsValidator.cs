// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.SignIn;

/// <summary>Keeps the portal from starting without an identity provider to sign operators in at.</summary>
internal sealed class SignInOptionsValidator : IValidateOptions<SignInOptions>
{
    public ValidateOptionsResult Validate(string? name, SignInOptions options)
    {
        var failures = new List<string>();

        if (options.Authority is not { IsAbsoluteUri: true })
        {
            failures.Add($"{SignInOptions.SectionName}:{nameof(options.Authority)} must be the absolute URI of the identity provider's issuer.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add($"{SignInOptions.SectionName}:{nameof(options.ClientId)} and {nameof(options.ClientSecret)} must name the portal's client at the identity provider.");
        }

        if (string.IsNullOrWhiteSpace(options.ClaimType) || string.IsNullOrWhiteSpace(options.ClaimValue))
        {
            failures.Add($"{SignInOptions.SectionName}:{nameof(options.ClaimType)} and {nameof(options.ClaimValue)} must name the claim that grants access.");
        }

        if (string.IsNullOrWhiteSpace(options.NameClaim))
        {
            failures.Add($"{SignInOptions.SectionName}:{nameof(options.NameClaim)} must name the claim that carries the operator's display name.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
