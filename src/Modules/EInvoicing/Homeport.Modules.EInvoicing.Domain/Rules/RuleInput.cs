// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Model;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>What a business rule checks: an invoice in the semantic model and the document it was read from.</summary>
/// <remarks>
/// A rule checks the model where the model holds exactly what the rule's
/// source tests. Where reading the model loses what the source tests — an
/// element repeated where the model keeps one, a blank value the model drops,
/// a context the source matches anywhere in the document — the rule checks
/// the document, so it reports what the source reports.
/// </remarks>
/// <param name="Invoice">The invoice in the semantic model of EN 16931.</param>
/// <param name="Root">The root element of the document it was read from.</param>
/// <param name="Syntax">The syntax of the document.</param>
public sealed record RuleInput(Invoice Invoice, XElement Root, InvoiceSyntax Syntax)
{
    /// <summary>Whether the document is a CII CrossIndustryInvoice rather than a UBL document.</summary>
    public bool IsCii => Syntax == InvoiceSyntax.Cii;

    /// <summary>Whether the document is a UBL CreditNote.</summary>
    public bool IsCreditNote => Syntax == InvoiceSyntax.UblCreditNote;

    /// <summary>The elements of the invoice lines (BG-25), in the order the model holds them.</summary>
    public IReadOnlyList<XElement> Lines => field ??= IsCii
        ? [.. Root.Element(RuleXml.Rsm + "SupplyChainTradeTransaction")?.Elements(RuleXml.Ram + "IncludedSupplyChainTradeLineItem") ?? []]
        : [.. Root.Elements(RuleXml.Cac + (IsCreditNote ? "CreditNoteLine" : "InvoiceLine"))];

    /// <summary>The JSON path of the invoice line <paramref name="element"/> lies in, or of the invoice when it lies in none.</summary>
    /// <param name="element">An element of the document.</param>
    public string LinePathOf(XElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        var line = element.AncestorsAndSelf().FirstOrDefault(Lines.Contains);

        return line is null ? RulePaths.Invoice : RulePaths.Line(RuleXml.IndexOf(Lines, line));
    }
}
