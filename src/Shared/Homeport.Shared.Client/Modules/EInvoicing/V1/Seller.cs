// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The seller (BG-4).</summary>
public sealed record Seller
{
    /// <summary>Seller name (BT-27).</summary>
    public string? Name { get; init; }

    /// <summary>Seller trading name (BT-28).</summary>
    public string? TradingName { get; init; }

    /// <summary>Every seller identifier (BT-29); empty when there is none.</summary>
    public IReadOnlyList<Identifier> Identifiers { get; init; } = [];

    /// <summary>Seller legal registration identifier (BT-30).</summary>
    public Identifier? LegalRegistrationIdentifier { get; init; }

    /// <summary>Seller VAT identifier (BT-31).</summary>
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Seller tax registration identifier (BT-32).</summary>
    public Identifier? TaxRegistrationIdentifier { get; init; }

    /// <summary>Seller additional legal information (BT-33).</summary>
    public string? AdditionalLegalInformation { get; init; }

    /// <summary>Seller electronic address (BT-34).</summary>
    public Identifier? ElectronicAddress { get; init; }

    /// <summary>Seller postal address (BG-5).</summary>
    public SellerPostalAddress? PostalAddress { get; init; }

    /// <summary>Seller contact (BG-6).</summary>
    public SellerContact? Contact { get; init; }
}
