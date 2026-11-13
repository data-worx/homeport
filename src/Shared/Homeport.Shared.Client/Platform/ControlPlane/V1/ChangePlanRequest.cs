// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A plan's new name and limits.</summary>
/// <param name="Name">The name: required, unique among plans, compared case-sensitively.</param>
/// <param name="RequestsPerSecond">How fast a client's token bucket refills: positive.</param>
/// <param name="Burst">How many requests a client's token bucket holds: positive, and may be below the rate.</param>
public sealed record ChangePlanRequest(string Name, int RequestsPerSecond, int Burst);
