// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>The plan a client is held to from now on.</summary>
/// <param name="PlanId">An existing plan.</param>
public sealed record ChangeClientPlanRequest(Guid PlanId);
