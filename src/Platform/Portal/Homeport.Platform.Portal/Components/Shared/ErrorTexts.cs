// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.Components.Shared;

/// <summary>The texts of a missing page, shown by the router and by a re-executed 404 alike.</summary>
internal static class ErrorTexts
{
    /// <summary>The title of a missing page.</summary>
    internal const string NotFoundTitle = "Page not found";

    /// <summary>The message of a missing page.</summary>
    internal const string NotFoundMessage = "The portal has no page at this address.";
}
