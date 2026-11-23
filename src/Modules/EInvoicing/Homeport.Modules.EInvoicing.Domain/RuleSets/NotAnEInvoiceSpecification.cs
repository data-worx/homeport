// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.RuleSets;

/// <summary>A specification whose documents are no e-invoice under EN 16931, such as the profile MINIMUM.</summary>
/// <param name="CustomizationId">The specification identifier (BT-24) such a document carries.</param>
/// <param name="Profile">The ZUGFeRD / Factur-X profile a PDF carrying it declares in its XMP metadata.</param>
/// <param name="Standard">The name of the standard in the module's <c>standards.json</c> it is taken from.</param>
public sealed record NotAnEInvoiceSpecification(string CustomizationId, string Profile, string Standard);
