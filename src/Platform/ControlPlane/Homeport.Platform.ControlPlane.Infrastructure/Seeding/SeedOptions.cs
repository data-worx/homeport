// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Infrastructure.Seeding;

/// <summary>
/// The one tenant, plan and client <c>migrate</c> keeps in the database, such
/// as <c>Seed:TenantId</c>. Until the admin portal brings a write API, they
/// are the control plane's whole content.
/// </summary>
public sealed class SeedOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Seed";

    public Guid TenantId { get; set; }

    public string TenantName { get; set; } = string.Empty;

    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = string.Empty;

    /// <summary>How fast the plan's token bucket refills.</summary>
    public int RequestsPerSecond { get; set; }

    /// <summary>How many requests the plan's token bucket holds.</summary>
    public int Burst { get; set; }

    /// <summary>The client id as the identity provider issued it.</summary>
    public string ClientId { get; set; } = string.Empty;
}
