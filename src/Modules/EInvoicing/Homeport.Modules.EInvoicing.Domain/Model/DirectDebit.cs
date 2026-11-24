// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The direct debit the invoice is paid by (BG-19).</summary>
public sealed record DirectDebit
{
    /// <summary>Mandate reference identifier (BT-89).</summary>
    [BusinessTerm("BT-89")]
    public Identifier? MandateReferenceIdentifier { get; init; }

    /// <summary>Bank assigned creditor identifier (BT-90).</summary>
    [BusinessTerm("BT-90")]
    public Identifier? BankAssignedCreditorIdentifier { get; init; }

    /// <summary>Debited account identifier (BT-91).</summary>
    [BusinessTerm("BT-91")]
    public Identifier? DebitedAccountIdentifier { get; init; }
}
