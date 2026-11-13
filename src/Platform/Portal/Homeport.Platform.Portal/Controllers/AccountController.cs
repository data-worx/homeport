// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Platform.Portal.Controllers;

/// <summary>
/// Starts the operator's sign-in at the identity provider and signs them out.
/// The browser navigates here; it answers with redirects, not with a body.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AccountController : Controller
{
    /// <summary>
    /// Sends the operator to the identity provider: authorization code with
    /// PKCE. Over plain HTTP it sends them back to the sign-in page instead.
    /// </summary>
    /// <param name="returnUrl">The portal page to return to once signed in.</param>
    /// <response code="302">To the identity provider, or back to the sign-in page.</response>
    // Anonymous by nature: it is how an anonymous operator signs in.
    [AllowAnonymous]
    [HttpGet(PortalPaths.SignInStart)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult SignInOperator([FromQuery(Name = PortalPaths.ReturnUrlParameter)] string? returnUrl)
    {
        var redirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");

        return Challenge(new AuthenticationProperties { RedirectUri = redirectUri }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    /// <summary>Signs the operator out of the portal and of the identity provider.</summary>
    /// <response code="302">To the identity provider's sign-out, which returns to the sign-in page.</response>
    // Anonymous, so an operator the portal turned away can still sign out
    // and sign in with another account.
    [AllowAnonymous]
    [HttpPost(PortalPaths.SignOut)]
    [ValidateAntiForgeryToken]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult SignOutOperator()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = PortalPaths.SignIn },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }
}
