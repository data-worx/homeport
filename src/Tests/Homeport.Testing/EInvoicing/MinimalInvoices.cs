// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// The smallest invoices that fit the XML schemas of UBL 2.1 and CII D16B and
/// the business rules the module implements, with the specification
/// identifier and issue date a test chooses, so a test of rule-set detection
/// is not disturbed by findings.
/// </summary>
/// <remarks>
/// A ticket that implements more rules extends them where they break one.
/// </remarks>
public static class MinimalInvoices
{
    /// <summary>A UBL Invoice; <paramref name="issueDate"/> as UBL writes it, <c>YYYY-MM-DD</c>.</summary>
    /// <param name="customizationId">The specification identifier (BT-24).</param>
    /// <param name="issueDate">The issue date (BT-2).</param>
    public static string Ubl(string customizationId, string issueDate) =>
        $"""<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID>{customizationId}</cbc:CustomizationID><cbc:ID>1</cbc:ID><cbc:IssueDate>{issueDate}</cbc:IssueDate><cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode><cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode><cac:AccountingSupplierParty><cac:Party><cac:PostalAddress><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:PostalAddress><cac:PartyLegalEntity><cbc:RegistrationName>Seller</cbc:RegistrationName></cac:PartyLegalEntity></cac:Party></cac:AccountingSupplierParty><cac:AccountingCustomerParty><cac:Party><cac:PostalAddress><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:PostalAddress><cac:PartyLegalEntity><cbc:RegistrationName>Buyer</cbc:RegistrationName></cac:PartyLegalEntity></cac:Party></cac:AccountingCustomerParty><cac:LegalMonetaryTotal><cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cbc:TaxExclusiveAmount currencyID="EUR">0</cbc:TaxExclusiveAmount><cbc:TaxInclusiveAmount currencyID="EUR">0</cbc:TaxInclusiveAmount><cbc:PayableAmount currencyID="EUR">0</cbc:PayableAmount></cac:LegalMonetaryTotal><cac:InvoiceLine><cbc:ID>1</cbc:ID><cbc:InvoicedQuantity unitCode="C62">1</cbc:InvoicedQuantity><cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cac:Item><cbc:Name>Item</cbc:Name></cac:Item><cac:Price><cbc:PriceAmount currencyID="EUR">0</cbc:PriceAmount></cac:Price></cac:InvoiceLine></Invoice>""";

    /// <summary>A CII CrossIndustryInvoice; <paramref name="issueDate"/> as CII writes it, <c>YYYYMMDD</c> in format 102.</summary>
    /// <param name="customizationId">The specification identifier (BT-24).</param>
    /// <param name="issueDate">The issue date (BT-2).</param>
    public static string Cii(string customizationId, string issueDate) =>
        $"""<?xml version="1.0" encoding="UTF-8"?><rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100"><rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>{customizationId}</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext><rsm:ExchangedDocument><ram:ID>1</ram:ID><ram:TypeCode>380</ram:TypeCode><ram:IssueDateTime><udt:DateTimeString format="102">{issueDate}</udt:DateTimeString></ram:IssueDateTime></rsm:ExchangedDocument><rsm:SupplyChainTradeTransaction><ram:IncludedSupplyChainTradeLineItem><ram:AssociatedDocumentLineDocument><ram:LineID>1</ram:LineID></ram:AssociatedDocumentLineDocument><ram:SpecifiedTradeProduct><ram:Name>Item</ram:Name></ram:SpecifiedTradeProduct><ram:SpecifiedLineTradeAgreement><ram:NetPriceProductTradePrice><ram:ChargeAmount>0</ram:ChargeAmount></ram:NetPriceProductTradePrice></ram:SpecifiedLineTradeAgreement><ram:SpecifiedLineTradeDelivery><ram:BilledQuantity unitCode="C62">1</ram:BilledQuantity></ram:SpecifiedLineTradeDelivery><ram:SpecifiedLineTradeSettlement><ram:SpecifiedTradeSettlementLineMonetarySummation><ram:LineTotalAmount>0</ram:LineTotalAmount></ram:SpecifiedTradeSettlementLineMonetarySummation></ram:SpecifiedLineTradeSettlement></ram:IncludedSupplyChainTradeLineItem><ram:ApplicableHeaderTradeAgreement><ram:SellerTradeParty><ram:Name>Seller</ram:Name><ram:PostalTradeAddress><ram:CountryID>DE</ram:CountryID></ram:PostalTradeAddress></ram:SellerTradeParty><ram:BuyerTradeParty><ram:Name>Buyer</ram:Name><ram:PostalTradeAddress><ram:CountryID>DE</ram:CountryID></ram:PostalTradeAddress></ram:BuyerTradeParty></ram:ApplicableHeaderTradeAgreement><ram:ApplicableHeaderTradeDelivery/><ram:ApplicableHeaderTradeSettlement><ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode><ram:SpecifiedTradeSettlementHeaderMonetarySummation><ram:LineTotalAmount>0</ram:LineTotalAmount><ram:TaxBasisTotalAmount>0</ram:TaxBasisTotalAmount><ram:GrandTotalAmount>0</ram:GrandTotalAmount><ram:DuePayableAmount>0</ram:DuePayableAmount></ram:SpecifiedTradeSettlementHeaderMonetarySummation></ram:ApplicableHeaderTradeSettlement></rsm:SupplyChainTradeTransaction></rsm:CrossIndustryInvoice>""";
}
