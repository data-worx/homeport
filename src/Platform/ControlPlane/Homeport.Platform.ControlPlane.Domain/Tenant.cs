// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>A department or customer of the operator: the unit of isolation and of metering.</summary>
public sealed class Tenant
{
    /// <summary>The longest name of a tenant.</summary>
    public const int MaxNameLength = 200;

    public required Guid Id { get; init; }

    public required string Name { get; set; }
}
