// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.Registry;

namespace Homeport.Platform.Gateway.Infrastructure.Registry;

/// <summary>Where the gateway reads the routes.</summary>
public sealed class RegistryOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Registry";

    /// <summary>
    /// The registry. The default names it by its service name, which service
    /// discovery resolves from the reference the AppHost and the compose file set.
    /// </summary>
    public Uri Url { get; set; } = new($"{Uri.UriSchemeHttp}://{RegistryService.ServiceName}");
}
