// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.AdminApi;
using Homeport.Platform.Portal.Components;
using Homeport.Platform.Portal.SignIn;
using Homeport.Shared.ServiceDefaults;
using Homeport.Shared.ServiceDefaults.Proxies;
using Microsoft.Extensions.Options;
using MudBlazor.Services;

namespace Homeport.Platform.Portal;

/// <summary>
/// The admin portal: the operator signs in and manages the platform through
/// the gateway's admin API (ADR-0020). Blazor Server, so the operator's token
/// never reaches the browser.
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the portal and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddTrustedProxies();
        builder.Services.AddOperatorSignIn();
        builder.Services.AddAdminApi();
        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddMudServices();
        builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.Always);

        var app = builder.Build();

        // First, so every later step sees the caller and scheme the operator's
        // reverse proxy saw.
        app.UseForwardedHeaders();

        // The portal answers a browser with HTML pages that show the trace id,
        // the one exception to Problem Details (ADR-0020).
        app.UseExceptionHandler(PortalPaths.Error, createScopeForErrors: true);
        app.UseStatusCodePagesWithReExecute(PortalPaths.Error + "/{0}");

        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        // The stylesheets, fonts and scripts of every page, the sign-in page included.
        app.MapStaticAssets()
            .AllowAnonymous();
        app.MapControllers();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        LogStart(app);
        app.Run();
    }

    // Once started, so the options have passed validation. No client secret:
    // it is a secret.
    private static void LogStart(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var signIn = app.Services.GetRequiredService<IOptions<SignInOptions>>().Value;
            var proxies = app.Services.GetRequiredService<IOptions<TrustedProxiesOptions>>().Value;
            var adminApi = app.Services.GetRequiredService<IOptions<AdminApiOptions>>().Value;

            app.Logger.LogInformation(
                "Portal started on {Urls}: operators sign in at {Authority} as client {ClientId}, access with the claim {ClaimType} = {ClaimValue}, names from {NameClaim}, admin API at {AdminApiUrl}, forwarded headers from proxies {TrustedProxies} and networks {TrustedNetworks}",
                app.Urls,
                signIn.Authority,
                signIn.ClientId,
                signIn.ClaimType,
                signIn.ClaimValue,
                signIn.NameClaim,
                adminApi.Url,
                proxies.Proxies,
                proxies.Networks);
        });
    }
}
