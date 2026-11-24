// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>The business process and the specification the invoice follows (BG-2).</summary>
public sealed record ProcessControl
{
    /// <summary>Business process type (BT-23).</summary>
    public string? BusinessProcessType { get; init; }

    /// <summary>Specification identifier (BT-24).</summary>
    public Identifier? SpecificationIdentifier { get; init; }
}
