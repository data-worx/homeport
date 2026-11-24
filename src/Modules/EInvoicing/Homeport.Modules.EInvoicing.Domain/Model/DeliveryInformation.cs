// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Model;

/// <summary>Where and when the goods and services were delivered (BG-13).</summary>
public sealed record DeliveryInformation
{
    /// <summary>Deliver to party name (BT-70).</summary>
    [BusinessTerm("BT-70")]
    public string? DeliverToPartyName { get; init; }

    /// <summary>Deliver to location identifier (BT-71).</summary>
    [BusinessTerm("BT-71")]
    public Identifier? DeliverToLocationIdentifier { get; init; }

    /// <summary>Actual delivery date (BT-72).</summary>
    [BusinessTerm("BT-72")]
    public DateOnly? ActualDeliveryDate { get; init; }

    /// <summary>Deliver to address (BG-15).</summary>
    [BusinessTerm("BG-15")]
    public DeliverToAddress? DeliverToAddress { get; init; }
}
