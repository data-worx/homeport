// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Application;
using Homeport.Platform.Registry.Infrastructure.Instances;
using Homeport.Platform.Registry.Infrastructure.Keys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Registry.Infrastructure;

/// <summary>Wires what the registry keeps and reads outside its process.</summary>
public static class RegistryInfrastructureExtensions
{
    /// <summary>The connection name of Valkey, as the AppHost declares it.</summary>
    public const string ValkeyConnectionName = "valkey";

    /// <summary>
    /// Keeps the instances in Valkey, connected while the registry starts
    /// (<see cref="ValkeyConnection"/>), and reads the registration keys from
    /// configuration, checked at start-up.
    /// </summary>
    public static IHostApplicationBuilder AddRegistryInfrastructure(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient(ValkeyConnectionName);

        builder.Services.AddOptions<RegistrationKeyOptions>()
            .BindConfiguration(RegistrationKeyOptions.SectionName)
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<RegistrationKeyOptions>, RegistrationKeyOptionsValidator>();
        builder.Services.AddSingleton<IRegistrationKeySource, ConfiguredRegistrationKeys>();
        builder.Services.AddSingleton<IInstanceStore, ValkeyInstanceStore>();
        builder.Services.AddHostedService<ValkeyConnection>();

        return builder;
    }
}
