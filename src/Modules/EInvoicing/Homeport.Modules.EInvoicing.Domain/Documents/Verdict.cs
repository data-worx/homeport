// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>What the module concludes about a processed document.</summary>
public enum Verdict
{
    /// <summary>The document is an invoice, but no e-invoice under EN 16931, such as a ZUGFeRD MINIMUM.</summary>
    NotAnEInvoice,
}
