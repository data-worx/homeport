// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>Where and when the goods and services were delivered (BG-13).</summary>
public sealed record DeliveryInformation
{
    /// <summary>Deliver to party name (BT-70).</summary>
    public string? DeliverToPartyName { get; init; }

    /// <summary>Deliver to location identifier (BT-71).</summary>
    public Identifier? DeliverToLocationIdentifier { get; init; }

    /// <summary>Actual delivery date (BT-72).</summary>
    public DateOnly? ActualDeliveryDate { get; init; }

    /// <summary>Deliver to address (BG-15).</summary>
    public DeliverToAddress? DeliverToAddress { get; init; }
}
