// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.Components;
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
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();
        builder.Services.AddMudServices();

        var app = builder.Build();

        // First, so every later step sees the caller and scheme the operator's
        // reverse proxy saw.
        app.UseForwardedHeaders();

        app.MapDefaultEndpoints();
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        LogStart(app);
        app.Run();
    }

    private static void LogStart(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var proxies = app.Services.GetRequiredService<IOptions<TrustedProxiesOptions>>().Value;

            app.Logger.LogInformation(
                "Portal started on {Urls}: forwarded headers from proxies {TrustedProxies} and networks {TrustedNetworks}",
                app.Urls,
                proxies.Proxies,
                proxies.Networks);
        });
    }
}
