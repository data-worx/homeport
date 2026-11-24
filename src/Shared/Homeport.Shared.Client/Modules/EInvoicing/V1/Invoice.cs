// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An invoice in the semantic model of EN 16931, whichever syntax it came in.</summary>
/// <remarks>
/// Amounts, quantities, prices and percentages are strings, exactly as the
/// document writes them. A member is <see langword="null"/>, or empty, when
/// the document does not carry it or carries it in a form the module cannot
/// read. Attachments are described, never returned.
/// </remarks>
public sealed record Invoice
{
    /// <summary>Invoice number (BT-1).</summary>
    public Identifier? Number { get; init; }

    /// <summary>Invoice issue date (BT-2).</summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>Invoice type code (BT-3).</summary>
    public string? TypeCode { get; init; }

    /// <summary>Invoice currency code (BT-5).</summary>
    public string? CurrencyCode { get; init; }

    /// <summary>VAT accounting currency code (BT-6).</summary>
    public string? VatAccountingCurrencyCode { get; init; }

    /// <summary>Value added tax point date (BT-7).</summary>
    public DateOnly? VatPointDate { get; init; }

    /// <summary>Value added tax point date code (BT-8).</summary>
    public string? VatPointDateCode { get; init; }

    /// <summary>Payment due date (BT-9).</summary>
    public DateOnly? PaymentDueDate { get; init; }

    /// <summary>Buyer reference (BT-10).</summary>
    public string? BuyerReference { get; init; }

    /// <summary>Project reference (BT-11).</summary>
    public string? ProjectReference { get; init; }

    /// <summary>Contract reference (BT-12).</summary>
    public string? ContractReference { get; init; }

    /// <summary>Purchase order reference (BT-13).</summary>
    public string? PurchaseOrderReference { get; init; }

    /// <summary>Sales order reference (BT-14).</summary>
    public string? SalesOrderReference { get; init; }

    /// <summary>Receiving advice reference (BT-15).</summary>
    public string? ReceivingAdviceReference { get; init; }

    /// <summary>Despatch advice reference (BT-16).</summary>
    public string? DespatchAdviceReference { get; init; }

    /// <summary>Tender or lot reference (BT-17).</summary>
    public string? TenderOrLotReference { get; init; }

    /// <summary>Invoiced object identifier (BT-18).</summary>
    public Identifier? InvoicedObjectIdentifier { get; init; }

    /// <summary>Buyer accounting reference (BT-19).</summary>
    public string? BuyerAccountingReference { get; init; }

    /// <summary>Payment terms (BT-20).</summary>
    public string? PaymentTerms { get; init; }

    /// <summary>Every invoice note (BG-1); empty when there is none.</summary>
    public IReadOnlyList<InvoiceNote> Notes { get; init; } = [];

    /// <summary>Process control (BG-2).</summary>
    public ProcessControl? ProcessControl { get; init; }

    /// <summary>Every preceding invoice reference (BG-3); empty when there is none.</summary>
    public IReadOnlyList<PrecedingInvoiceReference> PrecedingInvoiceReferences { get; init; } = [];

    /// <summary>Seller (BG-4).</summary>
    public Seller? Seller { get; init; }

    /// <summary>Buyer (BG-7).</summary>
    public Buyer? Buyer { get; init; }

    /// <summary>Payee (BG-10).</summary>
    public Payee? Payee { get; init; }

    /// <summary>Seller tax representative party (BG-11).</summary>
    public SellerTaxRepresentativeParty? SellerTaxRepresentative { get; init; }

    /// <summary>Delivery information (BG-13).</summary>
    public DeliveryInformation? DeliveryInformation { get; init; }

    /// <summary>Invoicing period (BG-14).</summary>
    public InvoicingPeriod? InvoicingPeriod { get; init; }

    /// <summary>Payment instructions (BG-16).</summary>
    public PaymentInstructions? PaymentInstructions { get; init; }

    /// <summary>Every document level allowances (BG-20); empty when there is none.</summary>
    public IReadOnlyList<DocumentLevelAllowance> DocumentLevelAllowances { get; init; } = [];

    /// <summary>Every document level charges (BG-21); empty when there is none.</summary>
    public IReadOnlyList<DocumentLevelCharge> DocumentLevelCharges { get; init; } = [];

    /// <summary>Document totals (BG-22).</summary>
    public DocumentTotals? DocumentTotals { get; init; }

    /// <summary>Every vAT breakdown (BG-23); empty when there is none.</summary>
    public IReadOnlyList<VatBreakdown> VatBreakdowns { get; init; } = [];

    /// <summary>Every additional supporting documents (BG-24); empty when there is none.</summary>
    public IReadOnlyList<AdditionalSupportingDocument> AdditionalSupportingDocuments { get; init; } = [];

    /// <summary>Every invoice line (BG-25); empty when there is none.</summary>
    public IReadOnlyList<InvoiceLine> Lines { get; init; } = [];
}
