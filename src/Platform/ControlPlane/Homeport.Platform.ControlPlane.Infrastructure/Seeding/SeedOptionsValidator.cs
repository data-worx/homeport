// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane.Infrastructure.Seeding;

/// <summary>Refuses a seed that the tables would reject or that limits nothing.</summary>
internal sealed class SeedOptionsValidator : IValidateOptions<SeedOptions>
{
    private const string Section = SeedOptions.SectionName;

    public ValidateOptionsResult Validate(string? name, SeedOptions options)
    {
        var failures = new List<string>();

        if (options.TenantId == Guid.Empty)
        {
            failures.Add($"{Section}:{nameof(SeedOptions.TenantId)} is missing.");
        }

        if (options.PlanId == Guid.Empty)
        {
            failures.Add($"{Section}:{nameof(SeedOptions.PlanId)} is missing.");
        }

        RequireText(failures, nameof(SeedOptions.TenantName), options.TenantName, Tenant.MaxNameLength);
        RequireText(failures, nameof(SeedOptions.PlanName), options.PlanName, Plan.MaxNameLength);
        RequireText(failures, nameof(SeedOptions.ClientId), options.ClientId, Client.MaxClientIdLength);

        if (options.RequestsPerSecond <= 0)
        {
            failures.Add($"{Section}:{nameof(SeedOptions.RequestsPerSecond)} must be positive.");
        }

        if (options.Burst < options.RequestsPerSecond)
        {
            failures.Add($"{Section}:{nameof(SeedOptions.Burst)} must be at least {nameof(SeedOptions.RequestsPerSecond)}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void RequireText(List<string> failures, string key, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{Section}:{key} is missing.");
        }
        else if (value.Length > maxLength)
        {
            failures.Add($"{Section}:{key} is longer than {maxLength} characters.");
        }
    }
}
