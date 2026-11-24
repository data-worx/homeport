// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The tax representative of the seller (BG-11).</summary>
public sealed record SellerTaxRepresentativeParty
{
    /// <summary>Seller tax representative name (BT-62).</summary>
    public string? Name { get; init; }

    /// <summary>Seller tax representative VAT identifier (BT-63).</summary>
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Seller tax representative postal address (BG-12).</summary>
    public SellerTaxRepresentativePostalAddress? PostalAddress { get; init; }
}
