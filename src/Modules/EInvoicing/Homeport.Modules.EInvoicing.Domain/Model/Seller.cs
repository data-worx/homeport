// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The seller (BG-4).</summary>
public sealed record Seller
{
    /// <summary>Seller name (BT-27).</summary>
    [BusinessTerm("BT-27")]
    public string? Name { get; init; }

    /// <summary>Seller trading name (BT-28).</summary>
    [BusinessTerm("BT-28")]
    public string? TradingName { get; init; }

    /// <summary>Every seller identifier (BT-29); empty when there is none.</summary>
    [BusinessTerm("BT-29")]
    public IReadOnlyList<Identifier> Identifiers { get; init; } = [];

    /// <summary>Seller legal registration identifier (BT-30).</summary>
    [BusinessTerm("BT-30")]
    public Identifier? LegalRegistrationIdentifier { get; init; }

    /// <summary>Seller VAT identifier (BT-31).</summary>
    [BusinessTerm("BT-31")]
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Seller tax registration identifier (BT-32).</summary>
    [BusinessTerm("BT-32")]
    public Identifier? TaxRegistrationIdentifier { get; init; }

    /// <summary>Seller additional legal information (BT-33).</summary>
    [BusinessTerm("BT-33")]
    public string? AdditionalLegalInformation { get; init; }

    /// <summary>Seller electronic address (BT-34).</summary>
    [BusinessTerm("BT-34")]
    public Identifier? ElectronicAddress { get; init; }

    /// <summary>Seller postal address (BG-5).</summary>
    [BusinessTerm("BG-5")]
    public SellerPostalAddress? PostalAddress { get; init; }

    /// <summary>Seller contact (BG-6).</summary>
    [BusinessTerm("BG-6")]
    public SellerContact? Contact { get; init; }
}
