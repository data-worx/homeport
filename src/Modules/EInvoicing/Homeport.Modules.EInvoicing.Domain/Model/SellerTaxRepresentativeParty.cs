// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The tax representative of the seller (BG-11).</summary>
public sealed record SellerTaxRepresentativeParty
{
    /// <summary>Seller tax representative name (BT-62).</summary>
    [BusinessTerm("BT-62")]
    public string? Name { get; init; }

    /// <summary>Seller tax representative VAT identifier (BT-63).</summary>
    [BusinessTerm("BT-63")]
    public Identifier? VatIdentifier { get; init; }

    /// <summary>Seller tax representative postal address (BG-12).</summary>
    [BusinessTerm("BG-12")]
    public SellerTaxRepresentativePostalAddress? PostalAddress { get; init; }
}
