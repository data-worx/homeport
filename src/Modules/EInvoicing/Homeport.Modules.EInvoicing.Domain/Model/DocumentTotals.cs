// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The monetary totals of the invoice (BG-22).</summary>
public sealed record DocumentTotals
{
    /// <summary>Sum of Invoice line net amount (BT-106).</summary>
    [BusinessTerm("BT-106")]
    public string? SumOfLineNetAmounts { get; init; }

    /// <summary>Sum of allowances on document level (BT-107).</summary>
    [BusinessTerm("BT-107")]
    public string? SumOfAllowances { get; init; }

    /// <summary>Sum of charges on document level (BT-108).</summary>
    [BusinessTerm("BT-108")]
    public string? SumOfCharges { get; init; }

    /// <summary>Invoice total amount without VAT (BT-109).</summary>
    [BusinessTerm("BT-109")]
    public string? TotalWithoutVat { get; init; }

    /// <summary>Invoice total VAT amount (BT-110).</summary>
    [BusinessTerm("BT-110")]
    public string? TotalVatAmount { get; init; }

    /// <summary>Invoice total VAT amount in accounting currency (BT-111).</summary>
    [BusinessTerm("BT-111")]
    public string? TotalVatAmountInAccountingCurrency { get; init; }

    /// <summary>Invoice total amount with VAT (BT-112).</summary>
    [BusinessTerm("BT-112")]
    public string? TotalWithVat { get; init; }

    /// <summary>Paid amount (BT-113).</summary>
    [BusinessTerm("BT-113")]
    public string? PaidAmount { get; init; }

    /// <summary>Rounding amount (BT-114).</summary>
    [BusinessTerm("BT-114")]
    public string? RoundingAmount { get; init; }

    /// <summary>Amount due for payment (BT-115).</summary>
    [BusinessTerm("BT-115")]
    public string? AmountDueForPayment { get; init; }
}
