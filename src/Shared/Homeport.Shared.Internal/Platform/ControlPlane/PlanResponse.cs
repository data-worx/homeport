// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.ControlPlane;

/// <summary>The rate limit of a plan: a token bucket per tenant.</summary>
/// <param name="PlanId">Identifies the plan.</param>
/// <param name="RequestsPerSecond">How fast the bucket refills.</param>
/// <param name="Burst">How many requests the bucket holds.</param>
public sealed record PlanResponse(Guid PlanId, int RequestsPerSecond, int Burst);
