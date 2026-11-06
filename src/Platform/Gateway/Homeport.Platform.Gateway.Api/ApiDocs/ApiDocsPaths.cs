// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>Where the consolidated API docs are served (ADR-0017).</summary>
internal static class ApiDocsPaths
{
    /// <summary>The view, rendered with Scalar.</summary>
    internal const string View = "/platform/docs";

    /// <summary>The documents, one per module and one for the platform, as <c>{name}.json</c> below it.</summary>
    internal const string Documents = View + "/openapi";

    /// <summary>The route parameter that names a document.</summary>
    internal const string DocumentName = "documentName";

    /// <summary>The route of one document, as Scalar fills it in.</summary>
    internal const string Document = Documents + "/{" + DocumentName + "}.json";

    /// <summary>The name of the platform's document; never a module id, as ADR-0013 reserves it.</summary>
    internal const string PlatformDocument = "platform";
}
