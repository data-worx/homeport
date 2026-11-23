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

    /// <summary>The meter a document is counted in, one per document the module processed.</summary>
    internal const string DocumentsMeter = "documents";

    /// <summary>The manifest the e-invoicing module registers with.</summary>
    internal static ModuleManifest Create()
    {
        return new ModuleManifest(
            Id: "e-invoicing",
            Name: "E-invoicing",
            Version: ModuleVersion.Of(typeof(EInvoicingManifest).Assembly),
            Routes:
            [
                new ManifestRoute(HttpMethods.Post, ValidatePath, ValidateScope, DocumentsMeter, Quantity: 1),
            ],
            Scopes: [ValidateScope],
            Meters: [DocumentsMeter]);
    }
}
