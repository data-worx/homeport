// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>Keeps the gateway from starting with a forwarding timeout that would fail every request.</summary>
internal sealed class ForwardingOptionsValidator : IValidateOptions<ForwardingOptions>
{
    /// <summary>Requires a positive timeout.</summary>
    public ValidateOptionsResult Validate(string? name, ForwardingOptions options)
    {
        return options.Timeout > TimeSpan.Zero
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"{ForwardingOptions.SectionName}:{nameof(options.Timeout)} must be positive.");
    }
}
