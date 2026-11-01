// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Registration;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Registry.Infrastructure.Keys;

/// <summary>
/// Refuses a registry whose registration keys cannot work. Runs at start-up,
/// so a misconfigured key fails the registry, not a module's registration.
/// </summary>
internal sealed class RegistrationKeyOptionsValidator : IValidateOptions<RegistrationKeyOptions>
{
    private const string Section = $"{RegistrationKeyOptions.SectionName}:{nameof(RegistrationKeyOptions.RegistrationKeys)}";

    public ValidateOptionsResult Validate(string? name, RegistrationKeyOptions options)
    {
        var failures = new List<string>();

        foreach (var (moduleId, keys) in options.RegistrationKeys)
        {
            if (keys.Length == 0 || keys.Any(string.IsNullOrWhiteSpace))
            {
                failures.Add($"{Section}:{moduleId} holds an empty key.");
            }

            if (keys.Length > RegistrationKeys.MaximumPerModule)
            {
                failures.Add($"{Section}:{moduleId} holds {keys.Length} keys; at most {RegistrationKeys.MaximumPerModule} are valid at once.");
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
