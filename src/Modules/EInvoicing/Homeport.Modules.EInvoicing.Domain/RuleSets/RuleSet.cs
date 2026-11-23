// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.RuleSets;

/// <summary>A line of rules an invoice claims to follow, named by its specification identifier (BT-24).</summary>
/// <remarks>
/// The ZUGFeRD / Factur-X profiles EN 16931 and XRECHNUNG carry the
/// identifiers of EN 16931 and XRechnung, so they are these rule sets.
/// </remarks>
public enum RuleSet
{
    /// <summary>EN 16931 itself, without a further specification.</summary>
    En16931,

    /// <summary>XRechnung, the German CIUS of EN 16931.</summary>
    XRechnung,

    /// <summary>The XRechnung extension, conformant to EN 16931 rather than compliant.</summary>
    XRechnungExtension,

    /// <summary>The ZUGFeRD / Factur-X profile BASIC.</summary>
    FacturXBasic,

    /// <summary>The ZUGFeRD / Factur-X profile EXTENDED.</summary>
    FacturXExtended,
}
