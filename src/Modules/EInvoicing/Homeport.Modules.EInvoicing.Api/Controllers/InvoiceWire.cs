// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Modules.EInvoicing.V1;

namespace Homeport.Modules.EInvoicing.Api.Controllers;

/// <summary>Maps the invoice of the domain onto its wire form, member by member.</summary>
internal static class InvoiceWire
{
    /// <summary>The wire form of an invoice.</summary>
    /// <param name="invoice">The invoice as the module read it.</param>
    public static Invoice From(Domain.Model.Invoice invoice) => Wire(invoice);

    /// <summary>The wire form of an identifier.</summary>
    private static Identifier Wire(Domain.Model.Identifier source) => new(source.Value, source.SchemeId, source.SchemeVersionId);

    /// <summary>The wire form of a binary object: its metadata, never its content.</summary>
    private static BinaryObject Wire(Domain.Model.BinaryObject source) => new(source.MimeCode, source.Filename);

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static Invoice Wire(Domain.Model.Invoice source) => new()
    {
        Number = source.Number is { } number ? Wire(number) : null,
        IssueDate = source.IssueDate,
        TypeCode = source.TypeCode,
        CurrencyCode = source.CurrencyCode,
        VatAccountingCurrencyCode = source.VatAccountingCurrencyCode,
        VatPointDate = source.VatPointDate,
        VatPointDateCode = source.VatPointDateCode,
        PaymentDueDate = source.PaymentDueDate,
        BuyerReference = source.BuyerReference,
        ProjectReference = source.ProjectReference,
        ContractReference = source.ContractReference,
        PurchaseOrderReference = source.PurchaseOrderReference,
        SalesOrderReference = source.SalesOrderReference,
        ReceivingAdviceReference = source.ReceivingAdviceReference,
        DespatchAdviceReference = source.DespatchAdviceReference,
        TenderOrLotReference = source.TenderOrLotReference,
        InvoicedObjectIdentifier = source.InvoicedObjectIdentifier is { } invoicedObjectIdentifier ? Wire(invoicedObjectIdentifier) : null,
        BuyerAccountingReference = source.BuyerAccountingReference,
        PaymentTerms = source.PaymentTerms,
        Notes = [.. source.Notes.Select(Wire)],
        ProcessControl = source.ProcessControl is { } processControl ? Wire(processControl) : null,
        PrecedingInvoiceReferences = [.. source.PrecedingInvoiceReferences.Select(Wire)],
        Seller = source.Seller is { } seller ? Wire(seller) : null,
        Buyer = source.Buyer is { } buyer ? Wire(buyer) : null,
        Payee = source.Payee is { } payee ? Wire(payee) : null,
        SellerTaxRepresentative = source.SellerTaxRepresentative is { } sellerTaxRepresentative ? Wire(sellerTaxRepresentative) : null,
        DeliveryInformation = source.DeliveryInformation is { } deliveryInformation ? Wire(deliveryInformation) : null,
        InvoicingPeriod = source.InvoicingPeriod is { } invoicingPeriod ? Wire(invoicingPeriod) : null,
        PaymentInstructions = source.PaymentInstructions is { } paymentInstructions ? Wire(paymentInstructions) : null,
        DocumentLevelAllowances = [.. source.DocumentLevelAllowances.Select(Wire)],
        DocumentLevelCharges = [.. source.DocumentLevelCharges.Select(Wire)],
        DocumentTotals = source.DocumentTotals is { } documentTotals ? Wire(documentTotals) : null,
        VatBreakdowns = [.. source.VatBreakdowns.Select(Wire)],
        AdditionalSupportingDocuments = [.. source.AdditionalSupportingDocuments.Select(Wire)],
        Lines = [.. source.Lines.Select(Wire)],
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoiceNote Wire(Domain.Model.InvoiceNote source) => new()
    {
        SubjectCode = source.SubjectCode,
        Note = source.Note,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static ProcessControl Wire(Domain.Model.ProcessControl source) => new()
    {
        BusinessProcessType = source.BusinessProcessType,
        SpecificationIdentifier = source.SpecificationIdentifier is { } specificationIdentifier ? Wire(specificationIdentifier) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static PrecedingInvoiceReference Wire(Domain.Model.PrecedingInvoiceReference source) => new()
    {
        Reference = source.Reference,
        IssueDate = source.IssueDate,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static Seller Wire(Domain.Model.Seller source) => new()
    {
        Name = source.Name,
        TradingName = source.TradingName,
        Identifiers = [.. source.Identifiers.Select(Wire)],
        LegalRegistrationIdentifier = source.LegalRegistrationIdentifier is { } legalRegistrationIdentifier ? Wire(legalRegistrationIdentifier) : null,
        VatIdentifier = source.VatIdentifier is { } vatIdentifier ? Wire(vatIdentifier) : null,
        TaxRegistrationIdentifier = source.TaxRegistrationIdentifier is { } taxRegistrationIdentifier ? Wire(taxRegistrationIdentifier) : null,
        AdditionalLegalInformation = source.AdditionalLegalInformation,
        ElectronicAddress = source.ElectronicAddress is { } electronicAddress ? Wire(electronicAddress) : null,
        PostalAddress = source.PostalAddress is { } postalAddress ? Wire(postalAddress) : null,
        Contact = source.Contact is { } contact ? Wire(contact) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static SellerPostalAddress Wire(Domain.Model.SellerPostalAddress source) => new()
    {
        AddressLine1 = source.AddressLine1,
        AddressLine2 = source.AddressLine2,
        AddressLine3 = source.AddressLine3,
        City = source.City,
        PostCode = source.PostCode,
        CountrySubdivision = source.CountrySubdivision,
        CountryCode = source.CountryCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static SellerContact Wire(Domain.Model.SellerContact source) => new()
    {
        ContactPoint = source.ContactPoint,
        TelephoneNumber = source.TelephoneNumber,
        EmailAddress = source.EmailAddress,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static Buyer Wire(Domain.Model.Buyer source) => new()
    {
        Name = source.Name,
        TradingName = source.TradingName,
        Identifier = source.Identifier is { } identifier ? Wire(identifier) : null,
        LegalRegistrationIdentifier = source.LegalRegistrationIdentifier is { } legalRegistrationIdentifier ? Wire(legalRegistrationIdentifier) : null,
        VatIdentifier = source.VatIdentifier is { } vatIdentifier ? Wire(vatIdentifier) : null,
        ElectronicAddress = source.ElectronicAddress is { } electronicAddress ? Wire(electronicAddress) : null,
        PostalAddress = source.PostalAddress is { } postalAddress ? Wire(postalAddress) : null,
        Contact = source.Contact is { } contact ? Wire(contact) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static BuyerPostalAddress Wire(Domain.Model.BuyerPostalAddress source) => new()
    {
        AddressLine1 = source.AddressLine1,
        AddressLine2 = source.AddressLine2,
        AddressLine3 = source.AddressLine3,
        City = source.City,
        PostCode = source.PostCode,
        CountrySubdivision = source.CountrySubdivision,
        CountryCode = source.CountryCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static BuyerContact Wire(Domain.Model.BuyerContact source) => new()
    {
        ContactPoint = source.ContactPoint,
        TelephoneNumber = source.TelephoneNumber,
        EmailAddress = source.EmailAddress,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static Payee Wire(Domain.Model.Payee source) => new()
    {
        Name = source.Name,
        Identifier = source.Identifier is { } identifier ? Wire(identifier) : null,
        LegalRegistrationIdentifier = source.LegalRegistrationIdentifier is { } legalRegistrationIdentifier ? Wire(legalRegistrationIdentifier) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static SellerTaxRepresentativeParty Wire(Domain.Model.SellerTaxRepresentativeParty source) => new()
    {
        Name = source.Name,
        VatIdentifier = source.VatIdentifier is { } vatIdentifier ? Wire(vatIdentifier) : null,
        PostalAddress = source.PostalAddress is { } postalAddress ? Wire(postalAddress) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static SellerTaxRepresentativePostalAddress Wire(Domain.Model.SellerTaxRepresentativePostalAddress source) => new()
    {
        AddressLine1 = source.AddressLine1,
        AddressLine2 = source.AddressLine2,
        AddressLine3 = source.AddressLine3,
        City = source.City,
        PostCode = source.PostCode,
        CountrySubdivision = source.CountrySubdivision,
        CountryCode = source.CountryCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DeliveryInformation Wire(Domain.Model.DeliveryInformation source) => new()
    {
        DeliverToPartyName = source.DeliverToPartyName,
        DeliverToLocationIdentifier = source.DeliverToLocationIdentifier is { } deliverToLocationIdentifier ? Wire(deliverToLocationIdentifier) : null,
        ActualDeliveryDate = source.ActualDeliveryDate,
        DeliverToAddress = source.DeliverToAddress is { } deliverToAddress ? Wire(deliverToAddress) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoicingPeriod Wire(Domain.Model.InvoicingPeriod source) => new()
    {
        StartDate = source.StartDate,
        EndDate = source.EndDate,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DeliverToAddress Wire(Domain.Model.DeliverToAddress source) => new()
    {
        AddressLine1 = source.AddressLine1,
        AddressLine2 = source.AddressLine2,
        AddressLine3 = source.AddressLine3,
        City = source.City,
        PostCode = source.PostCode,
        CountrySubdivision = source.CountrySubdivision,
        CountryCode = source.CountryCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static PaymentInstructions Wire(Domain.Model.PaymentInstructions source) => new()
    {
        PaymentMeansTypeCode = source.PaymentMeansTypeCode,
        PaymentMeansText = source.PaymentMeansText,
        RemittanceInformation = source.RemittanceInformation,
        CreditTransfers = [.. source.CreditTransfers.Select(Wire)],
        PaymentCard = source.PaymentCard is { } paymentCard ? Wire(paymentCard) : null,
        DirectDebit = source.DirectDebit is { } directDebit ? Wire(directDebit) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static CreditTransfer Wire(Domain.Model.CreditTransfer source) => new()
    {
        PaymentAccountIdentifier = source.PaymentAccountIdentifier is { } paymentAccountIdentifier ? Wire(paymentAccountIdentifier) : null,
        PaymentAccountName = source.PaymentAccountName,
        PaymentServiceProviderIdentifier = source.PaymentServiceProviderIdentifier is { } paymentServiceProviderIdentifier ? Wire(paymentServiceProviderIdentifier) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static PaymentCardInformation Wire(Domain.Model.PaymentCardInformation source) => new()
    {
        PrimaryAccountNumber = source.PrimaryAccountNumber,
        HolderName = source.HolderName,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DirectDebit Wire(Domain.Model.DirectDebit source) => new()
    {
        MandateReferenceIdentifier = source.MandateReferenceIdentifier is { } mandateReferenceIdentifier ? Wire(mandateReferenceIdentifier) : null,
        BankAssignedCreditorIdentifier = source.BankAssignedCreditorIdentifier is { } bankAssignedCreditorIdentifier ? Wire(bankAssignedCreditorIdentifier) : null,
        DebitedAccountIdentifier = source.DebitedAccountIdentifier is { } debitedAccountIdentifier ? Wire(debitedAccountIdentifier) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DocumentLevelAllowance Wire(Domain.Model.DocumentLevelAllowance source) => new()
    {
        Amount = source.Amount,
        BaseAmount = source.BaseAmount,
        Percentage = source.Percentage,
        VatCategoryCode = source.VatCategoryCode,
        VatRate = source.VatRate,
        Reason = source.Reason,
        ReasonCode = source.ReasonCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DocumentLevelCharge Wire(Domain.Model.DocumentLevelCharge source) => new()
    {
        Amount = source.Amount,
        BaseAmount = source.BaseAmount,
        Percentage = source.Percentage,
        VatCategoryCode = source.VatCategoryCode,
        VatRate = source.VatRate,
        Reason = source.Reason,
        ReasonCode = source.ReasonCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static DocumentTotals Wire(Domain.Model.DocumentTotals source) => new()
    {
        SumOfLineNetAmounts = source.SumOfLineNetAmounts,
        SumOfAllowances = source.SumOfAllowances,
        SumOfCharges = source.SumOfCharges,
        TotalWithoutVat = source.TotalWithoutVat,
        TotalVatAmount = source.TotalVatAmount,
        TotalVatAmountInAccountingCurrency = source.TotalVatAmountInAccountingCurrency,
        TotalWithVat = source.TotalWithVat,
        PaidAmount = source.PaidAmount,
        RoundingAmount = source.RoundingAmount,
        AmountDueForPayment = source.AmountDueForPayment,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static VatBreakdown Wire(Domain.Model.VatBreakdown source) => new()
    {
        TaxableAmount = source.TaxableAmount,
        TaxAmount = source.TaxAmount,
        CategoryCode = source.CategoryCode,
        Rate = source.Rate,
        ExemptionReason = source.ExemptionReason,
        ExemptionReasonCode = source.ExemptionReasonCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static AdditionalSupportingDocument Wire(Domain.Model.AdditionalSupportingDocument source) => new()
    {
        Reference = source.Reference,
        Description = source.Description,
        ExternalLocation = source.ExternalLocation,
        AttachedDocument = source.AttachedDocument is { } attachedDocument ? Wire(attachedDocument) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoiceLine Wire(Domain.Model.InvoiceLine source) => new()
    {
        Identifier = source.Identifier is { } identifier ? Wire(identifier) : null,
        Note = source.Note,
        ObjectIdentifier = source.ObjectIdentifier is { } objectIdentifier ? Wire(objectIdentifier) : null,
        InvoicedQuantity = source.InvoicedQuantity,
        InvoicedQuantityUnitCode = source.InvoicedQuantityUnitCode,
        NetAmount = source.NetAmount,
        PurchaseOrderLineReference = source.PurchaseOrderLineReference,
        BuyerAccountingReference = source.BuyerAccountingReference,
        Period = source.Period is { } period ? Wire(period) : null,
        Allowances = [.. source.Allowances.Select(Wire)],
        Charges = [.. source.Charges.Select(Wire)],
        PriceDetails = source.PriceDetails is { } priceDetails ? Wire(priceDetails) : null,
        VatInformation = source.VatInformation is { } vatInformation ? Wire(vatInformation) : null,
        Item = source.Item is { } item ? Wire(item) : null,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoiceLinePeriod Wire(Domain.Model.InvoiceLinePeriod source) => new()
    {
        StartDate = source.StartDate,
        EndDate = source.EndDate,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoiceLineAllowance Wire(Domain.Model.InvoiceLineAllowance source) => new()
    {
        Amount = source.Amount,
        BaseAmount = source.BaseAmount,
        Percentage = source.Percentage,
        Reason = source.Reason,
        ReasonCode = source.ReasonCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static InvoiceLineCharge Wire(Domain.Model.InvoiceLineCharge source) => new()
    {
        Amount = source.Amount,
        BaseAmount = source.BaseAmount,
        Percentage = source.Percentage,
        Reason = source.Reason,
        ReasonCode = source.ReasonCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static PriceDetails Wire(Domain.Model.PriceDetails source) => new()
    {
        NetPrice = source.NetPrice,
        PriceDiscount = source.PriceDiscount,
        GrossPrice = source.GrossPrice,
        BaseQuantity = source.BaseQuantity,
        BaseQuantityUnitCode = source.BaseQuantityUnitCode,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static LineVatInformation Wire(Domain.Model.LineVatInformation source) => new()
    {
        VatCategoryCode = source.VatCategoryCode,
        VatRate = source.VatRate,
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static ItemInformation Wire(Domain.Model.ItemInformation source) => new()
    {
        Name = source.Name,
        Description = source.Description,
        SellersIdentifier = source.SellersIdentifier is { } sellersIdentifier ? Wire(sellersIdentifier) : null,
        BuyersIdentifier = source.BuyersIdentifier is { } buyersIdentifier ? Wire(buyersIdentifier) : null,
        StandardIdentifier = source.StandardIdentifier is { } standardIdentifier ? Wire(standardIdentifier) : null,
        ClassificationIdentifiers = [.. source.ClassificationIdentifiers.Select(Wire)],
        CountryOfOrigin = source.CountryOfOrigin,
        Attributes = [.. source.Attributes.Select(Wire)],
    };

    /// <summary>The wire form of <paramref name="source"/>.</summary>
    private static ItemAttribute Wire(Domain.Model.ItemAttribute source) => new()
    {
        Name = source.Name,
        Value = source.Value,
    };
}
