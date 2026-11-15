// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.ApiDocs;

/// <summary>Where the operator's browser reaches the gateway's API docs (ADR-0017).</summary>
public sealed class ApiDocsOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "ApiDocs";

    /// <summary>
    /// The docs on the gateway's public endpoint, such as
    /// <c>https://api.example.com/platform/docs</c>. Unset, the portal links
    /// no module to its document, as where the docs are switched off.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>The document of <paramref name="moduleId"/>, or <c>null</c> without <see cref="Url"/>.</summary>
    public Uri? DocumentOf(string moduleId)
    {
        return Url is null ? null : new Uri($"{Url.ToString().TrimEnd('/')}/{Uri.EscapeDataString(moduleId)}");
    }
}
