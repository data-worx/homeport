// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.ControlPlane;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>Wires how the gateway authenticates the clients that call modules (ADR-0006).</summary>
internal static class ClientAuthenticationServiceCollectionExtensions
{
    /// <summary>The policy every module route requires: a valid token of a client the control plane knows.</summary>
    internal const string ClientPolicy = "client";

    /// <summary>
    /// Adds JWT bearer authentication against the identity provider in
    /// <see cref="ClientAuthenticationOptions"/>, checked at start-up, and
    /// <see cref="ClientPolicy"/>. Needs
    /// <see cref="ControlPlaneServiceCollectionExtensions.AddControlPlaneClient"/>.
    /// </summary>
    internal static IServiceCollection AddClientAuthentication(this IServiceCollection services)
    {
        services.AddOptions<ClientAuthenticationOptions>()
            .BindConfiguration(ClientAuthenticationOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ClientAuthenticationOptions>, ClientAuthenticationOptionsValidator>();
        services.AddSingleton<ClientTokenEvents>();
        services.AddSingleton<IAuthorizationHandler, KnownClientHandler>();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ClientAuthenticationOptions>>((bearer, client) =>
            {
                bearer.Authority = client.Value.Authority?.ToString();
                bearer.Audience = client.Value.Audience;
                bearer.RequireHttpsMetadata = client.Value.RequireHttpsMetadata;
                bearer.MapInboundClaims = false;
                bearer.EventsType = typeof(ClientTokenEvents);
            });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddAuthorizationBuilder()
            .AddPolicy(ClientPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new KnownClientRequirement()));

        return services;
    }
}
