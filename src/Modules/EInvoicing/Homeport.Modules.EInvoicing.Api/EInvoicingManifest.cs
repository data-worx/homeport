// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk;
using Homeport.Shared.Client.ModuleContract;

namespace Homeport.Modules.EInvoicing.Api;

/// <summary>What the e-invoicing module declares about itself in its manifest.</summary>
internal static class EInvoicingManifest
{
    /// <summary>The route that validates a document and reports what it found.</summary>
    internal const string ValidatePath = "/v1/validate";

    /// <summary>The scope a caller needs to validate a document.</summary>
    internal const string ValidateScope = "e-invoicing.validate";

    /// <summary>The route that reads a document into the semantic model of EN 16931, and validates it.</summary>
    internal const string ReadPath = "/v1/read";

    /// <summary>The scope a caller needs to read a document.</summary>
    internal const string ReadScope = "e-invoicing.read";

    /// <summary>The meter a document is counted in, one per document the module processed.</summary>
    internal const string DocumentsMeter = "documents";

    /// <summary>What a processed document counts in <see cref="DocumentsMeter"/>.</summary>
    internal const int ProcessedDocument = 1;

    /// <summary>What a document the module rejects counts in <see cref="DocumentsMeter"/>.</summary>
    internal const int RejectedDocument = 0;

    /// <summary>The manifest the e-invoicing module registers with.</summary>
    internal static ModuleManifest Create()
    {
        return new ModuleManifest(
            Id: "e-invoicing",
            Name: "E-invoicing",
            Version: ModuleVersion.Of(typeof(EInvoicingManifest).Assembly),
            Routes:
            [
                new ManifestRoute(HttpMethods.Post, ValidatePath, ValidateScope, DocumentsMeter, ProcessedDocument),
                new ManifestRoute(HttpMethods.Post, ReadPath, ReadScope, DocumentsMeter, ProcessedDocument),
            ],
            Scopes: [ValidateScope, ReadScope],
            Meters: [DocumentsMeter]);
    }
}
