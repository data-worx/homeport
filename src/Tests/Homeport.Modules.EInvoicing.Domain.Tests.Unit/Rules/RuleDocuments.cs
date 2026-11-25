// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Reading;
using Homeport.Modules.EInvoicing.Domain.Rules;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

/// <summary>
/// The minimal invoices, which break no implemented rule, as elements a test
/// changes to break one: it adds a fragment below an element or removes one,
/// both named by a path of local names.
/// </summary>
internal static class RuleDocuments
{
    private const string En16931 = "urn:cen.eu:en16931:2017";

    private const string Prefixes =
        "xmlns:cac=\"urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2\" "
        + "xmlns:cbc=\"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2\" "
        + "xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\" "
        + "xmlns:ram=\"urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100\" "
        + "xmlns:udt=\"urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100\"";

    /// <summary>The minimal UBL Invoice.</summary>
    public static XElement Ubl() => XElement.Parse(MinimalInvoices.Ubl(En16931, "2026-11-25"));

    /// <summary>The minimal CII CrossIndustryInvoice.</summary>
    public static XElement Cii() => XElement.Parse(MinimalInvoices.Cii(En16931, "20261125"));

    /// <summary>Adds <paramref name="fragment"/>, written with the prefixes cac, cbc, rsm, ram and udt, as the last children of the element at <paramref name="path"/>.</summary>
    /// <param name="root">The document.</param>
    /// <param name="path">The first element of this local name, then children by local name, separated by <c>/</c>.</param>
    /// <param name="fragment">The elements to add.</param>
    public static XElement With(this XElement root, string path, string fragment)
    {
        var wrapper = XElement.Parse($"<fragment {Prefixes}>{fragment}</fragment>");
        At(root, path).Add(wrapper.Elements());

        return root;
    }

    /// <summary>Removes the element at <paramref name="path"/>.</summary>
    /// <param name="root">The document.</param>
    /// <param name="path">The first element of this local name, then children by local name, separated by <c>/</c>.</param>
    public static XElement Without(this XElement root, string path)
    {
        At(root, path).Remove();

        return root;
    }

    /// <summary>Sets the value of the element at <paramref name="path"/>.</summary>
    /// <param name="root">The document.</param>
    /// <param name="path">The first element of this local name, then children by local name, separated by <c>/</c>.</param>
    /// <param name="value">The value.</param>
    public static XElement Set(this XElement root, string path, string value)
    {
        At(root, path).Value = value;

        return root;
    }

    /// <summary>The document as a rule checks it, with the invoice read from it.</summary>
    /// <param name="root">The document.</param>
    public static RuleInput Input(this XElement root)
    {
        var syntax = root.Name.LocalName switch
        {
            "CrossIndustryInvoice" => InvoiceSyntax.Cii,
            "CreditNote" => InvoiceSyntax.UblCreditNote,
            _ => InvoiceSyntax.UblInvoice,
        };

        return new RuleInput(InvoiceReader.Read(root, syntax), root, syntax);
    }

    /// <summary>The element at <paramref name="path"/>.</summary>
    private static XElement At(XElement root, string path)
    {
        var steps = path.Split('/');
        var element = root.DescendantsAndSelf().First(candidate => candidate.Name.LocalName == steps[0]);

        return steps.Skip(1).Aggregate(element, (parent, step) => parent.Elements().First(child => child.Name.LocalName == step));
    }
}
