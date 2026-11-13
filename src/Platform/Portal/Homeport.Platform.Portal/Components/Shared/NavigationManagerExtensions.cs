// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Components;

namespace Homeport.Platform.Portal.Components.Shared;

/// <summary>What a page does when the admin API needs the operator to sign in again.</summary>
internal static class NavigationManagerExtensions
{
    /// <summary>
    /// Starts the sign-in at the identity provider, outside the circuit, and
    /// returns to the current page afterwards.
    /// </summary>
    internal static void SignInAgain(this NavigationManager navigation)
    {
        var returnUrl = "/" + navigation.ToBaseRelativePath(navigation.Uri);
        navigation.NavigateTo($"{PortalPaths.SignInStart}?{PortalPaths.ReturnUrlParameter}={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
