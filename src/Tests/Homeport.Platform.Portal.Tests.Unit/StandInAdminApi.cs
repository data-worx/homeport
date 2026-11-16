// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Portal.AdminApi;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Platform.Registry.V1;
using Homeport.Shared.Client.Problems;
using ControlPlaneAudit = Homeport.Shared.Client.Platform.ControlPlane.V1.AuditEntriesResponse;
using MeteringAudit = Homeport.Shared.Client.Platform.Metering.V1.AuditEntriesResponse;

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

    /// <summary>The answer to <see cref="GetClientsAsync"/> by the tenant asked for; every client under <see cref="Guid.Empty"/>.</summary>
    internal Dictionary<Guid, AdminResult<ClientsResponse>> Clients { get; } = new()
    {
        [Guid.Empty] = AdminResult<ClientsResponse>.Success(new ClientsResponse([])),
    };

    /// <summary>The answer to a registration or a change of a client's plan.</summary>
    internal AdminResult<ClientResponse>? ClientChange { get; set; }

    /// <summary>The client ids <see cref="RemoveClientAsync"/> was called for.</summary>
    internal List<string> RemovedClients { get; } = [];

    public Task<AdminResult<ClientsResponse>> GetClientsAsync(Guid? tenantId, CancellationToken cancellationToken) =>
        Task.FromResult(Clients[tenantId ?? Guid.Empty]);

    public Task<AdminResult<ClientResponse>> RegisterClientAsync(RegisterClientRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(ClientChange ?? AdminResult<ClientResponse>.Success(new ClientResponse(request.ClientId, request.TenantId, "Tenant", request.PlanId, "Plan")));

    public Task<AdminResult<ClientResponse>> ChangeClientPlanAsync(string clientId, ChangeClientPlanRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(ClientChange ?? AdminResult<ClientResponse>.Success(new ClientResponse(clientId, Guid.Empty, "Tenant", request.PlanId, "Plan")));

    public Task<AdminResult<string>> RemoveClientAsync(string clientId, CancellationToken cancellationToken)
    {
        RemovedClients.Add(clientId);
        return Task.FromResult(AdminResult<string>.Success(clientId));
    }

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

    /// <summary>The answer to <see cref="GetModulesAsync"/>.</summary>
    internal AdminResult<ModulesResponse> Modules { get; set; } = AdminResult<ModulesResponse>.Success(new ModulesResponse([]));

    public Task<AdminResult<ModulesResponse>> GetModulesAsync(CancellationToken cancellationToken) => Task.FromResult(Modules);

    /// <summary>The answer to <see cref="GetUsageAsync"/>.</summary>
    internal AdminResult<AdminUsageResponse> Usage { get; set; } =
        AdminResult<AdminUsageResponse>.Success(new AdminUsageResponse(DateTime.UnixEpoch, DateTime.UnixEpoch, UsageApi.Day, []));

    /// <summary>The answer to <see cref="GetUsageAsync"/> by its filter, where a test sets one; <see cref="Usage"/> otherwise.</summary>
    internal Func<UsageFilter, Task<AdminResult<AdminUsageResponse>>>? UsageFor { get; set; }

    /// <summary>The filters <see cref="GetUsageAsync"/> was called with, in order.</summary>
    internal List<UsageFilter> UsageReads { get; } = [];

    /// <summary>The answer to <see cref="ExportUsageAsync"/>.</summary>
    internal AdminResult<byte[]> Export { get; set; } = AdminResult<byte[]>.Success("period,tenant,module,meter,quantity\r\n"u8.ToArray());

    /// <summary>The filters and media types <see cref="ExportUsageAsync"/> was called with, in order.</summary>
    internal List<(UsageFilter Filter, string MediaType)> Exports { get; } = [];

    public Task<AdminResult<AdminUsageResponse>> GetUsageAsync(UsageFilter filter, CancellationToken cancellationToken)
    {
        UsageReads.Add(filter);
        return UsageFor?.Invoke(filter) ?? Task.FromResult(Usage);
    }

    public Task<AdminResult<byte[]>> ExportUsageAsync(UsageFilter filter, string mediaType, CancellationToken cancellationToken)
    {
        Exports.Add((filter, mediaType));
        return Task.FromResult(Export);
    }

    /// <summary>The answer to <see cref="GetControlPlaneAuditAsync"/> by the cursor asked for.</summary>
    internal Func<string?, AdminResult<ControlPlaneAudit>> ControlPlaneAudit { get; set; } =
        _ => AdminResult<ControlPlaneAudit>.Success(new ControlPlaneAudit([], Next: null));

    /// <summary>The filters and cursors <see cref="GetControlPlaneAuditAsync"/> was called with, in order.</summary>
    internal List<(AuditFilter Filter, string? Before)> ControlPlaneAuditReads { get; } = [];

    /// <summary>The answer to <see cref="GetMeteringAuditAsync"/> by the cursor asked for.</summary>
    internal Func<string?, AdminResult<MeteringAudit>> MeteringAudit { get; set; } =
        _ => AdminResult<MeteringAudit>.Success(new MeteringAudit([], Next: null));

    /// <summary>The filters and cursors <see cref="GetMeteringAuditAsync"/> was called with, in order.</summary>
    internal List<(AuditFilter Filter, string? Before)> MeteringAuditReads { get; } = [];

    public Task<AdminResult<ControlPlaneAudit>> GetControlPlaneAuditAsync(AuditFilter filter, string? before, CancellationToken cancellationToken)
    {
        ControlPlaneAuditReads.Add((filter, before));
        return Task.FromResult(ControlPlaneAudit(before));
    }

    public Task<AdminResult<MeteringAudit>> GetMeteringAuditAsync(AuditFilter filter, string? before, CancellationToken cancellationToken)
    {
        MeteringAuditReads.Add((filter, before));
        return Task.FromResult(MeteringAudit(before));
    }
}
