// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal;

/// <summary>The paths of the portal's own pages and endpoints, outside the pages of the platform.</summary>
internal static class PortalPaths
{
    /// <summary>The sign-in page, where an anonymous request lands.</summary>
    internal const string SignIn = "/sign-in";

    /// <summary>Starts the sign-in at the identity provider.</summary>
    internal const string SignInStart = "/sign-in/start";

    /// <summary>Signs the operator out, at the portal and at the identity provider.</summary>
    internal const string SignOut = "/sign-out";

    /// <summary>Where the identity provider sends the operator back to.</summary>
    internal const string SignInCallback = "/signin-oidc";

    /// <summary>Where the identity provider sends the operator back to after signing out.</summary>
    internal const string SignOutCallback = "/signout-callback-oidc";

    /// <summary>Shown to a signed-in operator without the claim that grants access.</summary>
    internal const string AccessDenied = "/access-denied";

    /// <summary>Shown for a path the portal does not have.</summary>
    internal const string NotFound = "/not-found";

    /// <summary>Shown for an unexpected error, and for an error status without a page of its own.</summary>
    internal const string Error = "/error";

    /// <summary>The query parameter of the sign-in page that names why the sign-in did not start.</summary>
    internal const string ReasonParameter = "reason";

    /// <summary>The reason a sign-in over plain HTTP was refused.</summary>
    internal const string HttpsRequiredReason = "https-required";

    /// <summary>The query parameter that carries the page to return to after signing in.</summary>
    internal const string ReturnUrlParameter = "returnUrl";
}
