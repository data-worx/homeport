// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Application;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Registry.Infrastructure.Keys;

/// <summary>The registration keys from <see cref="RegistrationKeyOptions"/>.</summary>
internal sealed class ConfiguredRegistrationKeys(IOptions<RegistrationKeyOptions> options) : IRegistrationKeySource
{
    public IReadOnlyList<string> KeysFor(string moduleId)
    {
        return options.Value.RegistrationKeys.TryGetValue(moduleId, out var keys) ? keys : [];
    }
}
