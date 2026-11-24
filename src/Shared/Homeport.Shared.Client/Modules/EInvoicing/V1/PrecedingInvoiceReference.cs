// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A preceding invoice the invoice refers to, such as the one a credit note corrects (BG-3).</summary>
public sealed record PrecedingInvoiceReference
{
    /// <summary>Preceding Invoice reference (BT-25).</summary>
    public string? Reference { get; init; }

    /// <summary>Preceding Invoice issue date (BT-26).</summary>
    public DateOnly? IssueDate { get; init; }
}
