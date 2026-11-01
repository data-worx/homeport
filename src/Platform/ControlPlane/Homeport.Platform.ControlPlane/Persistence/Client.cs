// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Persistence;

/// <summary>A machine identity of a tenant, and the plan its requests are held to.</summary>
public sealed class Client
{
    /// <summary>The client id as the identity provider issued it.</summary>
    public required string ClientId { get; init; }

    public required Guid TenantId { get; set; }

    public required Guid PlanId { get; set; }

    public Plan? Plan { get; set; }
}
