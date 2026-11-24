// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>An allowance on the whole invoice (BG-20).</summary>
public sealed record DocumentLevelAllowance
{
    /// <summary>Document level allowance amount (BT-92).</summary>
    [BusinessTerm("BT-92")]
    public string? Amount { get; init; }

    /// <summary>Document level allowance base amount (BT-93).</summary>
    [BusinessTerm("BT-93")]
    public string? BaseAmount { get; init; }

    /// <summary>Document level allowance percentage (BT-94).</summary>
    [BusinessTerm("BT-94")]
    public string? Percentage { get; init; }

    /// <summary>Document level allowance VAT category code (BT-95).</summary>
    [BusinessTerm("BT-95")]
    public string? VatCategoryCode { get; init; }

    /// <summary>Document level allowance VAT rate (BT-96).</summary>
    [BusinessTerm("BT-96")]
    public string? VatRate { get; init; }

    /// <summary>Document level allowance reason (BT-97).</summary>
    [BusinessTerm("BT-97")]
    public string? Reason { get; init; }

    /// <summary>Document level allowance reason code (BT-98).</summary>
    [BusinessTerm("BT-98")]
    public string? ReasonCode { get; init; }
}
