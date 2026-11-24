// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>An account the payment is to be transferred to (BG-17).</summary>
public sealed record CreditTransfer
{
    /// <summary>Payment account identifier (BT-84).</summary>
    [BusinessTerm("BT-84")]
    public Identifier? PaymentAccountIdentifier { get; init; }

    /// <summary>Payment account name (BT-85).</summary>
    [BusinessTerm("BT-85")]
    public string? PaymentAccountName { get; init; }

    /// <summary>Payment service provider identifier (BT-86).</summary>
    [BusinessTerm("BT-86")]
    public Identifier? PaymentServiceProviderIdentifier { get; init; }
}
