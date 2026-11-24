// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The monetary totals of the invoice (BG-22).</summary>
public sealed record DocumentTotals
{
    /// <summary>Sum of Invoice line net amount (BT-106).</summary>
    public string? SumOfLineNetAmounts { get; init; }

    /// <summary>Sum of allowances on document level (BT-107).</summary>
    public string? SumOfAllowances { get; init; }

    /// <summary>Sum of charges on document level (BT-108).</summary>
    public string? SumOfCharges { get; init; }

    /// <summary>Invoice total amount without VAT (BT-109).</summary>
    public string? TotalWithoutVat { get; init; }

    /// <summary>Invoice total VAT amount (BT-110).</summary>
    public string? TotalVatAmount { get; init; }

    /// <summary>Invoice total VAT amount in accounting currency (BT-111).</summary>
    public string? TotalVatAmountInAccountingCurrency { get; init; }

    /// <summary>Invoice total amount with VAT (BT-112).</summary>
    public string? TotalWithVat { get; init; }

    /// <summary>Paid amount (BT-113).</summary>
    public string? PaidAmount { get; init; }

    /// <summary>Rounding amount (BT-114).</summary>
    public string? RoundingAmount { get; init; }

    /// <summary>Amount due for payment (BT-115).</summary>
    public string? AmountDueForPayment { get; init; }
}
