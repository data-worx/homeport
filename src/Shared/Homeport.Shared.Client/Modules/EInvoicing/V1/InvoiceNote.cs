// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A textual note on the invoice (BG-1).</summary>
public sealed record InvoiceNote
{
    /// <summary>Invoice note subject code (BT-21).</summary>
    public string? SubjectCode { get; init; }

    /// <summary>Invoice note (BT-22).</summary>
    public string? Note { get; init; }
}
