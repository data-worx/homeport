// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>How the invoice is to be paid (BG-16).</summary>
public sealed record PaymentInstructions
{
    /// <summary>Payment means type code (BT-81).</summary>
    public string? PaymentMeansTypeCode { get; init; }

    /// <summary>Payment means text (BT-82).</summary>
    public string? PaymentMeansText { get; init; }

    /// <summary>Remittance information (BT-83).</summary>
    public string? RemittanceInformation { get; init; }

    /// <summary>Every credit transfer (BG-17); empty when there is none.</summary>
    public IReadOnlyList<CreditTransfer> CreditTransfers { get; init; } = [];

    /// <summary>Payment card information (BG-18).</summary>
    public PaymentCardInformation? PaymentCard { get; init; }

    /// <summary>Direct debit (BG-19).</summary>
    public DirectDebit? DirectDebit { get; init; }
}
