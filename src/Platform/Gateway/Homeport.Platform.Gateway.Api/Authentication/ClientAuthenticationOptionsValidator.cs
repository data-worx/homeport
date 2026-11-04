// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>Keeps the gateway from starting without an identity provider to accept tokens from.</summary>
internal sealed class ClientAuthenticationOptionsValidator : IValidateOptions<ClientAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, ClientAuthenticationOptions options)
    {
        var failures = new List<string>();

        if (options.Authority is not { IsAbsoluteUri: true })
        {
            failures.Add($"{ClientAuthenticationOptions.SectionName}:{nameof(options.Authority)} must be the absolute URI of the identity provider's issuer.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{ClientAuthenticationOptions.SectionName}:{nameof(options.Audience)} must name the audience the gateway accepts.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientIdClaim))
        {
            failures.Add($"{ClientAuthenticationOptions.SectionName}:{nameof(options.ClientIdClaim)} must name the claim that carries the client id.");
        }

        if (string.IsNullOrWhiteSpace(options.ScopeClaim))
        {
            failures.Add($"{ClientAuthenticationOptions.SectionName}:{nameof(options.ScopeClaim)} must name the claim that carries the scopes.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
