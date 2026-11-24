// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>How the invoice is to be paid (BG-16).</summary>
public sealed record PaymentInstructions
{
    /// <summary>Payment means type code (BT-81).</summary>
    [BusinessTerm("BT-81")]
    public string? PaymentMeansTypeCode { get; init; }

    /// <summary>Payment means text (BT-82).</summary>
    [BusinessTerm("BT-82")]
    public string? PaymentMeansText { get; init; }

    /// <summary>Remittance information (BT-83).</summary>
    [BusinessTerm("BT-83")]
    public string? RemittanceInformation { get; init; }

    /// <summary>Every credit transfer (BG-17); empty when there is none.</summary>
    [BusinessTerm("BG-17")]
    public IReadOnlyList<CreditTransfer> CreditTransfers { get; init; } = [];

    /// <summary>Payment card information (BG-18).</summary>
    [BusinessTerm("BG-18")]
    public PaymentCardInformation? PaymentCard { get; init; }

    /// <summary>Direct debit (BG-19).</summary>
    [BusinessTerm("BG-19")]
    public DirectDebit? DirectDebit { get; init; }
}
