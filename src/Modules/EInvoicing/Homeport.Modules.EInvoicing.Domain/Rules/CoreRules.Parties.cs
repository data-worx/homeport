// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The core rules on the parties: seller, buyer, payee and the seller's tax representative.</summary>
public static partial class CoreRules
{
    /// <summary>BR-06: the invoice names the seller (BT-27).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br06(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Seller?.Name is null, "$.seller.name");
    }

    /// <summary>BR-07: the invoice names the buyer (BT-44).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br07(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Buyer?.Name is null, "$.buyer.name");
    }

    /// <summary>BR-08: the invoice carries the seller's postal address (BG-5).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br08(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Seller?.PostalAddress is null, "$.seller.postalAddress");
    }

    /// <summary>BR-09: the seller's postal address carries a country code (BT-40).</summary>
    /// <remarks>UBL tests an address that is there; CII tests the country code whether the address is there or not.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br09(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var address = input.Invoice.Seller?.PostalAddress;

        return BrokenAt((input.IsCii || address is not null) && address?.CountryCode is null, "$.seller.postalAddress.countryCode");
    }

    /// <summary>BR-10: the invoice carries the buyer's postal address (BG-8).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br10(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Buyer?.PostalAddress is null, "$.buyer.postalAddress");
    }

    /// <summary>BR-11: the buyer's postal address carries a country code (BT-55).</summary>
    /// <remarks>UBL tests an address that is there; CII tests the country code whether the address is there or not.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br11(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var address = input.Invoice.Buyer?.PostalAddress;

        return BrokenAt((input.IsCii || address is not null) && address?.CountryCode is null, "$.buyer.postalAddress.countryCode");
    }

    /// <summary>BR-17: a payee (BG-10) has a name (BT-59) and is not the seller.</summary>
    /// <remarks>
    /// Checked in the document, where every identifier of either party is
    /// compared. UBL compares the payee's name with the seller's trading name
    /// and the party identifiers, the bank assigned creditor identifier
    /// included; CII compares the payee's name with the seller's name, the
    /// party identifiers and the legal registration identifiers.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br17(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.IsCii
            ? input.Root.Descendants(Ram + "PayeeTradeParty").Where(IsCiiPayeeBroken).Select(_ => "$.payee")
            : input.Root.Descendants(Cac + "PayeeParty").Where(IsUblPayeeBroken).Select(_ => "$.payee");
    }

    /// <summary>BR-18: the seller's tax representative (BG-11) has a name (BT-62).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br18(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.SellerTaxRepresentative is { Name: null }, "$.sellerTaxRepresentative.name");
    }

    /// <summary>BR-19: the seller's tax representative (BG-11) has a postal address (BG-12).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br19(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.SellerTaxRepresentative is { PostalAddress: null }, "$.sellerTaxRepresentative.postalAddress");
    }

    /// <summary>BR-20: the postal address of the seller's tax representative carries a country code (BT-69).</summary>
    /// <remarks>UBL tests an address that is there; CII tests the country code of every tax representative, with an address or without.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br20(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var representative = input.Invoice.SellerTaxRepresentative;
        var address = representative?.PostalAddress;

        return BrokenAt(
            (input.IsCii ? representative is not null : address is not null) && address?.CountryCode is null,
            "$.sellerTaxRepresentative.postalAddress.countryCode");
    }

    /// <summary>BR-56: the seller's tax representative (BG-11) has a VAT identifier (BT-63).</summary>
    /// <remarks>
    /// Checked in the document: UBL asks for a company identifier under the
    /// tax scheme VAT, CII for a tax registration in the scheme <c>VA</c>.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br56(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.sellerTaxRepresentative.vatIdentifier";

        return input.IsCii
            ? input.Root.Descendants(Ram + "SellerTaxRepresentativeTradeParty")
                .Where(party => IsBlank(party.Elements(Ram + "SpecifiedTaxRegistration").Elements(Ram + "ID")
                    .FirstOrDefault(id => id.Attribute(SchemeIdAttribute)?.Value == CiiVatRegistrationScheme)))
                .Select(_ => path)
            : input.Root.Descendants(Cac + "TaxRepresentativeParty")
                .Where(party => !party.Elements(Cac + "PartyTaxScheme").Where(IsUblVat).Elements(Cbc + "CompanyID").Any())
                .Select(_ => path);
    }

    /// <summary>BR-62: the seller's electronic address (BT-34) names its scheme.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br62(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return ElectronicAddressBroken(input, "SellerTradeParty", "AccountingSupplierParty", "$.seller.electronicAddress");
    }

    /// <summary>BR-63: the buyer's electronic address (BT-49) names its scheme.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br63(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return ElectronicAddressBroken(input, "BuyerTradeParty", "AccountingCustomerParty", "$.buyer.electronicAddress");
    }

    /// <summary>
    /// Whether a party's electronic address lacks its scheme. Checked in the
    /// document: UBL asks every endpoint for the attribute, CII the first
    /// communication of the party for a scheme that is not blank.
    /// </summary>
    private static string[] ElectronicAddressBroken(RuleInput input, string ciiParty, string ublParty, string path)
    {
        if (input.IsCii)
        {
            var party = input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeAgreement")?.Element(Ram + ciiParty);
            var communication = party?.Element(Ram + "URIUniversalCommunication");

            return BrokenAt(communication is not null && IsBlank(communication.Element(Ram + "URIID")?.Attribute(SchemeIdAttribute)), path);
        }

        var endpoints = input.Root.Elements(Cac + ublParty).Elements(Cac + "Party").Elements(Cbc + "EndpointID");

        return BrokenAt(endpoints.Any(endpoint => endpoint.Attribute(SchemeIdAttribute) is null), path);
    }

    /// <summary>Whether a UBL payee lacks a name or shares the seller's trading name or a party identifier.</summary>
    private static bool IsUblPayeeBroken(XElement payee)
    {
        var seller = payee.Parent?.Element(Cac + "AccountingSupplierParty")?.Element(Cac + "Party");
        var names = payee.Elements(Cac + "PartyName").Elements(Cbc + "Name").ToList();

        return names.Count == 0
            || Shares(names, seller?.Elements(Cac + "PartyName").Elements(Cbc + "Name"))
            || Shares(payee.Elements(Cac + "PartyIdentification").Elements(Cbc + "ID"), seller?.Elements(Cac + "PartyIdentification").Elements(Cbc + "ID"));
    }

    /// <summary>Whether a CII payee lacks a name or shares the seller's name, a party identifier or a legal registration identifier.</summary>
    private static bool IsCiiPayeeBroken(XElement payee)
    {
        var seller = payee.Parent?.Parent?.Element(Ram + "ApplicableHeaderTradeAgreement")?.Element(Ram + "SellerTradeParty");
        var names = payee.Elements(Ram + "Name").ToList();

        return names.Count == 0
            || Shares(names, seller?.Elements(Ram + "Name"))
            || Shares(payee.Elements(Ram + "ID"), seller?.Elements(Ram + "ID"))
            || Shares(
                payee.Elements(Ram + "SpecifiedLegalOrganization").Elements(Ram + "ID"),
                seller?.Elements(Ram + "SpecifiedLegalOrganization").Elements(Ram + "ID"));
    }

    /// <summary>Whether any value of the first elements equals any of the second, as a general comparison <c>=</c> of untyped values tests it.</summary>
    private static bool Shares(IEnumerable<XElement> first, IEnumerable<XElement>? second)
    {
        var values = second?.Select(element => element.Value).ToHashSet(StringComparer.Ordinal) ?? [];

        return first.Any(element => values.Contains(element.Value));
    }
}
