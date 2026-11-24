// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The direct debit the invoice is paid by (BG-19).</summary>
public sealed record DirectDebit
{
    /// <summary>Mandate reference identifier (BT-89).</summary>
    public Identifier? MandateReferenceIdentifier { get; init; }

    /// <summary>Bank assigned creditor identifier (BT-90).</summary>
    public Identifier? BankAssignedCreditorIdentifier { get; init; }

    /// <summary>Debited account identifier (BT-91).</summary>
    public Identifier? DebitedAccountIdentifier { get; init; }
}
