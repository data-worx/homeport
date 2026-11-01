// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Persistence;

/// <summary>A department or customer of the operator: the unit of isolation and of metering.</summary>
public sealed class Tenant
{
    public required Guid Id { get; init; }

    public required string Name { get; set; }
}
