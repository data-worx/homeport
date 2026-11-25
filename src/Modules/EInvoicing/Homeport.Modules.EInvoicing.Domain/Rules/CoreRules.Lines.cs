// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Model;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The core rules on the invoice lines and their items.</summary>
public static partial class CoreRules
{
    /// <summary>BR-21: every invoice line carries a line identifier (BT-126).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br21(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(input.Invoice, line => line.Identifier is null, ".identifier");
    }

    /// <summary>BR-22: every invoice line carries an invoiced quantity (BT-129).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br22(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(input.Invoice, line => line.InvoicedQuantity is null, ".invoicedQuantity");
    }

    /// <summary>BR-23: the invoiced quantity of every invoice line carries its unit (BT-130).</summary>
    /// <remarks>Checked in the document: the schematron asks for the attribute, even a blank one, which the model drops.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br23(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LineElementsWhere(
            input,
            line => !(input.IsCii
                ? line.Elements(Ram + "SpecifiedLineTradeDelivery").Elements(Ram + "BilledQuantity")
                : line.Elements(Cbc + "InvoicedQuantity").Concat(line.Elements(Cbc + "CreditedQuantity")))
                .Any(quantity => quantity.Attribute(UnitCodeAttribute) is not null),
            ".invoicedQuantityUnitCode");
    }

    /// <summary>BR-24: every invoice line carries a net amount (BT-131).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br24(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(input.Invoice, line => line.NetAmount is null, ".netAmount");
    }

    /// <summary>BR-25: every invoice line carries an item name (BT-153).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br25(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(input.Invoice, line => line.Item?.Name is null, ".item.name");
    }

    /// <summary>BR-26: every invoice line carries an item net price (BT-146).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br26(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(input.Invoice, line => line.PriceDetails?.NetPrice is null, ".priceDetails.netPrice");
    }

    /// <summary>BR-27: the item net price (BT-146) of every invoice line is not negative.</summary>
    /// <remarks>A missing price breaks the rule too: the schematron compares an empty value, which is never at least 0.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br27(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LinesWhere(
            input.Invoice,
            line => !(decimal.TryParse(line.PriceDetails?.NetPrice, NumberStyles.Float, CultureInfo.InvariantCulture, out var price) && price >= 0),
            ".priceDetails.netPrice");
    }

    /// <summary>BR-28: the item gross price (BT-148) of every invoice line is not negative.</summary>
    /// <remarks>
    /// Checked in the document, where a line may carry several values: UBL
    /// compares the base amount of every price allowance, CII every amount of
    /// the gross price, and one that is not negative suffices.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br28(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LineElementsWhere(
            input,
            line =>
            {
                var prices = (input.IsCii
                    ? line.Elements(Ram + "SpecifiedLineTradeAgreement").Elements(Ram + "GrossPriceProductTradePrice").Elements(Ram + "ChargeAmount")
                    : line.Elements(Cac + "Price").Elements(Cac + "AllowanceCharge").Elements(Cbc + "BaseAmount")).ToList();

                return prices.Count > 0 && !AnyIsNotNegative(prices);
            },
            ".priceDetails.grossPrice");
    }

    /// <summary>BR-30: the period of every invoice line ends on or after its start (BT-134, BT-135).</summary>
    /// <remarks>
    /// Checked in the document, with the comparison of BR-29: UBL as xs:date,
    /// CII the values in format 102 as text.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br30(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LineElementsWhere(
            input,
            line => input.IsCii
                ? line.Elements(Ram + "SpecifiedLineTradeSettlement").Elements(Ram + "BillingSpecifiedPeriod").Any(IsCiiPeriodBroken)
                : line.Elements(Cac + "InvoicePeriod").Any(IsUblPeriodBroken),
            ".period");
    }

    /// <summary>BR-54: every item attribute (BG-32) carries a name (BT-160) and a value (BT-161).</summary>
    /// <remarks>Checked in the document: the schematron asks for the elements, even blank ones, which the model drops.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br54(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var (name, value) = input.IsCii ? (Ram + "Description", Ram + "Value") : (Cbc + "Name", Cbc + "Value");
        var attributes = input.IsCii ? input.Root.Descendants(Ram + "ApplicableProductCharacteristic") : input.Root.Descendants(Cac + "AdditionalItemProperty");

        return attributes
            .Where(attribute => attribute.Element(name) is null || attribute.Element(value) is null)
            .Select(attribute => ItemPathOf(input, attribute, ".attributes", attribute.ElementsBeforeSelf(attribute.Name).Count()));
    }

    /// <summary>BR-64: the item standard identifier (BT-157) names its scheme.</summary>
    /// <remarks>
    /// Checked in the document: UBL asks every identifier for the attribute;
    /// CII asks the identifier of a line, where there is one, for a scheme
    /// that is not blank.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br64(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return LineElementsWhere(
            input,
            line =>
            {
                if (input.IsCii)
                {
                    var identifiers = line.Elements(Ram + "SpecifiedTradeProduct").Elements(Ram + "GlobalID").ToList();

                    return identifiers.Count > 0 && IsBlank(identifiers[0].Attribute(SchemeIdAttribute));
                }

                return line.Elements(Cac + "Item").Elements(Cac + "StandardItemIdentification").Elements(Cbc + "ID")
                    .Any(id => id.Attribute(SchemeIdAttribute) is null);
            },
            ".item.standardIdentifier");
    }

    /// <summary>BR-65: every item classification identifier (BT-158) names its scheme.</summary>
    /// <remarks>
    /// Checked in the document: UBL asks every classification code for the
    /// attribute; CII asks every classification with a code for a code list
    /// that is not blank.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br65(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            return input.Root.Descendants(Ram + "DesignatedProductClassification")
                .Where(classification => classification.Element(Ram + "ClassCode") is { } code && IsBlank(code.Attribute(ListIdAttribute)))
                .Select(classification => ItemPathOf(input, classification, ".classificationIdentifiers", classification.ElementsBeforeSelf(classification.Name).Count()));
        }

        return input.Root.Descendants(Cbc + "ItemClassificationCode")
            .Where(code => code.Attribute(ListIdAttribute) is null)
            .Select(code => ItemPathOf(input, code, ".classificationIdentifiers", code.Parent!.ElementsBeforeSelf(code.Parent.Name).Count()));
    }

    /// <summary>The path below every line of the model that breaks a rule.</summary>
    private static IEnumerable<string> LinesWhere(Invoice invoice, Func<InvoiceLine, bool> broken, string member) =>
        invoice.Lines.Select((line, index) => (line, index)).Where(entry => broken(entry.line)).Select(entry => RulePaths.Line(entry.index) + member);

    /// <summary>The path below every line of the document that breaks a rule.</summary>
    private static IEnumerable<string> LineElementsWhere(RuleInput input, Func<XElement, bool> broken, string member) =>
        input.Lines.Select((line, index) => (line, index)).Where(entry => broken(entry.line)).Select(entry => RulePaths.Line(entry.index) + member);

    /// <summary>The path of an entry of a list of the item of the line <paramref name="element"/> lies in, or of the invoice outside any line.</summary>
    private static string ItemPathOf(RuleInput input, XElement element, string list, int index) =>
        input.LinePathOf(element) is var line && line == RulePaths.Invoice ? line : RulePaths.Item($"{line}.item{list}", index);
}
