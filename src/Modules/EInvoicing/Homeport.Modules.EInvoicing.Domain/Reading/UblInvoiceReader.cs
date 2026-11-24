// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Model;
using static Homeport.Modules.EInvoicing.Domain.Reading.XmlValues;

namespace Homeport.Modules.EInvoicing.Domain.Reading;

/// <summary>Reads a UBL Invoice or CreditNote into the semantic model of EN 16931.</summary>
/// <remarks>
/// The binding follows EN 16931-3-2 as the KoSIT XRechnung visualization
/// applies it. A credit note differs from an invoice in the names of its type
/// code, its lines and their quantity, carries the payment due date in its
/// payment means and the project reference as an additional document of the
/// type <c>50</c>.
/// </remarks>
internal static partial class UblInvoiceReader
{
    private const string UblDateFormat = "yyyy-MM-dd";

    // UNTDID 1001: the document type codes an additional document reference
    // takes for an invoiced object (BT-18) and, in a credit note, for the
    // project reference (BT-11); UBL-SR-43.
    private const string InvoicedObjectTypeCode = "130";
    private const string ProjectTypeCode = "50";

    // The tax scheme of VAT, and the scheme a party identification takes for
    // the bank assigned creditor identifier (BT-90).
    private const string VatScheme = "VAT";
    private const string SepaScheme = "SEPA";

    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XName CurrencyIdAttribute = "currencyID";
    private static readonly XName UnitCodeAttribute = "unitCode";
    private static readonly XName NameAttribute = "name";
    private static readonly XName SchemeIdAttribute = "schemeID";

    /// <summary>Reads <paramref name="root"/>, the root element of a UBL Invoice or CreditNote.</summary>
    /// <param name="root">The root element.</param>
    /// <param name="isCreditNote">Whether it is a CreditNote rather than an Invoice.</param>
    public static Invoice Read(XElement root, bool isCreditNote)
    {
        var currency = Token(root.Element(Cbc + "DocumentCurrencyCode"));
        var accountingCurrency = Token(root.Element(Cbc + "TaxCurrencyCode"));
        var references = root.Elements(Cac + "AdditionalDocumentReference").ToList();

        return new Invoice
        {
            Number = Identifier(root.Element(Cbc + "ID")),
            IssueDate = Date(Token(root.Element(Cbc + "IssueDate")), UblDateFormat),
            TypeCode = Token(root.Element(Cbc + (isCreditNote ? "CreditNoteTypeCode" : "InvoiceTypeCode"))),
            CurrencyCode = currency,
            VatAccountingCurrencyCode = accountingCurrency,
            VatPointDate = Date(Token(root.Element(Cbc + "TaxPointDate")), UblDateFormat),
            VatPointDateCode = Token(root.Element(Cac + "InvoicePeriod")?.Element(Cbc + "DescriptionCode")),
            PaymentDueDate = Date(
                Token(isCreditNote
                    ? root.Elements(Cac + "PaymentMeans").Elements(Cbc + "PaymentDueDate").FirstOrDefault()
                    : root.Element(Cbc + "DueDate")),
                UblDateFormat),
            BuyerReference = Text(root.Element(Cbc + "BuyerReference")),
            ProjectReference = Token(isCreditNote
                ? references.FirstOrDefault(reference => HasTypeCode(reference, ProjectTypeCode))?.Element(Cbc + "ID")
                : root.Element(Cac + "ProjectReference")?.Element(Cbc + "ID")),
            ContractReference = Token(root.Element(Cac + "ContractDocumentReference")?.Element(Cbc + "ID")),
            PurchaseOrderReference = Token(root.Element(Cac + "OrderReference")?.Element(Cbc + "ID")),
            SalesOrderReference = Token(root.Element(Cac + "OrderReference")?.Element(Cbc + "SalesOrderID")),
            ReceivingAdviceReference = Token(root.Element(Cac + "ReceiptDocumentReference")?.Element(Cbc + "ID")),
            DespatchAdviceReference = Token(root.Element(Cac + "DespatchDocumentReference")?.Element(Cbc + "ID")),
            TenderOrLotReference = Token(root.Element(Cac + "OriginatorDocumentReference")?.Element(Cbc + "ID")),
            InvoicedObjectIdentifier = Identifier(references.FirstOrDefault(reference => HasTypeCode(reference, InvoicedObjectTypeCode))?.Element(Cbc + "ID")),
            BuyerAccountingReference = Text(root.Element(Cbc + "AccountingCost")),
            PaymentTerms = Text(root.Element(Cac + "PaymentTerms")?.Element(Cbc + "Note")),
            Notes = [.. root.Elements(Cbc + "Note").Select(InvoiceNoteOf)],
            ProcessControl = new ProcessControl
            {
                BusinessProcessType = Text(root.Element(Cbc + "ProfileID")),
                SpecificationIdentifier = Identifier(root.Element(Cbc + "CustomizationID")),
            },
            PrecedingInvoiceReferences = [.. root.Elements(Cac + "BillingReference").Elements(Cac + "InvoiceDocumentReference").Select(reference => new PrecedingInvoiceReference
            {
                Reference = Token(reference.Element(Cbc + "ID")),
                IssueDate = Date(Token(reference.Element(Cbc + "IssueDate")), UblDateFormat),
            })],
            Seller = SellerOf(root.Element(Cac + "AccountingSupplierParty")?.Element(Cac + "Party")),
            Buyer = BuyerOf(root.Element(Cac + "AccountingCustomerParty")?.Element(Cac + "Party")),
            Payee = PayeeOf(root.Element(Cac + "PayeeParty")),
            SellerTaxRepresentative = TaxRepresentativeOf(root.Element(Cac + "TaxRepresentativeParty")),
            DeliveryInformation = DeliveryOf(root.Element(Cac + "Delivery")),
            InvoicingPeriod = root.Element(Cac + "InvoicePeriod") is { } period && (period.Element(Cbc + "StartDate") is not null || period.Element(Cbc + "EndDate") is not null)
                ? new InvoicingPeriod
                {
                    StartDate = Date(Token(period.Element(Cbc + "StartDate")), UblDateFormat),
                    EndDate = Date(Token(period.Element(Cbc + "EndDate")), UblDateFormat),
                }
                : null,
            PaymentInstructions = PaymentInstructionsOf(root),
            DocumentLevelAllowances = [.. AllowanceCharges(root, isCharge: false).Select(allowance => new DocumentLevelAllowance
            {
                Amount = Token(allowance.Element(Cbc + "Amount")),
                BaseAmount = Token(allowance.Element(Cbc + "BaseAmount")),
                Percentage = Token(allowance.Element(Cbc + "MultiplierFactorNumeric")),
                VatCategoryCode = Token(VatCategory(allowance, "TaxCategory")?.Element(Cbc + "ID")),
                VatRate = Token(VatCategory(allowance, "TaxCategory")?.Element(Cbc + "Percent")),
                Reason = Text(allowance.Element(Cbc + "AllowanceChargeReason")),
                ReasonCode = Token(allowance.Element(Cbc + "AllowanceChargeReasonCode")),
            })],
            DocumentLevelCharges = [.. AllowanceCharges(root, isCharge: true).Select(charge => new DocumentLevelCharge
            {
                Amount = Token(charge.Element(Cbc + "Amount")),
                BaseAmount = Token(charge.Element(Cbc + "BaseAmount")),
                Percentage = Token(charge.Element(Cbc + "MultiplierFactorNumeric")),
                VatCategoryCode = Token(VatCategory(charge, "TaxCategory")?.Element(Cbc + "ID")),
                VatRate = Token(VatCategory(charge, "TaxCategory")?.Element(Cbc + "Percent")),
                Reason = Text(charge.Element(Cbc + "AllowanceChargeReason")),
                ReasonCode = Token(charge.Element(Cbc + "AllowanceChargeReasonCode")),
            })],
            DocumentTotals = TotalsOf(root, currency, accountingCurrency),
            VatBreakdowns = [.. root.Elements(Cac + "TaxTotal").Elements(Cac + "TaxSubtotal").Select(subtotal => new VatBreakdown
            {
                TaxableAmount = Token(subtotal.Element(Cbc + "TaxableAmount")),
                TaxAmount = Token(subtotal.Element(Cbc + "TaxAmount")),
                CategoryCode = Token(subtotal.Element(Cac + "TaxCategory")?.Element(Cbc + "ID")),
                Rate = Token(subtotal.Element(Cac + "TaxCategory")?.Element(Cbc + "Percent")),
                ExemptionReason = Text(subtotal.Element(Cac + "TaxCategory")?.Element(Cbc + "TaxExemptionReason")),
                ExemptionReasonCode = Token(subtotal.Element(Cac + "TaxCategory")?.Element(Cbc + "TaxExemptionReasonCode")),
            })],
            AdditionalSupportingDocuments = [.. references
                .Where(reference => !HasTypeCode(reference, InvoicedObjectTypeCode) && !(isCreditNote && HasTypeCode(reference, ProjectTypeCode)))
                .Select(reference => new AdditionalSupportingDocument
                {
                    Reference = Token(reference.Element(Cbc + "ID")),
                    Description = Text(reference.Element(Cbc + "DocumentDescription")),
                    ExternalLocation = Text(reference.Element(Cac + "Attachment")?.Element(Cac + "ExternalReference")?.Element(Cbc + "URI")),
                    AttachedDocument = BinaryObject(reference.Element(Cac + "Attachment")?.Element(Cbc + "EmbeddedDocumentBinaryObject")),
                })],
            Lines = [.. root.Elements(Cac + (isCreditNote ? "CreditNoteLine" : "InvoiceLine")).Select(line => LineOf(line, isCreditNote))],
        };
    }

    /// <summary>A note, with the subject code written in front of it as <c>#CODE#</c>, the convention of XRechnung and Peppol.</summary>
    private static InvoiceNote InvoiceNoteOf(XElement note)
    {
        var text = Text(note);
        var match = text is null ? null : SubjectCodePrefix().Match(text);

        return match is { Success: true }
            ? new InvoiceNote { SubjectCode = match.Groups["code"].Value, Note = Text(match.Groups["note"].Value) }
            : new InvoiceNote { Note = text };
    }

    /// <summary>The seller (BG-4).</summary>
    private static Seller? SellerOf(XElement? party) => party is null ? null : new Seller
    {
        Name = Text(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "RegistrationName")),
        TradingName = Text(party.Element(Cac + "PartyName")?.Element(Cbc + "Name")),
        Identifiers = [.. PartyIdentifiers(party).Where(id => !IsSepa(id)).Select(id => Identifier(id)!)],
        LegalRegistrationIdentifier = Identifier(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "CompanyID")),
        VatIdentifier = Identifier(TaxSchemes(party).FirstOrDefault(IsVat)?.Element(Cbc + "CompanyID"), schemeId: null),
        TaxRegistrationIdentifier = Identifier(TaxSchemes(party).FirstOrDefault(scheme => !IsVat(scheme))?.Element(Cbc + "CompanyID"), schemeId: null),
        AdditionalLegalInformation = Text(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "CompanyLegalForm")),
        ElectronicAddress = Identifier(party.Element(Cbc + "EndpointID")),
        PostalAddress = AddressOf(party.Element(Cac + "PostalAddress")) is { } address
            ? new SellerPostalAddress
            {
                AddressLine1 = address.Line1,
                AddressLine2 = address.Line2,
                AddressLine3 = address.Line3,
                City = address.City,
                PostCode = address.PostCode,
                CountrySubdivision = address.Subdivision,
                CountryCode = address.CountryCode,
            }
            : null,
        Contact = party.Element(Cac + "Contact") is { } contact
            ? new SellerContact
            {
                ContactPoint = Text(contact.Element(Cbc + "Name")),
                TelephoneNumber = Text(contact.Element(Cbc + "Telephone")),
                EmailAddress = Text(contact.Element(Cbc + "ElectronicMail")),
            }
            : null,
    };

    /// <summary>The buyer (BG-7).</summary>
    private static Buyer? BuyerOf(XElement? party) => party is null ? null : new Buyer
    {
        Name = Text(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "RegistrationName")),
        TradingName = Text(party.Element(Cac + "PartyName")?.Element(Cbc + "Name")),
        Identifier = Identifier(PartyIdentifiers(party).FirstOrDefault()),
        LegalRegistrationIdentifier = Identifier(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "CompanyID")),
        VatIdentifier = Identifier(TaxSchemes(party).FirstOrDefault(IsVat)?.Element(Cbc + "CompanyID"), schemeId: null),
        ElectronicAddress = Identifier(party.Element(Cbc + "EndpointID")),
        PostalAddress = AddressOf(party.Element(Cac + "PostalAddress")) is { } address
            ? new BuyerPostalAddress
            {
                AddressLine1 = address.Line1,
                AddressLine2 = address.Line2,
                AddressLine3 = address.Line3,
                City = address.City,
                PostCode = address.PostCode,
                CountrySubdivision = address.Subdivision,
                CountryCode = address.CountryCode,
            }
            : null,
        Contact = party.Element(Cac + "Contact") is { } contact
            ? new BuyerContact
            {
                ContactPoint = Text(contact.Element(Cbc + "Name")),
                TelephoneNumber = Text(contact.Element(Cbc + "Telephone")),
                EmailAddress = Text(contact.Element(Cbc + "ElectronicMail")),
            }
            : null,
    };

    /// <summary>The payee (BG-10).</summary>
    private static Payee? PayeeOf(XElement? party) => party is null ? null : new Payee
    {
        Name = Text(party.Element(Cac + "PartyName")?.Element(Cbc + "Name")),
        Identifier = Identifier(PartyIdentifiers(party).FirstOrDefault(id => !IsSepa(id))),
        LegalRegistrationIdentifier = Identifier(party.Element(Cac + "PartyLegalEntity")?.Element(Cbc + "CompanyID")),
    };

    /// <summary>The tax representative of the seller (BG-11).</summary>
    private static SellerTaxRepresentativeParty? TaxRepresentativeOf(XElement? party) => party is null ? null : new SellerTaxRepresentativeParty
    {
        Name = Text(party.Element(Cac + "PartyName")?.Element(Cbc + "Name")),
        VatIdentifier = Identifier(TaxSchemes(party).FirstOrDefault(IsVat)?.Element(Cbc + "CompanyID"), schemeId: null),
        PostalAddress = AddressOf(party.Element(Cac + "PostalAddress")) is { } address
            ? new SellerTaxRepresentativePostalAddress
            {
                AddressLine1 = address.Line1,
                AddressLine2 = address.Line2,
                AddressLine3 = address.Line3,
                City = address.City,
                PostCode = address.PostCode,
                CountrySubdivision = address.Subdivision,
                CountryCode = address.CountryCode,
            }
            : null,
    };

    /// <summary>The delivery information (BG-13).</summary>
    private static DeliveryInformation? DeliveryOf(XElement? delivery) => delivery is null ? null : new DeliveryInformation
    {
        DeliverToPartyName = Text(delivery.Element(Cac + "DeliveryParty")?.Element(Cac + "PartyName")?.Element(Cbc + "Name")),
        DeliverToLocationIdentifier = Identifier(delivery.Element(Cac + "DeliveryLocation")?.Element(Cbc + "ID")),
        ActualDeliveryDate = Date(Token(delivery.Element(Cbc + "ActualDeliveryDate")), UblDateFormat),
        DeliverToAddress = AddressOf(delivery.Element(Cac + "DeliveryLocation")?.Element(Cac + "Address")) is { } address
            ? new DeliverToAddress
            {
                AddressLine1 = address.Line1,
                AddressLine2 = address.Line2,
                AddressLine3 = address.Line3,
                City = address.City,
                PostCode = address.PostCode,
                CountrySubdivision = address.Subdivision,
                CountryCode = address.CountryCode,
            }
            : null,
    };

    /// <summary>
    /// The payment instructions (BG-16), gathered from every payment means:
    /// EN 16931 knows one, UBL repeats the element for each account.
    /// </summary>
    private static PaymentInstructions? PaymentInstructionsOf(XElement root)
    {
        var means = root.Elements(Cac + "PaymentMeans").ToList();
        var mandate = means.Elements(Cac + "PaymentMandate").FirstOrDefault();
        var creditor = new[] { root.Element(Cac + "PayeeParty"), root.Element(Cac + "AccountingSupplierParty")?.Element(Cac + "Party") }
            .SelectMany(party => party is null ? [] : PartyIdentifiers(party))
            .FirstOrDefault(IsSepa);
        if (means.Count == 0 && mandate is null && creditor is null)
        {
            return null;
        }

        var code = means.Elements(Cbc + "PaymentMeansCode").FirstOrDefault();

        return new PaymentInstructions
        {
            PaymentMeansTypeCode = Token(code),
            PaymentMeansText = Text(code?.Attribute(NameAttribute)?.Value),
            RemittanceInformation = Text(means.Elements(Cbc + "PaymentID").FirstOrDefault()),
            CreditTransfers = [.. means.Elements(Cac + "PayeeFinancialAccount").Select(account => new CreditTransfer
            {
                PaymentAccountIdentifier = Identifier(account.Element(Cbc + "ID")),
                PaymentAccountName = Text(account.Element(Cbc + "Name")),
                PaymentServiceProviderIdentifier = Identifier(account.Element(Cac + "FinancialInstitutionBranch")?.Element(Cbc + "ID")),
            })],
            PaymentCard = means.Elements(Cac + "CardAccount").FirstOrDefault() is { } card
                ? new PaymentCardInformation
                {
                    PrimaryAccountNumber = Text(card.Element(Cbc + "PrimaryAccountNumberID")),
                    HolderName = Text(card.Element(Cbc + "HolderName")),
                }
                : null,
            DirectDebit = mandate is not null || creditor is not null
                ? new DirectDebit
                {
                    MandateReferenceIdentifier = Identifier(mandate?.Element(Cbc + "ID")),
                    BankAssignedCreditorIdentifier = Identifier(creditor, schemeId: null),
                    DebitedAccountIdentifier = Identifier(mandate?.Element(Cac + "PayerFinancialAccount")?.Element(Cbc + "ID")),
                }
                : null,
        };
    }

    /// <summary>
    /// The document totals (BG-22). The total VAT amount is the tax total in
    /// the invoice currency, or the one without a currency; the one in the
    /// accounting currency is the tax total in that currency.
    /// </summary>
    private static DocumentTotals? TotalsOf(XElement root, string? currency, string? accountingCurrency)
    {
        var totals = root.Element(Cac + "LegalMonetaryTotal");
        var taxAmounts = root.Elements(Cac + "TaxTotal").Elements(Cbc + "TaxAmount").ToList();
        if (totals is null && taxAmounts.Count == 0)
        {
            return null;
        }

        return new DocumentTotals
        {
            SumOfLineNetAmounts = Token(totals?.Element(Cbc + "LineExtensionAmount")),
            SumOfAllowances = Token(totals?.Element(Cbc + "AllowanceTotalAmount")),
            SumOfCharges = Token(totals?.Element(Cbc + "ChargeTotalAmount")),
            TotalWithoutVat = Token(totals?.Element(Cbc + "TaxExclusiveAmount")),
            TotalVatAmount = Token(taxAmounts.FirstOrDefault(amount => Token(amount.Attribute(CurrencyIdAttribute)) is not { } of || of == currency)),
            TotalVatAmountInAccountingCurrency = accountingCurrency is null || accountingCurrency == currency
                ? null
                : Token(taxAmounts.FirstOrDefault(amount => Token(amount.Attribute(CurrencyIdAttribute)) == accountingCurrency)),
            TotalWithVat = Token(totals?.Element(Cbc + "TaxInclusiveAmount")),
            PaidAmount = Token(totals?.Element(Cbc + "PrepaidAmount")),
            RoundingAmount = Token(totals?.Element(Cbc + "PayableRoundingAmount")),
            AmountDueForPayment = Token(totals?.Element(Cbc + "PayableAmount")),
        };
    }

    /// <summary>A line (BG-25).</summary>
    private static InvoiceLine LineOf(XElement line, bool isCreditNote)
    {
        var quantity = line.Element(Cbc + (isCreditNote ? "CreditedQuantity" : "InvoicedQuantity"));
        var period = line.Element(Cac + "InvoicePeriod");
        var price = line.Element(Cac + "Price");
        var priceAllowance = price?.Elements(Cac + "AllowanceCharge").FirstOrDefault(allowance => IsFalse(allowance.Element(Cbc + "ChargeIndicator")));
        var item = line.Element(Cac + "Item");
        var vat = item is null ? null : VatCategory(item, "ClassifiedTaxCategory");

        return new InvoiceLine
        {
            Identifier = Identifier(line.Element(Cbc + "ID")),
            Note = Text(line.Element(Cbc + "Note")),
            ObjectIdentifier = Identifier(line.Elements(Cac + "DocumentReference").FirstOrDefault(reference => HasTypeCode(reference, InvoicedObjectTypeCode))?.Element(Cbc + "ID")),
            InvoicedQuantity = Token(quantity),
            InvoicedQuantityUnitCode = Token(quantity?.Attribute(UnitCodeAttribute)),
            NetAmount = Token(line.Element(Cbc + "LineExtensionAmount")),
            PurchaseOrderLineReference = Token(line.Element(Cac + "OrderLineReference")?.Element(Cbc + "LineID")),
            BuyerAccountingReference = Text(line.Element(Cbc + "AccountingCost")),
            Period = period is null ? null : new InvoiceLinePeriod
            {
                StartDate = Date(Token(period.Element(Cbc + "StartDate")), UblDateFormat),
                EndDate = Date(Token(period.Element(Cbc + "EndDate")), UblDateFormat),
            },
            Allowances = [.. AllowanceCharges(line, isCharge: false).Select(allowance => new InvoiceLineAllowance
            {
                Amount = Token(allowance.Element(Cbc + "Amount")),
                BaseAmount = Token(allowance.Element(Cbc + "BaseAmount")),
                Percentage = Token(allowance.Element(Cbc + "MultiplierFactorNumeric")),
                Reason = Text(allowance.Element(Cbc + "AllowanceChargeReason")),
                ReasonCode = Token(allowance.Element(Cbc + "AllowanceChargeReasonCode")),
            })],
            Charges = [.. AllowanceCharges(line, isCharge: true).Select(charge => new InvoiceLineCharge
            {
                Amount = Token(charge.Element(Cbc + "Amount")),
                BaseAmount = Token(charge.Element(Cbc + "BaseAmount")),
                Percentage = Token(charge.Element(Cbc + "MultiplierFactorNumeric")),
                Reason = Text(charge.Element(Cbc + "AllowanceChargeReason")),
                ReasonCode = Token(charge.Element(Cbc + "AllowanceChargeReasonCode")),
            })],
            PriceDetails = price is null ? null : new PriceDetails
            {
                NetPrice = Token(price.Element(Cbc + "PriceAmount")),
                PriceDiscount = Token(priceAllowance?.Element(Cbc + "Amount")),
                GrossPrice = Token(priceAllowance?.Element(Cbc + "BaseAmount")),
                BaseQuantity = Token(price.Element(Cbc + "BaseQuantity")),
                BaseQuantityUnitCode = Token(price.Element(Cbc + "BaseQuantity")?.Attribute(UnitCodeAttribute)),
            },
            VatInformation = vat is null ? null : new LineVatInformation
            {
                VatCategoryCode = Token(vat.Element(Cbc + "ID")),
                VatRate = Token(vat.Element(Cbc + "Percent")),
            },
            Item = item is null ? null : new ItemInformation
            {
                Name = Text(item.Element(Cbc + "Name")),
                Description = Text(item.Element(Cbc + "Description")),
                SellersIdentifier = Identifier(item.Element(Cac + "SellersItemIdentification")?.Element(Cbc + "ID")),
                BuyersIdentifier = Identifier(item.Element(Cac + "BuyersItemIdentification")?.Element(Cbc + "ID")),
                StandardIdentifier = Identifier(item.Element(Cac + "StandardItemIdentification")?.Element(Cbc + "ID")),
                ClassificationIdentifiers = [.. item.Elements(Cac + "CommodityClassification").Elements(Cbc + "ItemClassificationCode").Select(Identifier).OfType<Identifier>()],
                CountryOfOrigin = Token(item.Element(Cac + "OriginCountry")?.Element(Cbc + "IdentificationCode")),
                Attributes = [.. item.Elements(Cac + "AdditionalItemProperty").Select(property => new ItemAttribute
                {
                    Name = Text(property.Element(Cbc + "Name")),
                    Value = Text(property.Element(Cbc + "Value")),
                })],
            },
        };
    }

    /// <summary>The parts of a postal address, which four groups share.</summary>
    private static AddressParts? AddressOf(XElement? address) => address is null ? null : new AddressParts(
        Text(address.Element(Cbc + "StreetName")),
        Text(address.Element(Cbc + "AdditionalStreetName")),
        Text(address.Element(Cac + "AddressLine")?.Element(Cbc + "Line")),
        Text(address.Element(Cbc + "CityName")),
        Text(address.Element(Cbc + "PostalZone")),
        Text(address.Element(Cbc + "CountrySubentity")),
        Token(address.Element(Cac + "Country")?.Element(Cbc + "IdentificationCode")));

    /// <summary>The allowances or the charges directly below <paramref name="parent"/>.</summary>
    private static IEnumerable<XElement> AllowanceCharges(XElement parent, bool isCharge) =>
        parent.Elements(Cac + "AllowanceCharge").Where(element =>
            isCharge ? IsTrue(element.Element(Cbc + "ChargeIndicator")) : IsFalse(element.Element(Cbc + "ChargeIndicator")));

    /// <summary>The tax category of VAT named <paramref name="name"/> below <paramref name="parent"/>.</summary>
    private static XElement? VatCategory(XElement parent, string name) =>
        parent.Elements(Cac + name).FirstOrDefault(IsVat);

    /// <summary>The identifiers of a party.</summary>
    private static IEnumerable<XElement> PartyIdentifiers(XElement party) =>
        party.Elements(Cac + "PartyIdentification").Elements(Cbc + "ID");

    /// <summary>The tax schemes of a party.</summary>
    private static IEnumerable<XElement> TaxSchemes(XElement party) => party.Elements(Cac + "PartyTaxScheme");

    /// <summary>Whether an element names the tax scheme VAT, as a party tax scheme or a tax category does.</summary>
    private static bool IsVat(XElement element) =>
        string.Equals(Token(element.Element(Cac + "TaxScheme")?.Element(Cbc + "ID")), VatScheme, StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether a party identification is the bank assigned creditor identifier.</summary>
    private static bool IsSepa(XElement id) => Token(id.Attribute(SchemeIdAttribute)) == SepaScheme;

    /// <summary>Whether a document reference has the given document type code.</summary>
    private static bool HasTypeCode(XElement reference, string code) => Token(reference.Element(Cbc + "DocumentTypeCode")) == code;

    /// <summary>The subject code UNTDID 4451 in front of a note, as in <c>#AAI#text</c>.</summary>
    [GeneratedRegex("^#(?<code>[A-Z]{3})#(?<note>.*)$", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex SubjectCodePrefix();
}
