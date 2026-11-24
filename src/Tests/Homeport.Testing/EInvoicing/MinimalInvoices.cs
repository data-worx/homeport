// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// The smallest invoices the XML schemas of UBL 2.1 and CII D16B admit, with
/// the specification identifier and issue date a test chooses, so a test of
/// rule-set detection is not disturbed by schema findings.
/// </summary>
/// <remarks>
/// They fit the schemas only; the business rules of EN 16931 they break.
/// </remarks>
public static class MinimalInvoices
{
    /// <summary>A UBL Invoice; <paramref name="issueDate"/> as UBL writes it, <c>YYYY-MM-DD</c>.</summary>
    /// <param name="customizationId">The specification identifier (BT-24).</param>
    /// <param name="issueDate">The issue date (BT-2).</param>
    public static string Ubl(string customizationId, string issueDate) =>
        $"""<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID>{customizationId}</cbc:CustomizationID><cbc:ID>1</cbc:ID><cbc:IssueDate>{issueDate}</cbc:IssueDate><cac:AccountingSupplierParty/><cac:AccountingCustomerParty/><cac:LegalMonetaryTotal><cbc:PayableAmount currencyID="EUR">0</cbc:PayableAmount></cac:LegalMonetaryTotal><cac:InvoiceLine><cbc:ID>1</cbc:ID><cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cac:Item/></cac:InvoiceLine></Invoice>""";

    /// <summary>A CII CrossIndustryInvoice; <paramref name="issueDate"/> as CII writes it, <c>YYYYMMDD</c> in format 102.</summary>
    /// <param name="customizationId">The specification identifier (BT-24).</param>
    /// <param name="issueDate">The issue date (BT-2).</param>
    public static string Cii(string customizationId, string issueDate) =>
        $"""<?xml version="1.0" encoding="UTF-8"?><rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100"><rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>{customizationId}</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext><rsm:ExchangedDocument><ram:ID>1</ram:ID><ram:IssueDateTime><udt:DateTimeString format="102">{issueDate}</udt:DateTimeString></ram:IssueDateTime></rsm:ExchangedDocument><rsm:SupplyChainTradeTransaction><ram:ApplicableHeaderTradeAgreement/><ram:ApplicableHeaderTradeDelivery/><ram:ApplicableHeaderTradeSettlement/></rsm:SupplyChainTradeTransaction></rsm:CrossIndustryInvoice>""";
}
