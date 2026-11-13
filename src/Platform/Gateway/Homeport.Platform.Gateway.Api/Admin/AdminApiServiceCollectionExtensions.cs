// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>Wires the admin API: its port, the operator's tokens and its routes (ADR-0020).</summary>
internal static class AdminApiServiceCollectionExtensions
{
    /// <summary>The policy every admin route requires: a valid operator token with the admin claim.</summary>
    internal const string AdminPolicy = "admin";

    /// <summary>The authentication scheme of the operator's tokens.</summary>
    internal const string OperatorScheme = "operator";

    // The default scheme: picks the client's or the operator's tokens by the
    // port a request arrived on, so neither is ever tried on the other's port.
    private const string PortScheme = "port";

    /// <summary>
    /// Adds the admin API's options, checked at start-up, JWT bearer
    /// authentication of the operator's tokens against the identity provider
    /// in <see cref="AdminApiOptions"/>, <see cref="AdminPolicy"/> and the
    /// admin routes. Called after the client authentication, which stays the
    /// scheme of every port but the admin port.
    /// </summary>
    internal static IServiceCollection AddAdminApi(this IServiceCollection services)
    {
        services.AddOptions<AdminApiOptions>()
            .BindConfiguration(AdminApiOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminApiOptions>, AdminApiOptionsValidator>();
        services.AddSingleton<AdminPort>();
        services.AddSingleton<OperatorTokenEvents>();
        services.AddSingleton<IAuthorizationHandler, AdminClaimHandler>();
        services.AddSingleton(_ => AdminRoutes.Configured());

        services.AddOptions<JwtBearerOptions>(OperatorScheme)
            .Configure<IOptions<AdminApiOptions>>((bearer, admin) =>
            {
                bearer.Authority = admin.Value.Authority?.ToString();
                bearer.Audience = admin.Value.Audience;
                bearer.RequireHttpsMetadata = admin.Value.RequireHttpsMetadata;
                bearer.MapInboundClaims = false;
                bearer.EventsType = typeof(OperatorTokenEvents);
            });

        services.AddAuthentication(PortScheme)
            .AddJwtBearer(OperatorScheme, _ => { })
            .AddPolicyScheme(PortScheme, displayName: null, policy => policy.ForwardDefaultSelector = context =>
                context.RequestServices.GetRequiredService<AdminPort>().Serves(context)
                    ? OperatorScheme
                    : JwtBearerDefaults.AuthenticationScheme);

        services.AddAuthorizationBuilder()
            .AddPolicy(AdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new AdminClaimRequirement()));

        return services;
    }
}
