// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>The limits a tenant's clients get: a token bucket of requests.</summary>
public sealed class Plan
{
    /// <summary>The longest name of a plan.</summary>
    public const int MaxNameLength = 200;

    public required Guid Id { get; init; }

    public required string Name { get; set; }

    /// <summary>How fast the bucket refills.</summary>
    public required int RequestsPerSecond { get; set; }

    /// <summary>How many requests the bucket holds.</summary>
    public required int Burst { get; set; }
}
