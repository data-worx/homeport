// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>An attribute of the item of a line (BG-32).</summary>
public sealed record ItemAttribute
{
    /// <summary>Item attribute name (BT-160).</summary>
    public string? Name { get; init; }

    /// <summary>Item attribute value (BT-161).</summary>
    public string? Value { get; init; }
}
