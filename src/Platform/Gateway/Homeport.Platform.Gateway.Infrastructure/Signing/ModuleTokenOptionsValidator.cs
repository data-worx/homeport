// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Authentication;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Refuses a module token lifetime that is not positive or longer than a
/// gateway token may live. Runs at start-up.
/// </summary>
public sealed class ModuleTokenOptionsValidator : IValidateOptions<ModuleTokenOptions>
{
    private const string Lifetime = $"{ModuleTokenOptions.SectionName}:{nameof(ModuleTokenOptions.Lifetime)}";

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ModuleTokenOptions options)
    {
        if (options.Lifetime <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{Lifetime} must be positive.");
        }

        if (options.Lifetime > GatewayTokens.MaximumLifetime)
        {
            return ValidateOptionsResult.Fail($"{Lifetime} must not exceed {GatewayTokens.MaximumLifetime}.");
        }

        return ValidateOptionsResult.Success;
    }
}
