// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A preceding invoice the invoice refers to, such as the one a credit note corrects (BG-3).</summary>
public sealed record PrecedingInvoiceReference
{
    /// <summary>Preceding Invoice reference (BT-25).</summary>
    [BusinessTerm("BT-25")]
    public string? Reference { get; init; }

    /// <summary>Preceding Invoice issue date (BT-26).</summary>
    [BusinessTerm("BT-26")]
    public DateOnly? IssueDate { get; init; }
}
