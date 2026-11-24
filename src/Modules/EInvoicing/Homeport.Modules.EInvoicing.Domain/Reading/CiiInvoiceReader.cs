// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Model;
using static Homeport.Modules.EInvoicing.Domain.Reading.XmlValues;

namespace Homeport.Modules.EInvoicing.Domain.Reading;

/// <summary>Reads a CII CrossIndustryInvoice into the semantic model of EN 16931.</summary>
/// <remarks>
/// The binding follows EN 16931-3-3 as the KoSIT XRechnung visualization
/// applies it. Where CII codes a term from another code list than the
/// semantic model, the model holds the code of the semantic model.
/// </remarks>
internal static class CiiInvoiceReader
{
    // Date format 102 (UNTDID 2379), the one EN 16931 allows in CII.
    private const string DateFormatCode = "102";
    private const string CiiDateFormat = "yyyyMMdd";

    // UNTDID 1001: the type codes of an additional referenced document for a
    // tender or lot (BT-17), an invoiced object (BT-18) and a supporting
    // document (BG-24).
    private const string TenderTypeCode = "50";
    private const string InvoicedObjectTypeCode = "130";
    private const string SupportingDocumentTypeCode = "916";

    // The schemes a tax registration takes for a VAT identifier and for a
    // tax registration identifier.
    private const string VatRegistrationScheme = "VA";
    private const string TaxRegistrationScheme = "FC";

    private static readonly XNamespace Rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private static readonly XNamespace Ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private static readonly XNamespace Udt = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";
    private static readonly XNamespace Qdt = "urn:un:unece:uncefact:data:standard:QualifiedDataType:100";
    private static readonly XName FormatAttribute = "format";
    private static readonly XName CurrencyIdAttribute = "currencyID";
    private static readonly XName UnitCodeAttribute = "unitCode";
    private static readonly XName SchemeIdAttribute = "schemeID";

    // BR-CL-06: CII codes the VAT point date code (BT-8) in UNTDID 2475, the
    // semantic model in UNTDID 2005. Each code stands for the same date.
    private static readonly Dictionary<string, string> VatPointDateCodes = new(StringComparer.Ordinal)
    {
        ["5"] = "3",
        ["29"] = "35",
        ["72"] = "432",
    };

    /// <summary>Reads <paramref name="root"/>, the root element of a CII CrossIndustryInvoice.</summary>
    /// <param name="root">The root element.</param>
    public static Invoice Read(XElement root)
    {
        var context = root.Element(Rsm + "ExchangedDocumentContext");
        var document = root.Element(Rsm + "ExchangedDocument");
        var transaction = root.Element(Rsm + "SupplyChainTradeTransaction");
        var agreement = transaction?.Element(Ram + "ApplicableHeaderTradeAgreement");
        var delivery = transaction?.Element(Ram + "ApplicableHeaderTradeDelivery");
        var settlement = transaction?.Element(Ram + "ApplicableHeaderTradeSettlement");
        var currency = Token(settlement?.Element(Ram + "InvoiceCurrencyCode"));
        var accountingCurrency = Token(settlement?.Element(Ram + "TaxCurrencyCode"));
        var taxes = Children(settlement, "ApplicableTradeTax").ToList();
        var terms = Children(settlement, "SpecifiedTradePaymentTerms").ToList();
        var references = Children(agreement, "AdditionalReferencedDocument").ToList();

        return new Invoice
        {
            Number = Identifier(document?.Element(Ram + "ID")),
            IssueDate = DateTimeOf(document?.Element(Ram + "IssueDateTime")),
            TypeCode = Token(document?.Element(Ram + "TypeCode")),
            CurrencyCode = currency,
            VatAccountingCurrencyCode = accountingCurrency,
            VatPointDate = taxes.Elements(Ram + "TaxPointDate").Select(DateOf).FirstOrDefault(date => date is not null),
            VatPointDateCode = Token(taxes.Elements(Ram + "DueDateTypeCode").FirstOrDefault()) is { } code
                ? VatPointDateCodes.GetValueOrDefault(code, code)
                : null,
            PaymentDueDate = terms.Elements(Ram + "DueDateDateTime").Select(DateTimeOf).FirstOrDefault(date => date is not null),
            BuyerReference = Text(agreement?.Element(Ram + "BuyerReference")),
            ProjectReference = Token(agreement?.Element(Ram + "SpecifiedProcuringProject")?.Element(Ram + "ID")),
            ContractReference = Token(agreement?.Element(Ram + "ContractReferencedDocument")?.Element(Ram + "IssuerAssignedID")),
            PurchaseOrderReference = Token(agreement?.Element(Ram + "BuyerOrderReferencedDocument")?.Element(Ram + "IssuerAssignedID")),
            SalesOrderReference = Token(agreement?.Element(Ram + "SellerOrderReferencedDocument")?.Element(Ram + "IssuerAssignedID")),
            ReceivingAdviceReference = Token(delivery?.Element(Ram + "ReceivingAdviceReferencedDocument")?.Element(Ram + "IssuerAssignedID")),
            DespatchAdviceReference = Token(delivery?.Element(Ram + "DespatchAdviceReferencedDocument")?.Element(Ram + "IssuerAssignedID")),
            TenderOrLotReference = Token(references.FirstOrDefault(reference => HasTypeCode(reference, TenderTypeCode))?.Element(Ram + "IssuerAssignedID")),
            InvoicedObjectIdentifier = ObjectIdentifierOf(references),
            BuyerAccountingReference = Text(settlement?.Element(Ram + "ReceivableSpecifiedTradeAccountingAccount")?.Element(Ram + "ID")),
            PaymentTerms = Text(terms.Elements(Ram + "Description").FirstOrDefault()),
            Notes = [.. Children(document, "IncludedNote").Select(note => new InvoiceNote
            {
                SubjectCode = Token(note.Element(Ram + "SubjectCode")),
                Note = Text(note.Element(Ram + "Content")),
            })],
            ProcessControl = new ProcessControl
            {
                BusinessProcessType = Text(context?.Element(Ram + "BusinessProcessSpecifiedDocumentContextParameter")?.Element(Ram + "ID")),
                SpecificationIdentifier = Identifier(context?.Element(Ram + "GuidelineSpecifiedDocumentContextParameter")?.Element(Ram + "ID")),
            },
            PrecedingInvoiceReferences = [.. Children(settlement, "InvoiceReferencedDocument").Select(reference => new PrecedingInvoiceReference
            {
                Reference = Token(reference.Element(Ram + "IssuerAssignedID")),
                IssueDate = QualifiedDateTimeOf(reference.Element(Ram + "FormattedIssueDateTime")),
            })],
            Seller = SellerOf(agreement?.Element(Ram + "SellerTradeParty")),
            Buyer = BuyerOf(agreement?.Element(Ram + "BuyerTradeParty")),
            Payee = PayeeOf(settlement?.Element(Ram + "PayeeTradeParty")),
            SellerTaxRepresentative = TaxRepresentativeOf(agreement?.Element(Ram + "SellerTaxRepresentativeTradeParty")),
            DeliveryInformation = DeliveryOf(delivery),
            InvoicingPeriod = settlement?.Element(Ram + "BillingSpecifiedPeriod") is { } period
                ? new InvoicingPeriod
                {
                    StartDate = DateTimeOf(period.Element(Ram + "StartDateTime")),
                    EndDate = DateTimeOf(period.Element(Ram + "EndDateTime")),
                }
                : null,
            PaymentInstructions = PaymentInstructionsOf(settlement, terms),
            DocumentLevelAllowances = [.. AllowanceCharges(settlement, isCharge: false).Select(allowance => new DocumentLevelAllowance
            {
                Amount = Token(allowance.Element(Ram + "ActualAmount")),
                BaseAmount = Token(allowance.Element(Ram + "BasisAmount")),
                Percentage = Token(allowance.Element(Ram + "CalculationPercent")),
                VatCategoryCode = Token(allowance.Element(Ram + "CategoryTradeTax")?.Element(Ram + "CategoryCode")),
                VatRate = Token(allowance.Element(Ram + "CategoryTradeTax")?.Element(Ram + "RateApplicablePercent")),
                Reason = Text(allowance.Element(Ram + "Reason")),
                ReasonCode = Token(allowance.Element(Ram + "ReasonCode")),
            })],
            DocumentLevelCharges = [.. AllowanceCharges(settlement, isCharge: true).Select(charge => new DocumentLevelCharge
            {
                Amount = Token(charge.Element(Ram + "ActualAmount")),
                BaseAmount = Token(charge.Element(Ram + "BasisAmount")),
                Percentage = Token(charge.Element(Ram + "CalculationPercent")),
                VatCategoryCode = Token(charge.Element(Ram + "CategoryTradeTax")?.Element(Ram + "CategoryCode")),
                VatRate = Token(charge.Element(Ram + "CategoryTradeTax")?.Element(Ram + "RateApplicablePercent")),
                Reason = Text(charge.Element(Ram + "Reason")),
                ReasonCode = Token(charge.Element(Ram + "ReasonCode")),
            })],
            DocumentTotals = TotalsOf(settlement?.Element(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation"), currency, accountingCurrency),
            VatBreakdowns = [.. taxes.Select(tax => new VatBreakdown
            {
                TaxableAmount = Token(tax.Element(Ram + "BasisAmount")),
                TaxAmount = Token(tax.Element(Ram + "CalculatedAmount")),
                CategoryCode = Token(tax.Element(Ram + "CategoryCode")),
                Rate = Token(tax.Element(Ram + "RateApplicablePercent")),
                ExemptionReason = Text(tax.Element(Ram + "ExemptionReason")),
                ExemptionReasonCode = Token(tax.Element(Ram + "ExemptionReasonCode")),
            })],
            AdditionalSupportingDocuments = [.. references
                .Where(reference => HasTypeCode(reference, SupportingDocumentTypeCode))
                .Select(reference => new AdditionalSupportingDocument
                {
                    Reference = Token(reference.Element(Ram + "IssuerAssignedID")),
                    Description = Text(reference.Element(Ram + "Name")),
                    ExternalLocation = Text(reference.Element(Ram + "URIID")),
                    AttachedDocument = BinaryObject(reference.Element(Ram + "AttachmentBinaryObject")),
                })],
            Lines = [.. Children(transaction, "IncludedSupplyChainTradeLineItem").Select(LineOf)],
        };
    }

    /// <summary>The seller (BG-4).</summary>
    private static Seller? SellerOf(XElement? party) => party is null ? null : new Seller
    {
        Name = Text(party.Element(Ram + "Name")),
        TradingName = Text(party.Element(Ram + "SpecifiedLegalOrganization")?.Element(Ram + "TradingBusinessName")),
        Identifiers =
        [
            .. party.Elements(Ram + "ID").Select(Identifier).OfType<Identifier>(),
            .. party.Elements(Ram + "GlobalID").Where(HasScheme).Select(Identifier).OfType<Identifier>(),
        ],
        LegalRegistrationIdentifier = Identifier(party.Element(Ram + "SpecifiedLegalOrganization")?.Element(Ram + "ID")),
        VatIdentifier = TaxRegistrationOf(party, VatRegistrationScheme),
        TaxRegistrationIdentifier = TaxRegistrationOf(party, TaxRegistrationScheme),
        AdditionalLegalInformation = Text(party.Element(Ram + "Description")),
        ElectronicAddress = Identifier(party.Element(Ram + "URIUniversalCommunication")?.Element(Ram + "URIID")),
        PostalAddress = AddressOf(party.Element(Ram + "PostalTradeAddress")) is { } address
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
        Contact = party.Element(Ram + "DefinedTradeContact") is { } contact
            ? new SellerContact
            {
                ContactPoint = ContactPointOf(contact),
                TelephoneNumber = Text(contact.Element(Ram + "TelephoneUniversalCommunication")?.Element(Ram + "CompleteNumber")),
                EmailAddress = Text(contact.Element(Ram + "EmailURIUniversalCommunication")?.Element(Ram + "URIID")),
            }
            : null,
    };

    /// <summary>The buyer (BG-7).</summary>
    private static Buyer? BuyerOf(XElement? party) => party is null ? null : new Buyer
    {
        Name = Text(party.Element(Ram + "Name")),
        TradingName = Text(party.Element(Ram + "SpecifiedLegalOrganization")?.Element(Ram + "TradingBusinessName")),
        Identifier = PartyIdentifierOf(party),
        LegalRegistrationIdentifier = Identifier(party.Element(Ram + "SpecifiedLegalOrganization")?.Element(Ram + "ID")),
        VatIdentifier = TaxRegistrationOf(party, VatRegistrationScheme),
        ElectronicAddress = Identifier(party.Element(Ram + "URIUniversalCommunication")?.Element(Ram + "URIID")),
        PostalAddress = AddressOf(party.Element(Ram + "PostalTradeAddress")) is { } address
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
        Contact = party.Element(Ram + "DefinedTradeContact") is { } contact
            ? new BuyerContact
            {
                ContactPoint = ContactPointOf(contact),
                TelephoneNumber = Text(contact.Element(Ram + "TelephoneUniversalCommunication")?.Element(Ram + "CompleteNumber")),
                EmailAddress = Text(contact.Element(Ram + "EmailURIUniversalCommunication")?.Element(Ram + "URIID")),
            }
            : null,
    };

    /// <summary>The payee (BG-10).</summary>
    private static Payee? PayeeOf(XElement? party) => party is null ? null : new Payee
    {
        Name = Text(party.Element(Ram + "Name")),
        Identifier = PartyIdentifierOf(party),
        LegalRegistrationIdentifier = Identifier(party.Element(Ram + "SpecifiedLegalOrganization")?.Element(Ram + "ID")),
    };

    /// <summary>The tax representative of the seller (BG-11).</summary>
    private static SellerTaxRepresentativeParty? TaxRepresentativeOf(XElement? party) => party is null ? null : new SellerTaxRepresentativeParty
    {
        Name = Text(party.Element(Ram + "Name")),
        VatIdentifier = Identifier(party.Element(Ram + "SpecifiedTaxRegistration")?.Element(Ram + "ID"), schemeId: null),
        PostalAddress = AddressOf(party.Element(Ram + "PostalTradeAddress")) is { } address
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

    /// <summary>The delivery information (BG-13): the ship-to party and the actual delivery date.</summary>
    private static DeliveryInformation? DeliveryOf(XElement? delivery)
    {
        var party = delivery?.Element(Ram + "ShipToTradeParty");
        var occurrence = delivery?.Element(Ram + "ActualDeliverySupplyChainEvent")?.Element(Ram + "OccurrenceDateTime");
        if (party is null && occurrence is null)
        {
            return null;
        }

        return new DeliveryInformation
        {
            DeliverToPartyName = Text(party?.Element(Ram + "Name")),
            DeliverToLocationIdentifier = party is null ? null : PartyIdentifierOf(party),
            ActualDeliveryDate = DateTimeOf(occurrence),
            DeliverToAddress = AddressOf(party?.Element(Ram + "PostalTradeAddress")) is { } address
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
    }

    /// <summary>
    /// The payment instructions (BG-16), gathered from every payment means:
    /// EN 16931 knows one, CII repeats the element for each account.
    /// </summary>
    private static PaymentInstructions? PaymentInstructionsOf(XElement? settlement, List<XElement> terms)
    {
        var means = Children(settlement, "SpecifiedTradeSettlementPaymentMeans").ToList();
        var mandate = terms.Elements(Ram + "DirectDebitMandateID").FirstOrDefault();
        var creditor = settlement?.Element(Ram + "CreditorReferenceID");
        var debited = means.Elements(Ram + "PayerPartyDebtorFinancialAccount").Elements(Ram + "IBANID").FirstOrDefault();
        var hasDirectDebit = mandate is not null || creditor is not null || debited is not null;
        var remittance = settlement?.Element(Ram + "PaymentReference");
        if (means.Count == 0 && !hasDirectDebit && remittance is null)
        {
            return null;
        }

        return new PaymentInstructions
        {
            PaymentMeansTypeCode = Token(means.Elements(Ram + "TypeCode").FirstOrDefault()),
            PaymentMeansText = Text(means.Elements(Ram + "Information").FirstOrDefault()),
            RemittanceInformation = Text(remittance),
            CreditTransfers = [.. means
                .Where(entry => entry.Element(Ram + "PayeePartyCreditorFinancialAccount") is not null)
                .Select(entry =>
                {
                    var account = entry.Element(Ram + "PayeePartyCreditorFinancialAccount")!;

                    return new CreditTransfer
                    {
                        PaymentAccountIdentifier = Identifier(account.Element(Ram + "IBANID") ?? account.Element(Ram + "ProprietaryID")),
                        PaymentAccountName = Text(account.Element(Ram + "AccountName")),
                        PaymentServiceProviderIdentifier = Identifier(entry.Element(Ram + "PayeeSpecifiedCreditorFinancialInstitution")?.Element(Ram + "BICID")),
                    };
                })],
            PaymentCard = means.Elements(Ram + "ApplicableTradeSettlementFinancialCard").FirstOrDefault() is { } card
                ? new PaymentCardInformation
                {
                    PrimaryAccountNumber = Text(card.Element(Ram + "ID")),
                    HolderName = Text(card.Element(Ram + "CardholderName")),
                }
                : null,
            DirectDebit = hasDirectDebit
                ? new DirectDebit
                {
                    MandateReferenceIdentifier = Identifier(mandate),
                    BankAssignedCreditorIdentifier = Identifier(creditor),
                    DebitedAccountIdentifier = Identifier(debited),
                }
                : null,
        };
    }

    /// <summary>
    /// The document totals (BG-22). The total VAT amount is the tax total in
    /// the invoice currency, or the one without a currency; the one in the
    /// accounting currency is the tax total in that currency.
    /// </summary>
    private static DocumentTotals? TotalsOf(XElement? totals, string? currency, string? accountingCurrency)
    {
        if (totals is null)
        {
            return null;
        }

        var taxAmounts = totals.Elements(Ram + "TaxTotalAmount").ToList();

        return new DocumentTotals
        {
            SumOfLineNetAmounts = Token(totals.Element(Ram + "LineTotalAmount")),
            SumOfAllowances = Token(totals.Element(Ram + "AllowanceTotalAmount")),
            SumOfCharges = Token(totals.Element(Ram + "ChargeTotalAmount")),
            TotalWithoutVat = Token(totals.Element(Ram + "TaxBasisTotalAmount")),
            TotalVatAmount = Token(taxAmounts.FirstOrDefault(amount => Token(amount.Attribute(CurrencyIdAttribute)) is not { } of || of == currency)),
            TotalVatAmountInAccountingCurrency = accountingCurrency is null || accountingCurrency == currency
                ? null
                : Token(taxAmounts.FirstOrDefault(amount => Token(amount.Attribute(CurrencyIdAttribute)) == accountingCurrency)),
            TotalWithVat = Token(totals.Element(Ram + "GrandTotalAmount")),
            PaidAmount = Token(totals.Element(Ram + "TotalPrepaidAmount")),
            RoundingAmount = Token(totals.Element(Ram + "RoundingAmount")),
            AmountDueForPayment = Token(totals.Element(Ram + "DuePayableAmount")),
        };
    }

    /// <summary>A line (BG-25).</summary>
    private static InvoiceLine LineOf(XElement line)
    {
        var document = line.Element(Ram + "AssociatedDocumentLineDocument");
        var agreement = line.Element(Ram + "SpecifiedLineTradeAgreement");
        var settlement = line.Element(Ram + "SpecifiedLineTradeSettlement");
        var quantity = line.Element(Ram + "SpecifiedLineTradeDelivery")?.Element(Ram + "BilledQuantity");
        var period = settlement?.Element(Ram + "BillingSpecifiedPeriod");
        var net = agreement?.Element(Ram + "NetPriceProductTradePrice");
        var gross = agreement?.Element(Ram + "GrossPriceProductTradePrice");
        var vat = settlement?.Element(Ram + "ApplicableTradeTax");
        var product = line.Element(Ram + "SpecifiedTradeProduct");

        // The base quantity is written with the gross price, the net price or
        // both; where both carry one, the gross price's counts.
        var baseQuantity = gross?.Element(Ram + "BasisQuantity") ?? net?.Element(Ram + "BasisQuantity");

        return new InvoiceLine
        {
            Identifier = Identifier(document?.Element(Ram + "LineID")),
            Note = Text(document?.Element(Ram + "IncludedNote")?.Element(Ram + "Content")),
            ObjectIdentifier = ObjectIdentifierOf(Children(settlement, "AdditionalReferencedDocument")),
            InvoicedQuantity = Token(quantity),
            InvoicedQuantityUnitCode = Token(quantity?.Attribute(UnitCodeAttribute)),
            NetAmount = Token(settlement?.Element(Ram + "SpecifiedTradeSettlementLineMonetarySummation")?.Element(Ram + "LineTotalAmount")),
            PurchaseOrderLineReference = Token(agreement?.Element(Ram + "BuyerOrderReferencedDocument")?.Element(Ram + "LineID")),
            BuyerAccountingReference = Text(settlement?.Element(Ram + "ReceivableSpecifiedTradeAccountingAccount")?.Element(Ram + "ID")),
            Period = period is null ? null : new InvoiceLinePeriod
            {
                StartDate = DateTimeOf(period.Element(Ram + "StartDateTime")),
                EndDate = DateTimeOf(period.Element(Ram + "EndDateTime")),
            },
            Allowances = [.. AllowanceCharges(settlement, isCharge: false).Select(allowance => new InvoiceLineAllowance
            {
                Amount = Token(allowance.Element(Ram + "ActualAmount")),
                BaseAmount = Token(allowance.Element(Ram + "BasisAmount")),
                Percentage = Token(allowance.Element(Ram + "CalculationPercent")),
                Reason = Text(allowance.Element(Ram + "Reason")),
                ReasonCode = Token(allowance.Element(Ram + "ReasonCode")),
            })],
            Charges = [.. AllowanceCharges(settlement, isCharge: true).Select(charge => new InvoiceLineCharge
            {
                Amount = Token(charge.Element(Ram + "ActualAmount")),
                BaseAmount = Token(charge.Element(Ram + "BasisAmount")),
                Percentage = Token(charge.Element(Ram + "CalculationPercent")),
                Reason = Text(charge.Element(Ram + "Reason")),
                ReasonCode = Token(charge.Element(Ram + "ReasonCode")),
            })],
            PriceDetails = net is null && gross is null ? null : new PriceDetails
            {
                NetPrice = Token(net?.Element(Ram + "ChargeAmount")),
                PriceDiscount = Token(gross?.Element(Ram + "AppliedTradeAllowanceCharge")?.Element(Ram + "ActualAmount")),
                GrossPrice = Token(gross?.Element(Ram + "ChargeAmount")),
                BaseQuantity = Token(baseQuantity),
                BaseQuantityUnitCode = Token(baseQuantity?.Attribute(UnitCodeAttribute)),
            },
            VatInformation = vat is null ? null : new LineVatInformation
            {
                VatCategoryCode = Token(vat.Element(Ram + "CategoryCode")),
                VatRate = Token(vat.Element(Ram + "RateApplicablePercent")),
            },
            Item = product is null ? null : new ItemInformation
            {
                Name = Text(product.Element(Ram + "Name")),
                Description = Text(product.Element(Ram + "Description")),
                SellersIdentifier = Identifier(product.Element(Ram + "SellerAssignedID")),
                BuyersIdentifier = Identifier(product.Element(Ram + "BuyerAssignedID")),
                StandardIdentifier = Identifier(product.Element(Ram + "GlobalID")),
                ClassificationIdentifiers = [.. product.Elements(Ram + "DesignatedProductClassification").Elements(Ram + "ClassCode").Select(Identifier).OfType<Identifier>()],
                CountryOfOrigin = Token(product.Element(Ram + "OriginTradeCountry")?.Element(Ram + "ID")),
                Attributes = [.. product.Elements(Ram + "ApplicableProductCharacteristic").Select(characteristic => new ItemAttribute
                {
                    Name = Text(characteristic.Element(Ram + "Description")),
                    Value = Text(characteristic.Element(Ram + "Value")),
                })],
            },
        };
    }

    /// <summary>The parts of a postal address, which four groups share.</summary>
    private static AddressParts? AddressOf(XElement? address) => address is null ? null : new AddressParts(
        Text(address.Element(Ram + "LineOne")),
        Text(address.Element(Ram + "LineTwo")),
        Text(address.Element(Ram + "LineThree")),
        Text(address.Element(Ram + "CityName")),
        Text(address.Element(Ram + "PostcodeCode")),
        Text(address.Element(Ram + "CountrySubDivisionName")),
        Token(address.Element(Ram + "CountryID")));

    /// <summary>The contact point of a contact: the person, else the department.</summary>
    private static string? ContactPointOf(XElement contact) =>
        Text(contact.Element(Ram + "PersonName")) ?? Text(contact.Element(Ram + "DepartmentName"));

    /// <summary>The identifier of a party: the global identifier with a scheme, else the plain one.</summary>
    private static Identifier? PartyIdentifierOf(XElement party) =>
        Identifier(party.Elements(Ram + "GlobalID").FirstOrDefault(HasScheme) ?? party.Element(Ram + "ID"));

    /// <summary>The tax registration of a party in the given scheme, without the scheme, which belongs to the binding.</summary>
    private static Identifier? TaxRegistrationOf(XElement party, string scheme) => Identifier(
        party.Elements(Ram + "SpecifiedTaxRegistration").Elements(Ram + "ID").FirstOrDefault(id => Token(id.Attribute(SchemeIdAttribute)) == scheme),
        schemeId: null);

    /// <summary>An invoiced object identifier among additional referenced documents, with its reference type code as the scheme.</summary>
    private static Identifier? ObjectIdentifierOf(IEnumerable<XElement> references) =>
        references.FirstOrDefault(reference => HasTypeCode(reference, InvoicedObjectTypeCode)) is { } found
            ? Identifier(found.Element(Ram + "IssuerAssignedID"), Token(found.Element(Ram + "ReferenceTypeCode")))
            : null;

    /// <summary>The allowances or the charges directly below <paramref name="parent"/>.</summary>
    private static IEnumerable<XElement> AllowanceCharges(XElement? parent, bool isCharge) =>
        Children(parent, "SpecifiedTradeAllowanceCharge").Where(element =>
        {
            var indicator = element.Element(Ram + "ChargeIndicator")?.Element(Udt + "Indicator");

            return isCharge ? IsTrue(indicator) : IsFalse(indicator);
        });

    /// <summary>The children named <paramref name="name"/> of an element that may be absent.</summary>
    private static IEnumerable<XElement> Children(XElement? parent, string name) =>
        parent?.Elements(Ram + name) ?? [];

    /// <summary>Whether an element names a scheme.</summary>
    private static bool HasScheme(XElement element) => Token(element.Attribute(SchemeIdAttribute)) is not null;

    /// <summary>Whether a referenced document has the given type code.</summary>
    private static bool HasTypeCode(XElement reference, string code) => Token(reference.Element(Ram + "TypeCode")) == code;

    /// <summary>The date of a date-time element, written as <c>udt:DateTimeString</c> in format 102.</summary>
    private static DateOnly? DateTimeOf(XElement? element) => FormattedDate(element?.Element(Udt + "DateTimeString"));

    /// <summary>The date of a formatted date-time element, written as <c>qdt:DateTimeString</c> in format 102.</summary>
    private static DateOnly? QualifiedDateTimeOf(XElement? element) => FormattedDate(element?.Element(Qdt + "DateTimeString"));

    /// <summary>The date of a date element, written as <c>udt:DateString</c> in format 102.</summary>
    private static DateOnly? DateOf(XElement element) => FormattedDate(element.Element(Udt + "DateString"));

    /// <summary>A date written in format 102; none for any other format.</summary>
    private static DateOnly? FormattedDate(XElement? value) =>
        value is not null && Token(value.Attribute(FormatAttribute)) == DateFormatCode ? Date(value.Value, CiiDateFormat) : null;
}
