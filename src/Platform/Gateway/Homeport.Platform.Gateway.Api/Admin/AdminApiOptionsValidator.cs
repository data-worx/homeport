// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// Keeps the gateway from starting with an admin port but without an identity
/// provider to accept the operator's tokens from. Without a port nothing is
/// required: the admin API is off.
/// </summary>
internal sealed class AdminApiOptionsValidator : IValidateOptions<AdminApiOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminApiOptions options)
    {
        if (options.Port is null)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (options.Port is < IPEndPoint.MinPort + 1 or > IPEndPoint.MaxPort)
        {
            failures.Add($"{AdminApiOptions.SectionName}:{nameof(options.Port)} must be a port between {IPEndPoint.MinPort + 1} and {IPEndPoint.MaxPort}.");
        }

        if (options.Authority is not { IsAbsoluteUri: true })
        {
            failures.Add($"{AdminApiOptions.SectionName}:{nameof(options.Authority)} must be the absolute URI of the identity provider's issuer.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{AdminApiOptions.SectionName}:{nameof(options.Audience)} must name the audience the admin API accepts.");
        }

        if (string.IsNullOrWhiteSpace(options.ClaimType) || string.IsNullOrWhiteSpace(options.ClaimValue))
        {
            failures.Add($"{AdminApiOptions.SectionName}:{nameof(options.ClaimType)} and {nameof(options.ClaimValue)} must name the claim that grants admin access.");
        }

        if (string.IsNullOrWhiteSpace(options.NameClaim))
        {
            failures.Add($"{AdminApiOptions.SectionName}:{nameof(options.NameClaim)} must name the claim that carries the operator's display name.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
