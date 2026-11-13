// SPDX-License-Identifier: AGPL-3.0-or-later

using Bunit;
using Homeport.Platform.Portal.AdminApi;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Homeport.Platform.Portal.Tests.Unit;

/// <summary>
/// A bUnit context with MudBlazor and <see cref="Api"/> in place of the admin
/// API. MudBlazor's JavaScript is not there; its calls answer with defaults.
/// </summary>
public abstract class PortalTestContext : BunitContext, IAsyncLifetime
{
    protected PortalTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IAdminApi>(Api);
    }

    // MudBlazor's services dispose only asynchronously; xUnit calls this
    // before the synchronous Dispose, which then finds nothing left to do.
    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    /// <summary>The admin API the components call.</summary>
    internal StandInAdminApi Api { get; } = new();

    /// <summary>
    /// Renders <typeparamref name="TPage"/> with the providers the layout
    /// renders around a page, so its dialogs and popovers have a place.
    /// </summary>
    protected (IRenderedComponent<TPage> Page, IRenderedComponent<MudDialogProvider> Dialogs) RenderWithProviders<TPage>()
        where TPage : Microsoft.AspNetCore.Components.IComponent
    {
        Render<MudPopoverProvider>();
        var dialogs = Render<MudDialogProvider>();
        return (Render<TPage>(), dialogs);
    }
}
