// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>
/// The plans of the admin API (ADR-0020): <c>/platform/v1/admin/plans</c>
/// on the gateway's admin endpoint, for the operator only.
/// </summary>
public static class PlansApi
{
    /// <summary>
    /// The path below <c>/platform</c>, the path the control plane serves it at.
    /// <c>GET</c> returns <see cref="PlansResponse"/>; <c>POST</c> takes
    /// <see cref="CreatePlanRequest"/> and returns the new <see cref="PlanResponse"/>.
    /// </summary>
    public const string Path = "/v1/admin/plans";

    /// <summary>The route value that names the plan in <see cref="PlanPath"/>.</summary>
    public const string PlanIdRouteValue = "planId";

    /// <summary>
    /// One plan. <c>GET</c> returns its <see cref="PlanResponse"/>; <c>PUT</c>
    /// takes <see cref="ChangePlanRequest"/> and returns it changed;
    /// <c>DELETE</c> deletes it while no client uses it.
    /// </summary>
    public const string PlanPath = Path + "/{" + PlanIdRouteValue + "}";

    /// <summary>The field path of the plan's name in a validation problem.</summary>
    public const string NameField = "name";

    /// <summary>The field path of the plan's rate in a validation problem.</summary>
    public const string RequestsPerSecondField = "requestsPerSecond";

    /// <summary>The field path of the plan's burst in a validation problem.</summary>
    public const string BurstField = "burst";
}
