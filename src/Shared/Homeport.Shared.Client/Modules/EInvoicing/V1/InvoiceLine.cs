// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A line of the invoice (BG-25).</summary>
public sealed record InvoiceLine
{
    /// <summary>Invoice line identifier (BT-126).</summary>
    public Identifier? Identifier { get; init; }

    /// <summary>Invoice line note (BT-127).</summary>
    public string? Note { get; init; }

    /// <summary>Invoice line object identifier (BT-128).</summary>
    public Identifier? ObjectIdentifier { get; init; }

    /// <summary>Invoiced quantity (BT-129).</summary>
    public string? InvoicedQuantity { get; init; }

    /// <summary>Invoiced quantity unit of measure code (BT-130).</summary>
    public string? InvoicedQuantityUnitCode { get; init; }

    /// <summary>Invoice line net amount (BT-131).</summary>
    public string? NetAmount { get; init; }

    /// <summary>Referenced purchase order line reference (BT-132).</summary>
    public string? PurchaseOrderLineReference { get; init; }

    /// <summary>Invoice line Buyer accounting reference (BT-133).</summary>
    public string? BuyerAccountingReference { get; init; }

    /// <summary>Invoice line period (BG-26).</summary>
    public InvoiceLinePeriod? Period { get; init; }

    /// <summary>Every invoice line allowances (BG-27); empty when there is none.</summary>
    public IReadOnlyList<InvoiceLineAllowance> Allowances { get; init; } = [];

    /// <summary>Every invoice line charges (BG-28); empty when there is none.</summary>
    public IReadOnlyList<InvoiceLineCharge> Charges { get; init; } = [];

    /// <summary>Price details (BG-29).</summary>
    public PriceDetails? PriceDetails { get; init; }

    /// <summary>Line VAT information (BG-30).</summary>
    public LineVatInformation? VatInformation { get; init; }

    /// <summary>Item information (BG-31).</summary>
    public ItemInformation? Item { get; init; }
}
