// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.AdminApi;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Portal.Tests.Unit;

/// <summary>The admin API, answering with what a test sets.</summary>
internal sealed class StandInAdminApi : IAdminApi
{
    /// <summary>A problem of the gateway that is no validation problem.</summary>
    internal static readonly ProblemResponse Unavailable = new(
        new Uri(ProblemConventions.TypePrefix + "platform-service-unavailable"),
        "The platform service is unavailable.",
        503,
        "The control plane did not answer.",
        Instance: null,
        "4bf92f3577b34da6a3ce929d0e0e4736",
        Errors: null);

    /// <summary>The answer to <see cref="GetTenantsAsync"/>.</summary>
    internal AdminResult<TenantsResponse> Tenants { get; set; } = AdminResult<TenantsResponse>.Success(new TenantsResponse([]));

    /// <summary>The answer to a create or a rename.</summary>
    internal AdminResult<TenantResponse>? Change { get; set; }

    public Task<AdminResult<TenantsResponse>> GetTenantsAsync(CancellationToken cancellationToken) => Task.FromResult(Tenants);

    public Task<AdminResult<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Change ?? AdminResult<TenantResponse>.Success(new TenantResponse(Guid.NewGuid(), request.Name, 0)));

    public Task<AdminResult<TenantResponse>> RenameTenantAsync(Guid tenantId, RenameTenantRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(Change ?? AdminResult<TenantResponse>.Success(new TenantResponse(tenantId, request.Name, 0)));

    /// <summary>The answer to <see cref="GetPlansAsync"/>.</summary>
    internal AdminResult<PlansResponse> Plans { get; set; } = AdminResult<PlansResponse>.Success(new PlansResponse([]));

    /// <summary>The answer to a create or a change of a plan.</summary>
    internal AdminResult<PlanResponse>? PlanChange { get; set; }

    /// <summary>The answer to <see cref="DeletePlanAsync"/>.</summary>
    internal AdminResult<Guid>? PlanDeletion { get; set; }

    public Task<AdminResult<PlansResponse>> GetPlansAsync(CancellationToken cancellationToken) => Task.FromResult(Plans);

    public Task<AdminResult<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(PlanChange ?? AdminResult<PlanResponse>.Success(new PlanResponse(Guid.NewGuid(), request.Name, request.RequestsPerSecond, request.Burst, 0)));

    public Task<AdminResult<PlanResponse>> ChangePlanAsync(Guid planId, ChangePlanRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(PlanChange ?? AdminResult<PlanResponse>.Success(new PlanResponse(planId, request.Name, request.RequestsPerSecond, request.Burst, 0)));

    public Task<AdminResult<Guid>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken) =>
        Task.FromResult(PlanDeletion ?? AdminResult<Guid>.Success(planId));

    /// <summary>A validation problem with <paramref name="message"/> at <paramref name="field"/>.</summary>
    internal static ProblemResponse ValidationOf(string field, string message)
    {
        return new ProblemResponse(
            new Uri(ProblemConventions.TypePrefix + "validation"),
            "One or more validation errors occurred.",
            400,
            Detail: null,
            Instance: null,
            "0af7651916cd43dd8448eb211c80319c",
            new Dictionary<string, string[]> { [field] = [message] });
    }
}
