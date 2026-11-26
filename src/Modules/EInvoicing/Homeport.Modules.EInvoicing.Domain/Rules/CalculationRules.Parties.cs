// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The calculation rules on the parties: the prefix of a VAT identifier and the identification of the seller (BG-4).</summary>
public static partial class CalculationRules
{
    // The scheme UBL's party identification of the seller must not carry for BR-CO-26: a SEPA creditor identifier.
    private const string SepaScheme = "SEPA";

    /// <summary>BR-CO-09: every VAT identifier (BT-31, BT-48, BT-63) starts with a prefix of <see cref="VatIdentifierPrefixes"/>.</summary>
    /// <remarks>
    /// Checked in the document, on every tax scheme of VAT in UBL and every
    /// tax registration with the scheme <c>VA</c> in CII, whichever party
    /// carries it. The first two characters are compared exactly, untrimmed;
    /// a scheme without an identifier breaks the rule.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo09(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var identifiers = input.IsCii
            ? input.Root.Descendants(Ram + "SpecifiedTaxRegistration").Elements(Ram + "ID")
                .Where(identifier => identifier.Attribute(SchemeIdAttribute)?.Value == CiiVatRegistrationScheme)
                .Select(identifier => (Element: identifier, Value: identifier.Value))
            : input.Root.Descendants(Cac + "PartyTaxScheme").Where(IsUblVat)
                .Select(scheme => (Element: scheme, Value: scheme.Element(Cbc + "CompanyID")?.Value ?? string.Empty));

        return identifiers
            .Where(identifier => !VatIdentifierPrefixes.All.Contains(FirstCharacters(identifier.Value, 2)))
            .Select(identifier => VatIdentifierPathOf(input, identifier.Element));
    }

    /// <summary>BR-CO-26: the seller carries a seller identifier (BT-29), a legal registration identifier (BT-30) or a VAT identifier (BT-31).</summary>
    /// <remarks>
    /// Checked in the document. UBL does not count a party identification
    /// with the scheme SEPA; CII counts a global identifier and only a tax
    /// registration with the scheme <c>VA</c>.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo26(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var sellers = input.IsCii
            ? input.Root.Descendants(Ram + "SellerTradeParty").Where(seller => !IsCiiSellerIdentified(seller))
            : input.Root.Descendants(Cac + "AccountingSupplierParty").Where(seller => !IsUblSellerIdentified(seller));

        return sellers.Select(_ => "$.seller");
    }

    /// <summary>Whether a UBL seller carries one of the identifiers BR-CO-26 asks for.</summary>
    private static bool IsUblSellerIdentified(XElement seller)
    {
        var party = seller.Elements(Cac + "Party").ToList();

        return party.Elements(Cac + "PartyTaxScheme").Where(IsUblVat).Elements(Cbc + "CompanyID").Any()
            || party.Elements(Cac + "PartyIdentification").Elements(Cbc + "ID").Any(identifier => identifier.Attribute(SchemeIdAttribute)?.Value != SepaScheme)
            || party.Elements(Cac + "PartyLegalEntity").Elements(Cbc + "CompanyID").Any();
    }

    /// <summary>Whether a CII seller carries one of the identifiers BR-CO-26 asks for.</summary>
    private static bool IsCiiSellerIdentified(XElement seller) =>
        seller.Element(Ram + "ID") is not null
        || seller.Element(Ram + "GlobalID") is not null
        || seller.Elements(Ram + "SpecifiedLegalOrganization").Elements(Ram + "ID").Any()
        || seller.Elements(Ram + "SpecifiedTaxRegistration").Elements(Ram + "ID").Any(identifier => identifier.Attribute(SchemeIdAttribute)?.Value == CiiVatRegistrationScheme);

    /// <summary>The path of the VAT identifier of the party the element belongs to, or of the invoice for any other party.</summary>
    private static string VatIdentifierPathOf(RuleInput input, XElement element)
    {
        var (seller, buyer, representative) = input.IsCii
            ? (Ram + "SellerTradeParty", Ram + "BuyerTradeParty", Ram + "SellerTaxRepresentativeTradeParty")
            : (Cac + "AccountingSupplierParty", Cac + "AccountingCustomerParty", Cac + "TaxRepresentativeParty");
        var party = element.Ancestors().FirstOrDefault(ancestor => ancestor.Name == seller || ancestor.Name == buyer || ancestor.Name == representative);

        return party?.Name switch
        {
            null => RulePaths.Invoice,
            var name when name == seller => "$.seller.vatIdentifier",
            var name when name == buyer => "$.buyer.vatIdentifier",
            _ => "$.sellerTaxRepresentative.vatIdentifier",
        };
    }

    /// <summary>The first <paramref name="count"/> characters as XPath's <c>substring</c> counts them, by code point.</summary>
    private static string FirstCharacters(string value, int count) =>
        string.Concat(value.EnumerateRunes().Take(count).Select(rune => rune.ToString()));
}
