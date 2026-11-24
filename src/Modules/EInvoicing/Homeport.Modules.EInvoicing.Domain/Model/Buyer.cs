// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The buyer (BG-7).</summary>
public sealed record Buyer
{
    /// <summary>Buyer name (BT-44).</summary>
    [BusinessTerm("BT-44")]
    public string? Name { get; init; }

    /// <summary>Buyer trading name (BT-45).</summary>
    [BusinessTerm("BT-45")]
    public string? TradingName { get; init; }

    /// <summary>Buyer identifier (BT-46).</summary>
    [BusinessTerm("BT-46")]
    public Identifier? Identifier { get; init; }

    /// <summary>Buyer legal registration identifier (BT-47).</summary>
    [BusinessTerm("BT-47")]
    public Identifier? LegalRegistrationIdentifier { get; init; }

    /// <summary>Buyer VAT identifier (BT-48).</summary>
    [BusinessTerm("BT-48")]
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Buyer electronic address (BT-49).</summary>
    [BusinessTerm("BT-49")]
    public Identifier? ElectronicAddress { get; init; }

    /// <summary>Buyer postal address (BG-8).</summary>
    [BusinessTerm("BG-8")]
    public BuyerPostalAddress? PostalAddress { get; init; }

    /// <summary>Buyer contact (BG-9).</summary>
    [BusinessTerm("BG-9")]
    public BuyerContact? Contact { get; init; }
}
