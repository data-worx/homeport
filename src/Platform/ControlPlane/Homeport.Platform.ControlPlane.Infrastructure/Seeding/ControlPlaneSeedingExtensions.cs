// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane.Infrastructure.Seeding;

/// <summary>Wires the seed <c>migrate</c> keeps in the database.</summary>
public static class ControlPlaneSeedingExtensions
{
    /// <summary>
    /// Adds <see cref="ControlPlaneSeeder"/> with the <see cref="SeedOptions"/>
    /// bound from <see cref="SeedOptions.SectionName"/> and validated.
    /// </summary>
    public static IHostApplicationBuilder AddControlPlaneSeeding(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<SeedOptions>().BindConfiguration(SeedOptions.SectionName);
        builder.Services.AddSingleton<IValidateOptions<SeedOptions>, SeedOptionsValidator>();
        builder.Services.AddScoped<ControlPlaneSeeder>();

        return builder;
    }
}
