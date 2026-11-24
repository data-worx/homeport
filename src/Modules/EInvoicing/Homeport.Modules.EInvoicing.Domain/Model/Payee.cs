// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The payee, when it is not the seller (BG-10).</summary>
public sealed record Payee
{
    /// <summary>Payee name (BT-59).</summary>
    [BusinessTerm("BT-59")]
    public string? Name { get; init; }

    /// <summary>Payee identifier (BT-60).</summary>
    [BusinessTerm("BT-60")]
    public Identifier? Identifier { get; init; }

    /// <summary>Payee legal registration identifier (BT-61).</summary>
    [BusinessTerm("BT-61")]
    public Identifier? LegalRegistrationIdentifier { get; init; }
}
