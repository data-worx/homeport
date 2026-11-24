// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A textual note on the invoice (BG-1).</summary>
public sealed record InvoiceNote
{
    /// <summary>Invoice note subject code (BT-21).</summary>
    [BusinessTerm("BT-21")]
    public string? SubjectCode { get; init; }

    /// <summary>Invoice note (BT-22).</summary>
    [BusinessTerm("BT-22")]
    public string? Note { get; init; }
}
