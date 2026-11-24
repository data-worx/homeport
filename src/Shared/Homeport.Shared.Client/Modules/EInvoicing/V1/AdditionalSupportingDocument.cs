// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>A document that supports the invoice (BG-24).</summary>
public sealed record AdditionalSupportingDocument
{
    /// <summary>Supporting document reference (BT-122).</summary>
    public string? Reference { get; init; }

    /// <summary>Supporting document description (BT-123).</summary>
    public string? Description { get; init; }

    /// <summary>External document location (BT-124).</summary>
    public string? ExternalLocation { get; init; }

    /// <summary>Attached document (BT-125).</summary>
    public BinaryObject? AttachedDocument { get; init; }
}
