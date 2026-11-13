// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A plan as the operator sees it.</summary>
/// <param name="Id">The plan's id.</param>
/// <param name="Name">The plan's name, unique among plans.</param>
/// <param name="RequestsPerSecond">How many requests per second a client of the plan gets on average.</param>
/// <param name="Burst">How many requests a client of the plan may make at once.</param>
/// <param name="ClientCount">How many clients use the plan; a plan in use cannot be deleted.</param>
public sealed record PlanResponse(Guid Id, string Name, int RequestsPerSecond, int Burst, int ClientCount);
