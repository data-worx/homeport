// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>Every plan of the installation.</summary>
/// <param name="Plans">The plans, ordered by name.</param>
public sealed record PlansResponse(IReadOnlyList<PlanResponse> Plans);
