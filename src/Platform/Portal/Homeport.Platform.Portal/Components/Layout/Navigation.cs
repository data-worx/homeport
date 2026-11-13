// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.Theming;

namespace Homeport.Platform.Portal.Components.Layout;

/// <summary>
/// The entries of the drawer. A page adds its entry with the page itself,
/// so the drawer never links to a page that does not exist.
/// </summary>
internal static class Navigation
{
    /// <summary>The pages of the platform's tenants, plans, clients, modules and usage.</summary>
    internal static readonly IReadOnlyList<NavigationEntry> Main =
    [
        new("Tenants", "tenants", HomeportIcons.Platform.Tenant),
    ];

    /// <summary>The pages of the platform itself, shown under <em>Platform</em>.</summary>
    internal static readonly IReadOnlyList<NavigationEntry> Platform = [];
}

/// <summary>One entry of the drawer.</summary>
/// <param name="Label">The text of the entry.</param>
/// <param name="Href">The page it links to, relative to the base address.</param>
/// <param name="Icon">An icon from <c>HomeportIcons</c>.</param>
internal sealed record NavigationEntry(string Label, string Href, string Icon);
