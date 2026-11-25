// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The core rules on references to other documents and on the delivery address.</summary>
public static partial class CoreRules
{
    /// <summary>BR-52: every additional supporting document carries a reference (BT-122).</summary>
    /// <remarks>
    /// Checked in the document, which writes other references in the same
    /// element: the schematron tests every one, an invoiced object identifier
    /// and, in CII, a line's references included, while the model holds only
    /// the supporting documents as BG-24.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br52(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            var supporting = (input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeAgreement")
                ?.Elements(Ram + "AdditionalReferencedDocument") ?? [])
                .Where(reference => reference.Element(Ram + "TypeCode")?.Value.Trim() == SupportingDocumentTypeCode)
                .ToList();

            return input.Root.Descendants(Ram + "AdditionalReferencedDocument")
                .Where(reference => IsBlank(reference.Element(Ram + "IssuerAssignedID")))
                .Select(reference => ReferencePathOf(input, supporting, reference));
        }

        var references = input.Root.Elements(Cac + "AdditionalDocumentReference").ToList();
        var model = references.Where(reference => !IsUblNonSupporting(input, reference)).ToList();

        return input.Root.Descendants(Cac + "AdditionalDocumentReference")
            .Where(reference => IsBlank(reference.Element(Cbc + "ID")))
            .Select(reference => ReferencePathOf(input, model, reference));
    }

    /// <summary>BR-55: every preceding invoice reference (BG-3) carries the preceding invoice number (BT-25).</summary>
    /// <remarks>
    /// Checked in the document: UBL asks every billing reference for an
    /// invoice document reference with an identifier, blank or not, which the
    /// model reads only where there is one; CII asks for one that is not blank.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br55(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string list = "$.precedingInvoiceReferences";
        if (input.IsCii)
        {
            var references = input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement")
                ?.Elements(Ram + "InvoiceReferencedDocument") ?? [];

            return references.Select((reference, index) => (reference, index))
                .Where(entry => IsBlank(entry.reference.Element(Ram + "IssuerAssignedID")))
                .Select(entry => $"{RulePaths.Item(list, entry.index)}.reference");
        }

        return input.Root.Descendants(Cac + "BillingReference")
            .Where(reference => !reference.Elements(Cac + "InvoiceDocumentReference").Elements(Cbc + "ID").Any())
            .Select(_ => list);
    }

    /// <summary>BR-57: every deliver to address (BG-15) carries a country code (BT-80).</summary>
    /// <remarks>
    /// Checked in the document: UBL asks every delivery location's address
    /// for the element, blank or not; CII asks the ship-to party's address
    /// for a code that is not blank.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br57(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.deliveryInformation.deliverToAddress.countryCode";
        if (input.IsCii)
        {
            var address = input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeDelivery")
                ?.Element(Ram + "ShipToTradeParty")?.Element(Ram + "PostalTradeAddress");

            return BrokenAt(address is not null && IsBlank(address.Element(Ram + "CountryID")), path);
        }

        return input.Root.Descendants(Cac + "Delivery").Elements(Cac + "DeliveryLocation").Elements(Cac + "Address")
            .Where(address => !address.Elements(Cac + "Country").Elements(Cbc + "IdentificationCode").Any())
            .Select(_ => path);
    }

    /// <summary>UNTDID 1001: the type code of a supporting document in CII (BG-24).</summary>
    private const string SupportingDocumentTypeCode = "916";

    /// <summary>UNTDID 1001: the type code of an invoiced object (BT-18) and, in a UBL CreditNote, of a project (BT-11).</summary>
    private const string InvoicedObjectTypeCode = "130";

    /// <summary>UNTDID 1001: the type code of a project reference in a UBL CreditNote (BT-11) and of a tender or lot in CII (BT-17).</summary>
    private const string ProjectTypeCode = "50";

    /// <summary>Whether a UBL additional document reference holds no supporting document but an invoiced object or, in a credit note, a project.</summary>
    private static bool IsUblNonSupporting(RuleInput input, XElement reference)
    {
        var type = reference.Element(Cbc + "DocumentTypeCode")?.Value.Trim();

        return type == InvoicedObjectTypeCode || (input.IsCreditNote && type == ProjectTypeCode);
    }

    /// <summary>The path of the supporting document the model reads from <paramref name="reference"/>, or of what else it reads it as.</summary>
    private static string ReferencePathOf(RuleInput input, IReadOnlyList<XElement> supporting, XElement reference)
    {
        if (IndexOf(supporting, reference) is var index and >= 0)
        {
            return $"{RulePaths.Item("$.additionalSupportingDocuments", index)}.reference";
        }

        var type = (input.IsCii ? reference.Element(Ram + "TypeCode") : reference.Element(Cbc + "DocumentTypeCode"))?.Value.Trim();

        return input.LinePathOf(reference) is var line && line != RulePaths.Invoice
            ? $"{line}.objectIdentifier"
            : type switch
            {
                InvoicedObjectTypeCode => "$.invoicedObjectIdentifier",
                ProjectTypeCode => input.IsCii ? "$.tenderOrLotReference" : "$.projectReference",
                _ => RulePaths.Invoice,
            };
    }
}
