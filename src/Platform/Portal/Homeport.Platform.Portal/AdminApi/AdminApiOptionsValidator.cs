// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>Refuses to start without an absolute address of the admin API.</summary>
internal sealed class AdminApiOptionsValidator : IValidateOptions<AdminApiOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminApiOptions options)
    {
        return options.Url is { IsAbsoluteUri: true }
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"{AdminApiOptions.SectionName}:{nameof(AdminApiOptions.Url)} must be the absolute address of the gateway's admin endpoint.");
    }
}
