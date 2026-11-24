// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The buyer (BG-7).</summary>
public sealed record Buyer
{
    /// <summary>Buyer name (BT-44).</summary>
    public string? Name { get; init; }

    /// <summary>Buyer trading name (BT-45).</summary>
    public string? TradingName { get; init; }

    /// <summary>Buyer identifier (BT-46).</summary>
    public Identifier? Identifier { get; init; }

    /// <summary>Buyer legal registration identifier (BT-47).</summary>
    public Identifier? LegalRegistrationIdentifier { get; init; }

    /// <summary>Buyer VAT identifier (BT-48).</summary>
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Buyer electronic address (BT-49).</summary>
    public Identifier? ElectronicAddress { get; init; }

    /// <summary>Buyer postal address (BG-8).</summary>
    public BuyerPostalAddress? PostalAddress { get; init; }

    /// <summary>Buyer contact (BG-9).</summary>
    public BuyerContact? Contact { get; init; }
}
