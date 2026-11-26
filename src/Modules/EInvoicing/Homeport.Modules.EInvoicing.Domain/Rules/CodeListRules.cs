// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The code-list rules of EN 16931 (BR-CL-nn) as the EN 16931 validation
/// artefacts test them, one method per rule, which returns the JSON path of
/// every place the invoice breaks it.
/// </summary>
/// <remarks>
/// The rules are checked in the document: the schematron matches a code
/// wherever its element occurs — an amount's currency, a scheme identifier
/// of any party — and each syntax names other elements, so each method
/// follows the context of each syntax's own rule. A code is checked against
/// the lists of <see cref="CodeLists"/>, as <see cref="CodeList.Contains"/>
/// tests it. A finding points at the term of the model where there is one
/// for the code, else at the party or the invoice line it lies in, else at
/// the invoice.
/// </remarks>
public static partial class CodeListRules
{
    /// <summary>Every code-list rule, by its identifier.</summary>
    public static IReadOnlyDictionary<string, Func<RuleInput, IEnumerable<string>>> All { get; } =
        new Dictionary<string, Func<RuleInput, IEnumerable<string>>>(StringComparer.Ordinal)
        {
            ["BR-CL-01"] = BrCl01,
            ["BR-CL-03"] = BrCl03,
            ["BR-CL-04"] = BrCl04,
            ["BR-CL-05"] = BrCl05,
            ["BR-CL-06"] = BrCl06,
            ["BR-CL-07"] = BrCl07,
            ["BR-CL-08"] = BrCl08,
            ["BR-CL-10"] = BrCl10,
            ["BR-CL-11"] = BrCl11,
            ["BR-CL-13"] = BrCl13,
            ["BR-CL-14"] = BrCl14,
            ["BR-CL-15"] = BrCl15,
            ["BR-CL-16"] = BrCl16,
            ["BR-CL-17"] = BrCl17,
            ["BR-CL-18"] = BrCl18,
            ["BR-CL-19"] = BrCl19,
            ["BR-CL-20"] = BrCl20,
            ["BR-CL-21"] = BrCl21,
            ["BR-CL-22"] = BrCl22,
            ["BR-CL-23"] = BrCl23,
            ["BR-CL-24"] = BrCl24,
            ["BR-CL-25"] = BrCl25,
            ["BR-CL-26"] = BrCl26,
        };

    /// <summary>The elements of the document named <paramref name="name"/> whose parent is named <paramref name="parent"/>.</summary>
    private static IEnumerable<XElement> Below(RuleInput input, XName parent, XName name) =>
        input.Root.Descendants(name).Where(element => element.Parent?.Name == parent);

    /// <summary>The elements of <paramref name="elements"/> that carry the attribute, as a context <c>[@name]</c> selects them.</summary>
    private static IEnumerable<XElement> WithAttribute(this IEnumerable<XElement> elements, XName attribute) =>
        elements.Where(element => element.Attribute(attribute) is not null);

    /// <summary>The paths of the elements whose value <paramref name="list"/> does not contain.</summary>
    private static IEnumerable<string> NotCoded(IEnumerable<XElement> elements, string list, Func<XElement, string> pathOf) =>
        elements.Where(element => !CodeLists.Named(list).Contains(element.Value)).Select(pathOf);

    /// <summary>The paths of the elements whose attribute <paramref name="list"/> does not contain.</summary>
    private static IEnumerable<string> NotCoded(IEnumerable<XElement> elements, XName attribute, string list, Func<XElement, string> pathOf) =>
        elements.Where(element => !CodeLists.Named(list).Contains(element.Attribute(attribute)?.Value)).Select(pathOf);

    /// <summary>The path of the invoice line the element lies in, or <paramref name="headerPath"/> when it lies in none.</summary>
    private static string LineOr(RuleInput input, XElement element, string headerPath) =>
        input.LinePathOf(element) is var line && line == RulePaths.Invoice ? headerPath : line;

    /// <summary>The path of the party the element belongs to; of the invoice line or the invoice when it belongs to none the model holds.</summary>
    private static string PartyPathOf(RuleInput input, XElement element)
    {
        foreach (var ancestor in element.Ancestors())
        {
            var path = ancestor.Name.LocalName switch
            {
                "AccountingSupplierParty" or "SellerTradeParty" => "$.seller",
                "AccountingCustomerParty" or "BuyerTradeParty" => "$.buyer",
                "PayeeParty" or "PayeeTradeParty" => "$.payee",
                "TaxRepresentativeParty" or "SellerTaxRepresentativeTradeParty" => "$.sellerTaxRepresentative",
                "Delivery" or "ApplicableHeaderTradeDelivery" => "$.deliveryInformation",
                _ => null,
            };
            if (path is not null && (ancestor.Name.Namespace == Cac || ancestor.Name.Namespace == Ram))
            {
                return LineOr(input, element, path);
            }
        }

        return input.LinePathOf(element);
    }
}
