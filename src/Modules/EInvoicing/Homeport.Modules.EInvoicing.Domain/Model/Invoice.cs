// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>An invoice in the semantic model of EN 16931, whichever syntax it came in.</summary>
/// <remarks>
/// Every member carries the identifier of the business term or group it
/// holds. Amounts, quantities, prices and percentages are kept as the
/// document writes them; a member is none, or empty, when the document does
/// not carry it or carries it in a form the module cannot read.
/// </remarks>
public sealed record Invoice
{
    /// <summary>Invoice number (BT-1).</summary>
    [BusinessTerm("BT-1")]
    public Identifier? Number { get; init; }

    /// <summary>Invoice issue date (BT-2).</summary>
    [BusinessTerm("BT-2")]
    public DateOnly? IssueDate { get; init; }

    /// <summary>Invoice type code (BT-3).</summary>
    [BusinessTerm("BT-3")]
    public string? TypeCode { get; init; }

    /// <summary>Invoice currency code (BT-5).</summary>
    [BusinessTerm("BT-5")]
    public string? CurrencyCode { get; init; }

    /// <summary>VAT accounting currency code (BT-6).</summary>
    [BusinessTerm("BT-6")]
    public string? VatAccountingCurrencyCode { get; init; }

    /// <summary>Value added tax point date (BT-7).</summary>
    [BusinessTerm("BT-7")]
    public DateOnly? VatPointDate { get; init; }

    /// <summary>Value added tax point date code (BT-8).</summary>
    [BusinessTerm("BT-8")]
    public string? VatPointDateCode { get; init; }

    /// <summary>Payment due date (BT-9).</summary>
    [BusinessTerm("BT-9")]
    public DateOnly? PaymentDueDate { get; init; }

    /// <summary>Buyer reference (BT-10).</summary>
    [BusinessTerm("BT-10")]
    public string? BuyerReference { get; init; }

    /// <summary>Project reference (BT-11).</summary>
    [BusinessTerm("BT-11")]
    public string? ProjectReference { get; init; }

    /// <summary>Contract reference (BT-12).</summary>
    [BusinessTerm("BT-12")]
    public string? ContractReference { get; init; }

    /// <summary>Purchase order reference (BT-13).</summary>
    [BusinessTerm("BT-13")]
    public string? PurchaseOrderReference { get; init; }

    /// <summary>Sales order reference (BT-14).</summary>
    [BusinessTerm("BT-14")]
    public string? SalesOrderReference { get; init; }

    /// <summary>Receiving advice reference (BT-15).</summary>
    [BusinessTerm("BT-15")]
    public string? ReceivingAdviceReference { get; init; }

    /// <summary>Despatch advice reference (BT-16).</summary>
    [BusinessTerm("BT-16")]
    public string? DespatchAdviceReference { get; init; }

    /// <summary>Tender or lot reference (BT-17).</summary>
    [BusinessTerm("BT-17")]
    public string? TenderOrLotReference { get; init; }

    /// <summary>Invoiced object identifier (BT-18).</summary>
    [BusinessTerm("BT-18")]
    public Identifier? InvoicedObjectIdentifier { get; init; }

    /// <summary>Buyer accounting reference (BT-19).</summary>
    [BusinessTerm("BT-19")]
    public string? BuyerAccountingReference { get; init; }

    /// <summary>Payment terms (BT-20).</summary>
    [BusinessTerm("BT-20")]
    public string? PaymentTerms { get; init; }

    /// <summary>Every invoice note (BG-1); empty when there is none.</summary>
    [BusinessTerm("BG-1")]
    public IReadOnlyList<InvoiceNote> Notes { get; init; } = [];

    /// <summary>Process control (BG-2).</summary>
    [BusinessTerm("BG-2")]
    public ProcessControl? ProcessControl { get; init; }

    /// <summary>Every preceding invoice reference (BG-3); empty when there is none.</summary>
    [BusinessTerm("BG-3")]
    public IReadOnlyList<PrecedingInvoiceReference> PrecedingInvoiceReferences { get; init; } = [];

    /// <summary>Seller (BG-4).</summary>
    [BusinessTerm("BG-4")]
    public Seller? Seller { get; init; }

    /// <summary>Buyer (BG-7).</summary>
    [BusinessTerm("BG-7")]
    public Buyer? Buyer { get; init; }

    /// <summary>Payee (BG-10).</summary>
    [BusinessTerm("BG-10")]
    public Payee? Payee { get; init; }

    /// <summary>Seller tax representative party (BG-11).</summary>
    [BusinessTerm("BG-11")]
    public SellerTaxRepresentativeParty? SellerTaxRepresentative { get; init; }

    /// <summary>Delivery information (BG-13).</summary>
    [BusinessTerm("BG-13")]
    public DeliveryInformation? DeliveryInformation { get; init; }

    /// <summary>Invoicing period (BG-14).</summary>
    [BusinessTerm("BG-14")]
    public InvoicingPeriod? InvoicingPeriod { get; init; }

    /// <summary>Payment instructions (BG-16).</summary>
    [BusinessTerm("BG-16")]
    public PaymentInstructions? PaymentInstructions { get; init; }

    /// <summary>Every document level allowances (BG-20); empty when there is none.</summary>
    [BusinessTerm("BG-20")]
    public IReadOnlyList<DocumentLevelAllowance> DocumentLevelAllowances { get; init; } = [];

    /// <summary>Every document level charges (BG-21); empty when there is none.</summary>
    [BusinessTerm("BG-21")]
    public IReadOnlyList<DocumentLevelCharge> DocumentLevelCharges { get; init; } = [];

    /// <summary>Document totals (BG-22).</summary>
    [BusinessTerm("BG-22")]
    public DocumentTotals? DocumentTotals { get; init; }

    /// <summary>Every vAT breakdown (BG-23); empty when there is none.</summary>
    [BusinessTerm("BG-23")]
    public IReadOnlyList<VatBreakdown> VatBreakdowns { get; init; } = [];

    /// <summary>Every additional supporting documents (BG-24); empty when there is none.</summary>
    [BusinessTerm("BG-24")]
    public IReadOnlyList<AdditionalSupportingDocument> AdditionalSupportingDocuments { get; init; } = [];

    /// <summary>Every invoice line (BG-25); empty when there is none.</summary>
    [BusinessTerm("BG-25")]
    public IReadOnlyList<InvoiceLine> Lines { get; init; } = [];
}
