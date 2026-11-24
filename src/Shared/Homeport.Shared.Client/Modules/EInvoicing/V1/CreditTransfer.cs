// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An account the payment is to be transferred to (BG-17).</summary>
public sealed record CreditTransfer
{
    /// <summary>Payment account identifier (BT-84).</summary>
    public Identifier? PaymentAccountIdentifier { get; init; }

    /// <summary>Payment account name (BT-85).</summary>
    public string? PaymentAccountName { get; init; }

    /// <summary>Payment service provider identifier (BT-86).</summary>
    public Identifier? PaymentServiceProviderIdentifier { get; init; }
}
