// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>An attribute of the item of a line (BG-32).</summary>
public sealed record ItemAttribute
{
    /// <summary>Item attribute name (BT-160).</summary>
    [BusinessTerm("BT-160")]
    public string? Name { get; init; }

    /// <summary>Item attribute value (BT-161).</summary>
    [BusinessTerm("BT-161")]
    public string? Value { get; init; }
}
