// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The payee, when it is not the seller (BG-10).</summary>
public sealed record Payee
{
    /// <summary>Payee name (BT-59).</summary>
    public string? Name { get; init; }

    /// <summary>Payee identifier (BT-60).</summary>
    public Identifier? Identifier { get; init; }

    /// <summary>Payee legal registration identifier (BT-61).</summary>
    public Identifier? LegalRegistrationIdentifier { get; init; }
}
