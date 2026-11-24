# Reading an invoice

The e-invoicing module, module id `e-invoicing`, reads the electronic
invoices you receive into one model: the semantic model of EN 16931, the
same for a UBL invoice, a UBL credit note and a CII invoice, and for the
invoice a ZUGFeRD or Factur-X PDF carries. It validates the document as
[validating an invoice](validating-an-invoice.md) describes, and returns the
report together with the invoice.

## The request

```text
POST /modules/e-invoicing/v1/read
```

- The body is the document itself, as you received it, with `Content-Type`
  `application/xml`, `text/xml` or `application/pdf`, exactly as for
  [`validate`](validating-an-invoice.md#the-request).
- The token needs the scope `e-invoicing.read`; a token with
  `e-invoicing.validate` alone is answered with `403`. See
  [obtaining a token](obtaining-a-token.md).
- A document the module cannot process is answered with `422`, as listed
  under [rejected documents](validating-an-invoice.md#rejected-documents).

## The response

A document the module processed is answered with `200`:

```json
{
  "report": {
    "container": "xml",
    "syntax": "cii",
    "customizationId": "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0",
    "ruleSet": "xrechnung",
    "ruleSetVersion": "3.0",
    "verdict": null,
    "findings": []
  },
  "invoice": {
    "number": { "value": "INV-2026-0001", "schemeId": null, "schemeVersionId": null },
    "issueDate": "2026-11-02",
    "typeCode": "380",
    "currencyCode": "EUR",
    "seller": { "name": "Seller GmbH", "…": "…" },
    "documentTotals": { "totalWithVat": "1190.0", "…": "…" },
    "lines": [ { "identifier": { "value": "1", "schemeId": null, "schemeVersionId": null }, "…": "…" } ],
    "…": "…"
  }
}
```

`report` is the report `validate` returns. `invoice` is the invoice, read as
far as the document carries it — also when the report finds it invalid. It
is `null` only for a ZUGFeRD 1.0 invoice, which is written in no syntax
EN 16931 binds to. A ZUGFeRD MINIMUM or BASIC WL invoice, no e-invoice under
EN 16931, is read all the same.

In .NET, the response is `InvoiceReadResponse` of
`Homeport.Shared.Client.Modules.EInvoicing.V1`, the invoice its `Invoice`.

## How the model is written

- Every member is written, `null` when the document does not carry it, and
  a list is `[]` when the document carries none. A group the document does
  not carry is `null`.
- **Amounts, quantities, prices and percentages are strings**, exactly as the
  document writes them: `1190.0` stays `"1190.0"`, `19` stays `"19"`. Parse
  them with a decimal type, never as a floating-point number.
- **Dates** are `yyyy-MM-dd`. A date the document writes in another form
  than its syntax allows is `null`.
- **Identifiers** are objects: `value`, and the `schemeId` and
  `schemeVersionId` the document names, or `null`.
- **Codes, references and texts** are strings. Codes, references and
  identifiers are trimmed; texts are kept as written.
- **An attachment** (BT-125) is described by `mimeCode` and `filename`. Its
  content is never returned.

## Where the syntaxes differ

The model is the same whichever syntax the document came in; the syntax is
in the report. Where UBL and CII bind a term differently, the module reads
it like this:

| Term | Reading |
|---|---|
| BT-8 | CII codes it in UNTDID 2475, the model in UNTDID 2005, as UBL does: `5` becomes `3`, `29` becomes `35`, `72` becomes `432`. Another code is kept as written |
| BT-21 | UBL writes the subject code in front of the note, as `#AAI#text`; the module splits it into `subjectCode` and `note` |
| BG-16 | EN 16931 knows one payment instruction; both syntaxes may repeat the payment means. The module reads the type code, the text and the remittance information of the first, every account as a credit transfer, the first card and the first mandate |
| BT-7 | CII writes the VAT point date with each VAT breakdown; the module reads the first |
| BT-41, BT-56 | CII may write a person and a department; the module reads the person, else the department |
| BT-149, BT-150 | CII may write the base quantity with the gross and with the net price; the module reads the gross price's, else the net price's |
| BT-90 | UBL writes it as a party identification with the scheme `SEPA`; the module reads it without that scheme, as CII does |

## Extensions

XRechnung Extension and ZUGFeRD / Factur-X EXTENDED add elements that
EN 16931 has no term for. The module reads the EN 16931 core of such an
invoice and leaves the extension out; the report then carries the warning
`EINV-EXTENSION-IGNORED`.

## What it counts

Every document answered with `200` counts one in the meter `documents`; a
request answered with `4xx` or `5xx` counts nothing, as for
[`validate`](validating-an-invoice.md#what-it-counts).

## The fields

Every business term (BT) and business group (BG) of EN 16931 has one field.
`[]` marks a list. The names of the terms are those of EN 16931; the
identifiers and names are taken from the semantic model the KoSIT
XRechnung visualization publishes.

| Field | Term | Name | Type |
|---|---|---|---|
| `invoice.number` | BT-1 | Invoice number | identifier |
| `invoice.issueDate` | BT-2 | Invoice issue date | date |
| `invoice.typeCode` | BT-3 | Invoice type code | code |
| `invoice.currencyCode` | BT-5 | Invoice currency code | code |
| `invoice.vatAccountingCurrencyCode` | BT-6 | VAT accounting currency code | code |
| `invoice.vatPointDate` | BT-7 | Value added tax point date | date |
| `invoice.vatPointDateCode` | BT-8 | Value added tax point date code | code |
| `invoice.paymentDueDate` | BT-9 | Payment due date | date |
| `invoice.buyerReference` | BT-10 | Buyer reference | text |
| `invoice.projectReference` | BT-11 | Project reference | reference |
| `invoice.contractReference` | BT-12 | Contract reference | reference |
| `invoice.purchaseOrderReference` | BT-13 | Purchase order reference | reference |
| `invoice.salesOrderReference` | BT-14 | Sales order reference | reference |
| `invoice.receivingAdviceReference` | BT-15 | Receiving advice reference | reference |
| `invoice.despatchAdviceReference` | BT-16 | Despatch advice reference | reference |
| `invoice.tenderOrLotReference` | BT-17 | Tender or lot reference | reference |
| `invoice.invoicedObjectIdentifier` | BT-18 | Invoiced object identifier | identifier |
| `invoice.buyerAccountingReference` | BT-19 | Buyer accounting reference | text |
| `invoice.paymentTerms` | BT-20 | Payment terms | text |
| `invoice.notes[]` | BG-1 | Invoice note | list of groups |
| `invoice.notes[].subjectCode` | BT-21 | Invoice note subject code | code |
| `invoice.notes[].note` | BT-22 | Invoice note | text |
| `invoice.processControl` | BG-2 | Process control | group |
| `invoice.processControl.businessProcessType` | BT-23 | Business process type | text |
| `invoice.processControl.specificationIdentifier` | BT-24 | Specification identifier | identifier |
| `invoice.precedingInvoiceReferences[]` | BG-3 | Preceding invoice reference | list of groups |
| `invoice.precedingInvoiceReferences[].reference` | BT-25 | Preceding Invoice reference | reference |
| `invoice.precedingInvoiceReferences[].issueDate` | BT-26 | Preceding Invoice issue date | date |
| `invoice.seller` | BG-4 | Seller | group |
| `invoice.seller.name` | BT-27 | Seller name | text |
| `invoice.seller.tradingName` | BT-28 | Seller trading name | text |
| `invoice.seller.identifiers[]` | BT-29 | Seller identifier | list of identifiers |
| `invoice.seller.legalRegistrationIdentifier` | BT-30 | Seller legal registration identifier | identifier |
| `invoice.seller.vatIdentifier` | BT-31 | Seller VAT identifier | identifier |
| `invoice.seller.taxRegistrationIdentifier` | BT-32 | Seller tax registration identifier | identifier |
| `invoice.seller.additionalLegalInformation` | BT-33 | Seller additional legal information | text |
| `invoice.seller.electronicAddress` | BT-34 | Seller electronic address | identifier |
| `invoice.seller.postalAddress` | BG-5 | Seller postal address | group |
| `invoice.seller.postalAddress.addressLine1` | BT-35 | Seller address line 1 | text |
| `invoice.seller.postalAddress.addressLine2` | BT-36 | Seller address line 2 | text |
| `invoice.seller.postalAddress.addressLine3` | BT-162 | Seller address line 3 | text |
| `invoice.seller.postalAddress.city` | BT-37 | Seller city | text |
| `invoice.seller.postalAddress.postCode` | BT-38 | Seller post code | text |
| `invoice.seller.postalAddress.countrySubdivision` | BT-39 | Seller country subdivision | text |
| `invoice.seller.postalAddress.countryCode` | BT-40 | Seller country code | code |
| `invoice.seller.contact` | BG-6 | Seller contact | group |
| `invoice.seller.contact.contactPoint` | BT-41 | Seller contact point | text |
| `invoice.seller.contact.telephoneNumber` | BT-42 | Seller contact telephone number | text |
| `invoice.seller.contact.emailAddress` | BT-43 | Seller contact email address | text |
| `invoice.buyer` | BG-7 | Buyer | group |
| `invoice.buyer.name` | BT-44 | Buyer name | text |
| `invoice.buyer.tradingName` | BT-45 | Buyer trading name | text |
| `invoice.buyer.identifier` | BT-46 | Buyer identifier | identifier |
| `invoice.buyer.legalRegistrationIdentifier` | BT-47 | Buyer legal registration identifier | identifier |
| `invoice.buyer.vatIdentifier` | BT-48 | Buyer VAT identifier | identifier |
| `invoice.buyer.electronicAddress` | BT-49 | Buyer electronic address | identifier |
| `invoice.buyer.postalAddress` | BG-8 | Buyer postal address | group |
| `invoice.buyer.postalAddress.addressLine1` | BT-50 | Buyer address line 1 | text |
| `invoice.buyer.postalAddress.addressLine2` | BT-51 | Buyer address line 2 | text |
| `invoice.buyer.postalAddress.addressLine3` | BT-163 | Buyer address line 3 | text |
| `invoice.buyer.postalAddress.city` | BT-52 | Buyer city | text |
| `invoice.buyer.postalAddress.postCode` | BT-53 | Buyer post code | text |
| `invoice.buyer.postalAddress.countrySubdivision` | BT-54 | Buyer country subdivision | text |
| `invoice.buyer.postalAddress.countryCode` | BT-55 | Buyer country code | code |
| `invoice.buyer.contact` | BG-9 | Buyer contact | group |
| `invoice.buyer.contact.contactPoint` | BT-56 | Buyer contact point | text |
| `invoice.buyer.contact.telephoneNumber` | BT-57 | Buyer contact telephone number | text |
| `invoice.buyer.contact.emailAddress` | BT-58 | Buyer contact email address | text |
| `invoice.payee` | BG-10 | Payee | group |
| `invoice.payee.name` | BT-59 | Payee name | text |
| `invoice.payee.identifier` | BT-60 | Payee identifier | identifier |
| `invoice.payee.legalRegistrationIdentifier` | BT-61 | Payee legal registration identifier | identifier |
| `invoice.sellerTaxRepresentative` | BG-11 | Seller tax representative party | group |
| `invoice.sellerTaxRepresentative.name` | BT-62 | Seller tax representative name | text |
| `invoice.sellerTaxRepresentative.vatIdentifier` | BT-63 | Seller tax representative VAT identifier | identifier |
| `invoice.sellerTaxRepresentative.postalAddress` | BG-12 | Seller tax representative postal address | group |
| `invoice.sellerTaxRepresentative.postalAddress.addressLine1` | BT-64 | Tax representative address line 1 | text |
| `invoice.sellerTaxRepresentative.postalAddress.addressLine2` | BT-65 | Tax representative address line 2 | text |
| `invoice.sellerTaxRepresentative.postalAddress.addressLine3` | BT-164 | Tax representative address line 3 | text |
| `invoice.sellerTaxRepresentative.postalAddress.city` | BT-66 | Tax representative city | text |
| `invoice.sellerTaxRepresentative.postalAddress.postCode` | BT-67 | Tax representative post code | text |
| `invoice.sellerTaxRepresentative.postalAddress.countrySubdivision` | BT-68 | Tax representative country subdivision | text |
| `invoice.sellerTaxRepresentative.postalAddress.countryCode` | BT-69 | Tax representative country code | code |
| `invoice.deliveryInformation` | BG-13 | Delivery information | group |
| `invoice.deliveryInformation.deliverToPartyName` | BT-70 | Deliver to party name | text |
| `invoice.deliveryInformation.deliverToLocationIdentifier` | BT-71 | Deliver to location identifier | identifier |
| `invoice.deliveryInformation.actualDeliveryDate` | BT-72 | Actual delivery date | date |
| `invoice.deliveryInformation.deliverToAddress` | BG-15 | Deliver to address | group |
| `invoice.deliveryInformation.deliverToAddress.addressLine1` | BT-75 | Deliver to address line 1 | text |
| `invoice.deliveryInformation.deliverToAddress.addressLine2` | BT-76 | Deliver to address line 2 | text |
| `invoice.deliveryInformation.deliverToAddress.addressLine3` | BT-165 | Deliver to address line 3 | text |
| `invoice.deliveryInformation.deliverToAddress.city` | BT-77 | Deliver to city | text |
| `invoice.deliveryInformation.deliverToAddress.postCode` | BT-78 | Deliver to post code | text |
| `invoice.deliveryInformation.deliverToAddress.countrySubdivision` | BT-79 | Deliver to country subdivision | text |
| `invoice.deliveryInformation.deliverToAddress.countryCode` | BT-80 | Deliver to country code | code |
| `invoice.invoicingPeriod` | BG-14 | Invoicing period | group |
| `invoice.invoicingPeriod.startDate` | BT-73 | Invoicing period start date | date |
| `invoice.invoicingPeriod.endDate` | BT-74 | Invoicing period end date | date |
| `invoice.paymentInstructions` | BG-16 | Payment instructions | group |
| `invoice.paymentInstructions.paymentMeansTypeCode` | BT-81 | Payment means type code | code |
| `invoice.paymentInstructions.paymentMeansText` | BT-82 | Payment means text | text |
| `invoice.paymentInstructions.remittanceInformation` | BT-83 | Remittance information | text |
| `invoice.paymentInstructions.creditTransfers[]` | BG-17 | Credit transfer | list of groups |
| `invoice.paymentInstructions.creditTransfers[].paymentAccountIdentifier` | BT-84 | Payment account identifier | identifier |
| `invoice.paymentInstructions.creditTransfers[].paymentAccountName` | BT-85 | Payment account name | text |
| `invoice.paymentInstructions.creditTransfers[].paymentServiceProviderIdentifier` | BT-86 | Payment service provider identifier | identifier |
| `invoice.paymentInstructions.paymentCard` | BG-18 | Payment card information | group |
| `invoice.paymentInstructions.paymentCard.primaryAccountNumber` | BT-87 | Payment card primary account number | text |
| `invoice.paymentInstructions.paymentCard.holderName` | BT-88 | Payment card holder name | text |
| `invoice.paymentInstructions.directDebit` | BG-19 | Direct debit | group |
| `invoice.paymentInstructions.directDebit.mandateReferenceIdentifier` | BT-89 | Mandate reference identifier | identifier |
| `invoice.paymentInstructions.directDebit.bankAssignedCreditorIdentifier` | BT-90 | Bank assigned creditor identifier | identifier |
| `invoice.paymentInstructions.directDebit.debitedAccountIdentifier` | BT-91 | Debited account identifier | identifier |
| `invoice.documentLevelAllowances[]` | BG-20 | Document level allowances | list of groups |
| `invoice.documentLevelAllowances[].amount` | BT-92 | Document level allowance amount | amount |
| `invoice.documentLevelAllowances[].baseAmount` | BT-93 | Document level allowance base amount | amount |
| `invoice.documentLevelAllowances[].percentage` | BT-94 | Document level allowance percentage | percentage |
| `invoice.documentLevelAllowances[].vatCategoryCode` | BT-95 | Document level allowance VAT category code | code |
| `invoice.documentLevelAllowances[].vatRate` | BT-96 | Document level allowance VAT rate | percentage |
| `invoice.documentLevelAllowances[].reason` | BT-97 | Document level allowance reason | text |
| `invoice.documentLevelAllowances[].reasonCode` | BT-98 | Document level allowance reason code | code |
| `invoice.documentLevelCharges[]` | BG-21 | Document level charges | list of groups |
| `invoice.documentLevelCharges[].amount` | BT-99 | Document level charge amount | amount |
| `invoice.documentLevelCharges[].baseAmount` | BT-100 | Document level charge base amount | amount |
| `invoice.documentLevelCharges[].percentage` | BT-101 | Document level charge percentage | percentage |
| `invoice.documentLevelCharges[].vatCategoryCode` | BT-102 | Document level charge VAT category code | code |
| `invoice.documentLevelCharges[].vatRate` | BT-103 | Document level charge VAT rate | percentage |
| `invoice.documentLevelCharges[].reason` | BT-104 | Document level charge reason | text |
| `invoice.documentLevelCharges[].reasonCode` | BT-105 | Document level charge reason code | code |
| `invoice.documentTotals` | BG-22 | Document totals | group |
| `invoice.documentTotals.sumOfLineNetAmounts` | BT-106 | Sum of Invoice line net amount | amount |
| `invoice.documentTotals.sumOfAllowances` | BT-107 | Sum of allowances on document level | amount |
| `invoice.documentTotals.sumOfCharges` | BT-108 | Sum of charges on document level | amount |
| `invoice.documentTotals.totalWithoutVat` | BT-109 | Invoice total amount without VAT | amount |
| `invoice.documentTotals.totalVatAmount` | BT-110 | Invoice total VAT amount | amount |
| `invoice.documentTotals.totalVatAmountInAccountingCurrency` | BT-111 | Invoice total VAT amount in accounting currency | amount |
| `invoice.documentTotals.totalWithVat` | BT-112 | Invoice total amount with VAT | amount |
| `invoice.documentTotals.paidAmount` | BT-113 | Paid amount | amount |
| `invoice.documentTotals.roundingAmount` | BT-114 | Rounding amount | amount |
| `invoice.documentTotals.amountDueForPayment` | BT-115 | Amount due for payment | amount |
| `invoice.vatBreakdowns[]` | BG-23 | VAT breakdown | list of groups |
| `invoice.vatBreakdowns[].taxableAmount` | BT-116 | VAT category taxable amount | amount |
| `invoice.vatBreakdowns[].taxAmount` | BT-117 | VAT category tax amount | amount |
| `invoice.vatBreakdowns[].categoryCode` | BT-118 | VAT category code | code |
| `invoice.vatBreakdowns[].rate` | BT-119 | VAT category rate | percentage |
| `invoice.vatBreakdowns[].exemptionReason` | BT-120 | VAT exemption reason text | text |
| `invoice.vatBreakdowns[].exemptionReasonCode` | BT-121 | VAT exemption reason code | code |
| `invoice.additionalSupportingDocuments[]` | BG-24 | Additional supporting documents | list of groups |
| `invoice.additionalSupportingDocuments[].reference` | BT-122 | Supporting document reference | reference |
| `invoice.additionalSupportingDocuments[].description` | BT-123 | Supporting document description | text |
| `invoice.additionalSupportingDocuments[].externalLocation` | BT-124 | External document location | text |
| `invoice.additionalSupportingDocuments[].attachedDocument` | BT-125 | Attached document | attachment |
| `invoice.lines[]` | BG-25 | Invoice line | list of groups |
| `invoice.lines[].identifier` | BT-126 | Invoice line identifier | identifier |
| `invoice.lines[].note` | BT-127 | Invoice line note | text |
| `invoice.lines[].objectIdentifier` | BT-128 | Invoice line object identifier | identifier |
| `invoice.lines[].invoicedQuantity` | BT-129 | Invoiced quantity | quantity |
| `invoice.lines[].invoicedQuantityUnitCode` | BT-130 | Invoiced quantity unit of measure code | code |
| `invoice.lines[].netAmount` | BT-131 | Invoice line net amount | amount |
| `invoice.lines[].purchaseOrderLineReference` | BT-132 | Referenced purchase order line reference | reference |
| `invoice.lines[].buyerAccountingReference` | BT-133 | Invoice line Buyer accounting reference | text |
| `invoice.lines[].period` | BG-26 | Invoice line period | group |
| `invoice.lines[].period.startDate` | BT-134 | Invoice line period start date | date |
| `invoice.lines[].period.endDate` | BT-135 | Invoice line period end date | date |
| `invoice.lines[].allowances[]` | BG-27 | Invoice line allowances | list of groups |
| `invoice.lines[].allowances[].amount` | BT-136 | Invoice line allowance amount | amount |
| `invoice.lines[].allowances[].baseAmount` | BT-137 | Invoice line allowance base amount | amount |
| `invoice.lines[].allowances[].percentage` | BT-138 | Invoice line allowance percentage | percentage |
| `invoice.lines[].allowances[].reason` | BT-139 | Invoice line allowance reason | text |
| `invoice.lines[].allowances[].reasonCode` | BT-140 | Invoice line allowance reason code | code |
| `invoice.lines[].charges[]` | BG-28 | Invoice line charges | list of groups |
| `invoice.lines[].charges[].amount` | BT-141 | Invoice line charge amount | amount |
| `invoice.lines[].charges[].baseAmount` | BT-142 | Invoice line charge base amount | amount |
| `invoice.lines[].charges[].percentage` | BT-143 | Invoice line charge percentage | percentage |
| `invoice.lines[].charges[].reason` | BT-144 | Invoice line charge reason | text |
| `invoice.lines[].charges[].reasonCode` | BT-145 | Invoice line charge reason code | code |
| `invoice.lines[].priceDetails` | BG-29 | Price details | group |
| `invoice.lines[].priceDetails.netPrice` | BT-146 | Item net price | unit price |
| `invoice.lines[].priceDetails.priceDiscount` | BT-147 | Item price discount | unit price |
| `invoice.lines[].priceDetails.grossPrice` | BT-148 | Item gross price | unit price |
| `invoice.lines[].priceDetails.baseQuantity` | BT-149 | Item price base quantity | quantity |
| `invoice.lines[].priceDetails.baseQuantityUnitCode` | BT-150 | Item price base quantity unit of measure | code |
| `invoice.lines[].vatInformation` | BG-30 | Line vat information | group |
| `invoice.lines[].vatInformation.vatCategoryCode` | BT-151 | Invoiced item VAT category code | code |
| `invoice.lines[].vatInformation.vatRate` | BT-152 | Invoiced item VAT rate | percentage |
| `invoice.lines[].item` | BG-31 | Item information | group |
| `invoice.lines[].item.name` | BT-153 | Item name | text |
| `invoice.lines[].item.description` | BT-154 | Item description | text |
| `invoice.lines[].item.sellersIdentifier` | BT-155 | Item Sellers identifier | identifier |
| `invoice.lines[].item.buyersIdentifier` | BT-156 | Item Buyers identifier | identifier |
| `invoice.lines[].item.standardIdentifier` | BT-157 | Item standard identifier | identifier |
| `invoice.lines[].item.classificationIdentifiers[]` | BT-158 | Item classification identifier | list of identifiers |
| `invoice.lines[].item.countryOfOrigin` | BT-159 | Item country of origin | code |
| `invoice.lines[].item.attributes[]` | BG-32 | Item attributes | list of groups |
| `invoice.lines[].item.attributes[].name` | BT-160 | Item attribute name | text |
| `invoice.lines[].item.attributes[].value` | BT-161 | Item attribute value | text |

## Try it locally

Get a token for `local-client` as [obtaining a token](obtaining-a-token.md)
describes, with `-d scope=e-invoicing.read`, and send the document as for
[`validate`](validating-an-invoice.md#try-it-locally), to
`http://<gateway>/modules/e-invoicing/v1/read`.
