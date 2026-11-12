// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>A machine identity of a tenant, and the plan its requests are held to.</summary>
public sealed class Client
{
    /// <summary>The longest client id; the identity providers Homeport targets stay well below.</summary>
    public const int MaxClientIdLength = 255;

    /// <summary>The client id as the identity provider issued it.</summary>
    public required string ClientId { get; init; }

    public required Guid TenantId { get; set; }

    public required Guid PlanId { get; set; }

    public Plan? Plan { get; set; }
}
