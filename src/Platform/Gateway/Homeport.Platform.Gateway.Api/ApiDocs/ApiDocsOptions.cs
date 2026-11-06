// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>Whether the gateway serves the consolidated API docs (ADR-0017).</summary>
public sealed class ApiDocsOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Gateway:Docs";

    /// <summary>
    /// Serves the view at <see cref="ApiDocsPaths.View"/> and its documents,
    /// anonymously. Default: on in Development, off otherwise.
    /// </summary>
    public bool Enabled { get; set; }
}
