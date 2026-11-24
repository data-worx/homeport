// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The contact of the buyer (BG-9).</summary>
public sealed record BuyerContact
{
    /// <summary>Buyer contact point (BT-56).</summary>
    [BusinessTerm("BT-56")]
    public string? ContactPoint { get; init; }

    /// <summary>Buyer contact telephone number (BT-57).</summary>
    [BusinessTerm("BT-57")]
    public string? TelephoneNumber { get; init; }

    /// <summary>Buyer contact email address (BT-58).</summary>
    [BusinessTerm("BT-58")]
    public string? EmailAddress { get; init; }
}
