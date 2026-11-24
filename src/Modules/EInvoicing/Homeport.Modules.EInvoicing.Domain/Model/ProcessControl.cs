// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>The business process and the specification the invoice follows (BG-2).</summary>
public sealed record ProcessControl
{
    /// <summary>Business process type (BT-23).</summary>
    [BusinessTerm("BT-23")]
    public string? BusinessProcessType { get; init; }

    /// <summary>Specification identifier (BT-24).</summary>
    [BusinessTerm("BT-24")]
    public Identifier? SpecificationIdentifier { get; init; }
}
