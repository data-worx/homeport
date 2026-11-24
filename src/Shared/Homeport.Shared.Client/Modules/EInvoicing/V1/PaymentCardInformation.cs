// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The payment card the invoice is paid with (BG-18).</summary>
public sealed record PaymentCardInformation
{
    /// <summary>Payment card primary account number (BT-87).</summary>
    public string? PrimaryAccountNumber { get; init; }

    /// <summary>Payment card holder name (BT-88).</summary>
    public string? HolderName { get; init; }
}
