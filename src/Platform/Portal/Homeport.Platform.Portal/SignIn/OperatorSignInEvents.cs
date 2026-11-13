// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Homeport.Platform.Portal.SignIn;

/// <summary>Refuses a sign-in over plain HTTP before the operator leaves for the identity provider.</summary>
internal sealed class OperatorSignInEvents(ILogger<OperatorSignInEvents> logger) : OpenIdConnectEvents
{
    /// <summary>The sign-in page, telling the operator why the sign-in did not start.</summary>
    internal static readonly string HttpsRequiredPage =
        new PathString(PortalPaths.SignIn).Add(QueryString.Create(PortalPaths.ReasonParameter, PortalPaths.HttpsRequiredReason));

    // TLS ends at the operator's reverse proxy, which says so in
    // X-Forwarded-Proto (ADR-0020). A sign-in that would come back over
    // plain HTTP is not started: the identity provider would send the code
    // in the clear, and the Secure cookies would never arrive.
    public override Task RedirectToIdentityProvider(RedirectContext context)
    {
        if (!context.Request.IsHttps)
        {
            logger.LogWarning("A sign-in over plain HTTP was refused; the portal needs HTTPS, or a trusted proxy that terminates it");
            context.Response.Redirect(HttpsRequiredPage);
            context.HandleResponse();
        }

        return base.RedirectToIdentityProvider(context);
    }
}
