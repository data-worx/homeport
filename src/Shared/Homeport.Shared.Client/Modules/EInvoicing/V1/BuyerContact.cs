// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The contact of the buyer (BG-9).</summary>
public sealed record BuyerContact
{
    /// <summary>Buyer contact point (BT-56).</summary>
    public string? ContactPoint { get; init; }

    /// <summary>Buyer contact telephone number (BT-57).</summary>
    public string? TelephoneNumber { get; init; }

    /// <summary>Buyer contact email address (BT-58).</summary>
    public string? EmailAddress { get; init; }
}
