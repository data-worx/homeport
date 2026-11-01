// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Homeport.Platform.Registry.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Registry.Api.Authentication;

/// <summary>
/// Authenticates a module instance by its registration key (ADR-0012): a
/// bearer token that must be one of the keys configured for the module id in
/// the route. An unknown module id fails like a wrong key.
/// </summary>
internal sealed class RegistrationKeyHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    Registrations registrations)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, loggerFactory, encoder)
{
    /// <summary>The name of the scheme.</summary>
    internal const string SchemeName = "RegistrationKey";

    /// <summary>The route value that names the module the request is for.</summary>
    internal const string ModuleIdRouteValue = "moduleId";

    private readonly ILogger logger = loggerFactory.CreateLogger<RegistrationKeyHandler>();

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.GetRouteValue(ModuleIdRouteValue) is not string moduleId)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
            || !string.Equals(header.Scheme, JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrEmpty(header.Parameter))
        {
            logger.LogWarning("A request for module {ModuleId} was rejected: it carries no registration key", moduleId);
            return Task.FromResult(AuthenticateResult.Fail("No registration key."));
        }

        if (!registrations.IsAuthorized(moduleId, header.Parameter))
        {
            logger.LogWarning("A request for module {ModuleId} was rejected: unknown module id or wrong registration key", moduleId);
            return Task.FromResult(AuthenticateResult.Fail("Unknown module id or wrong registration key."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, moduleId)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
