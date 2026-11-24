// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An allowance on the whole invoice (BG-20).</summary>
public sealed record DocumentLevelAllowance
{
    /// <summary>Document level allowance amount (BT-92).</summary>
    public string? Amount { get; init; }

    /// <summary>Document level allowance base amount (BT-93).</summary>
    public string? BaseAmount { get; init; }

    /// <summary>Document level allowance percentage (BT-94).</summary>
    public string? Percentage { get; init; }

    /// <summary>Document level allowance VAT category code (BT-95).</summary>
    public string? VatCategoryCode { get; init; }

    /// <summary>Document level allowance VAT rate (BT-96).</summary>
    public string? VatRate { get; init; }

    /// <summary>Document level allowance reason (BT-97).</summary>
    public string? Reason { get; init; }

    /// <summary>Document level allowance reason code (BT-98).</summary>
    public string? ReasonCode { get; init; }
}
