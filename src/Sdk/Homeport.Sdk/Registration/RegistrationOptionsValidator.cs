// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Sdk.Registration;

/// <summary>
/// Refuses a module that is told to register but not with what. Runs at
/// start-up, so the module fails before it serves anything.
/// </summary>
internal sealed class RegistrationOptionsValidator : IValidateOptions<RegistrationOptions>
{
    private const string Section = RegistrationOptions.SectionName;

    public ValidateOptionsResult Validate(string? name, RegistrationOptions options)
    {
        if (options.Url is null)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Key))
        {
            failures.Add($"{Section}:{nameof(RegistrationOptions.Key)} is not set. The registry accepts a module only with its registration key.");
        }

        if (options.InstanceAddress is not { IsAbsoluteUri: true })
        {
            failures.Add($"{Section}:{nameof(RegistrationOptions.InstanceAddress)} is not an absolute URI. The gateway reaches the instance there.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
