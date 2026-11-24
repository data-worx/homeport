// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A line of the invoice (BG-25).</summary>
public sealed record InvoiceLine
{
    /// <summary>Invoice line identifier (BT-126).</summary>
    [BusinessTerm("BT-126")]
    public Identifier? Identifier { get; init; }

    /// <summary>Invoice line note (BT-127).</summary>
    [BusinessTerm("BT-127")]
    public string? Note { get; init; }

    /// <summary>Invoice line object identifier (BT-128).</summary>
    [BusinessTerm("BT-128")]
    public Identifier? ObjectIdentifier { get; init; }

    /// <summary>Invoiced quantity (BT-129).</summary>
    [BusinessTerm("BT-129")]
    public string? InvoicedQuantity { get; init; }

    /// <summary>Invoiced quantity unit of measure code (BT-130).</summary>
    [BusinessTerm("BT-130")]
    public string? InvoicedQuantityUnitCode { get; init; }

    /// <summary>Invoice line net amount (BT-131).</summary>
    [BusinessTerm("BT-131")]
    public string? NetAmount { get; init; }

    /// <summary>Referenced purchase order line reference (BT-132).</summary>
    [BusinessTerm("BT-132")]
    public string? PurchaseOrderLineReference { get; init; }

    /// <summary>Invoice line Buyer accounting reference (BT-133).</summary>
    [BusinessTerm("BT-133")]
    public string? BuyerAccountingReference { get; init; }

    /// <summary>Invoice line period (BG-26).</summary>
    [BusinessTerm("BG-26")]
    public InvoiceLinePeriod? Period { get; init; }

    /// <summary>Every invoice line allowances (BG-27); empty when there is none.</summary>
    [BusinessTerm("BG-27")]
    public IReadOnlyList<InvoiceLineAllowance> Allowances { get; init; } = [];

    /// <summary>Every invoice line charges (BG-28); empty when there is none.</summary>
    [BusinessTerm("BG-28")]
    public IReadOnlyList<InvoiceLineCharge> Charges { get; init; } = [];

    /// <summary>Price details (BG-29).</summary>
    [BusinessTerm("BG-29")]
    public PriceDetails? PriceDetails { get; init; }

    /// <summary>Line VAT information (BG-30).</summary>
    [BusinessTerm("BG-30")]
    public LineVatInformation? VatInformation { get; init; }

    /// <summary>Item information (BG-31).</summary>
    [BusinessTerm("BG-31")]
    public ItemInformation? Item { get; init; }
}
