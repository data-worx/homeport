// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Plans;

/// <summary>A plan with what the operator sees about it at a glance.</summary>
/// <param name="Id">The plan's id.</param>
/// <param name="Name">The plan's name.</param>
/// <param name="RequestsPerSecond">How fast a client's bucket refills.</param>
/// <param name="Burst">How many requests a client's bucket holds.</param>
/// <param name="ClientCount">How many clients use the plan.</param>
public sealed record PlanOverview(Guid Id, string Name, int RequestsPerSecond, int Burst, int ClientCount);
