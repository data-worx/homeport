// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The code-list rules on the parties: the schemes of their identifiers and addresses, and their country codes.</summary>
public static partial class CodeListRules
{
    // The scheme UBL's BR-CL-10 accepts besides ISO 6523 for the seller and the payee: a SEPA creditor identifier.
    private const string SepaScheme = "SEPA";

    /// <summary>BR-CL-10: the scheme of a party identifier (BT-29, BT-46, BT-60) is a code of ISO 6523 ICD.</summary>
    /// <remarks>
    /// UBL checks every party identification with a scheme, and accepts the
    /// scheme <c>SEPA</c> as well below the seller or the payee. CII checks
    /// every global identifier with a scheme but those of a product and of
    /// a ship-to party, which BR-CL-21 and BR-CL-26 check.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl10(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var list = CodeLists.Named(CodeLists.Iso6523Icd);
        if (input.IsCii)
        {
            var identifiers = input.Root.Descendants(Ram + "GlobalID").WithAttribute(SchemeIdAttribute)
                .Where(identifier => !identifier.Ancestors().Any(ancestor => ancestor.Name == Ram + "SpecifiedTradeProduct" || ancestor.Name == Ram + "ShipToTradeParty"));

            return NotCoded(identifiers, SchemeIdAttribute, CodeLists.Iso6523Icd, element => PartyPathOf(input, element));
        }

        return Below(input, Cac + "PartyIdentification", Cbc + "ID").WithAttribute(SchemeIdAttribute)
            .Where(identifier => !list.Contains(identifier.Attribute(SchemeIdAttribute)?.Value) && !IsSepaOfSellerOrPayee(identifier))
            .Select(identifier => PartyPathOf(input, identifier));
    }

    /// <summary>BR-CL-11: the scheme of a legal registration identifier (BT-30, BT-47, BT-61) is a code of ISO 6523 ICD.</summary>
    /// <remarks>
    /// UBL checks every company identifier of a legal entity with a scheme.
    /// CII checks every <c>ram:ID</c> with a scheme but one of a tax
    /// registration, whatever it identifies.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl11(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var identifiers = input.IsCii
            ? CiiIdentifiersOfBrCl11(input)
            : Below(input, Cac + "PartyLegalEntity", Cbc + "CompanyID").WithAttribute(SchemeIdAttribute);

        return NotCoded(identifiers, SchemeIdAttribute, CodeLists.Iso6523Icd, element => PartyPathOf(input, element));
    }

    /// <summary>BR-CL-14: a country code of an address (BT-40, BT-55, BT-69, BT-80) is a code of ISO 3166-1.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl14(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? input.Root.Descendants(Ram + "CountryID")
            : Below(input, Cac + "Country", Cbc + "IdentificationCode");

        return NotCoded(codes, CodeLists.Iso3166, element => PartyPathOf(input, element));
    }

    /// <summary>BR-CL-25: the scheme of an electronic address (BT-34, BT-49) is a code of the EAS list.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl25(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var addresses = input.IsCii
            ? Below(input, Ram + "URIUniversalCommunication", Ram + "URIID")
            : input.Root.Descendants(Cbc + "EndpointID");

        return NotCoded(addresses.WithAttribute(SchemeIdAttribute), SchemeIdAttribute, CodeLists.Eas, element => PartyPathOf(input, element));
    }

    /// <summary>BR-CL-26: the scheme of the deliver to location identifier (BT-71) is a code of ISO 6523 ICD.</summary>
    /// <remarks>UBL checks every delivery location's identifier with a scheme; CII the global identifiers of the ship-to party of the header.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl26(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var identifiers = input.IsCii
            ? Below(input, Ram + "ShipToTradeParty", Ram + "GlobalID").Where(identifier => identifier.Parent?.Parent?.Name == Ram + "ApplicableHeaderTradeDelivery")
            : Below(input, Cac + "DeliveryLocation", Cbc + "ID");

        return NotCoded(identifiers.WithAttribute(SchemeIdAttribute), SchemeIdAttribute, CodeLists.Iso6523Icd, element => LineOr(input, element, "$.deliveryInformation"));
    }

    /// <summary>The CII identifiers BR-CL-11 checks: every <c>ram:ID</c> with a scheme outside a tax registration.</summary>
    /// <remarks>
    /// The rule comes before BR-CL-15 in its pattern, so it also checks the
    /// identifier of an origin country that carries a scheme, which BR-CL-15
    /// then leaves alone.
    /// </remarks>
    private static IEnumerable<XElement> CiiIdentifiersOfBrCl11(RuleInput input) =>
        input.Root.Descendants(Ram + "ID").WithAttribute(SchemeIdAttribute)
            .Where(identifier => !identifier.Ancestors(Ram + "SpecifiedTaxRegistration").Any());

    /// <summary>Whether a UBL party identifier has the scheme SEPA, which BR-CL-10 accepts below the seller or the payee.</summary>
    private static bool IsSepaOfSellerOrPayee(XElement identifier) =>
        NormalizeSpace(identifier.Attribute(SchemeIdAttribute)?.Value) == SepaScheme
        && identifier.Ancestors().Any(ancestor => ancestor.Name == Cac + "AccountingSupplierParty" || ancestor.Name == Cac + "PayeeParty");
}
