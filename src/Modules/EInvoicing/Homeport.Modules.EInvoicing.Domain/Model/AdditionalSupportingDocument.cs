// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>A document that supports the invoice (BG-24).</summary>
public sealed record AdditionalSupportingDocument
{
    /// <summary>Supporting document reference (BT-122).</summary>
    [BusinessTerm("BT-122")]
    public string? Reference { get; init; }

    /// <summary>Supporting document description (BT-123).</summary>
    [BusinessTerm("BT-123")]
    public string? Description { get; init; }

    /// <summary>External document location (BT-124).</summary>
    [BusinessTerm("BT-124")]
    public string? ExternalLocation { get; init; }

    /// <summary>Attached document (BT-125).</summary>
    [BusinessTerm("BT-125")]
    public BinaryObject? AttachedDocument { get; init; }
}
