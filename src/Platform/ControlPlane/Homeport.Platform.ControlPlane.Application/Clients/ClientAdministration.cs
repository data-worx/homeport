// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Application.Clients;

/// <summary>
/// The operator registers the clients of a tenant, moves them to another plan
/// and removes them. A client is created at the identity provider; Homeport
/// only maps its client id (ADR-0020). Every change is stored together with
/// its audit entry.
/// </summary>
public sealed class ClientAdministration(IClientStore store, TimeProvider clock)
{
    /// <summary>The client is not registered.</summary>
    public const string NotFoundCode = "client.not-found";

    /// <summary>Every client, or every client of <paramref name="tenantId"/>, ordered by client id.</summary>
    public Task<IReadOnlyList<ClientOverview>> ListAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        return store.ListAsync(tenantId, cancellationToken);
    }

    /// <summary>The client <paramref name="clientId"/>, or <see cref="NotFoundCode"/>.</summary>
    public async Task<Result<ClientOverview>> GetAsync(string clientId, CancellationToken cancellationToken)
    {
        var client = await store.FindOverviewAsync(clientId, cancellationToken);

        return client is null ? NotFound(clientId) : Result<ClientOverview>.Success(client);
    }

    /// <summary>Registers the client <paramref name="clientId"/> for a tenant and a plan on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<ClientOverview>> RegisterAsync(string clientId, Guid tenantId, Guid planId, Operator by, CancellationToken cancellationToken)
    {
        var tenant = await store.FindTenantAsync(tenantId, cancellationToken);
        var plan = await store.FindPlanAsync(planId, cancellationToken);
        var registered = !string.IsNullOrWhiteSpace(clientId) && await store.FindOverviewAsync(clientId, cancellationToken) is not null;

        var created = Client.Register(clientId, tenant, plan, registered);
        if (!created.IsSuccess)
        {
            return Result<ClientOverview>.Failure(created.Error);
        }

        var client = created.Value;
        store.Add(client);
        store.Add(by.Audit(
            clock.GetUtcNow(),
            AuditEntry.ClientRegistered,
            AuditEntry.ClientTarget(client.ClientId),
            $"Registered the client '{client.ClientId}' for the tenant '{tenant!.Name}' on the plan '{plan!.Name}'."));

        return await SaveAsync(new ClientOverview(client.ClientId, tenant.Id, tenant.Name, plan.Id, plan.Name), cancellationToken);
    }

    /// <summary>Holds the client <paramref name="clientId"/> to the plan <paramref name="planId"/> on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<ClientOverview>> ChangePlanAsync(string clientId, Guid planId, Operator by, CancellationToken cancellationToken)
    {
        var overview = await store.FindOverviewAsync(clientId, cancellationToken);
        var client = await store.FindAsync(clientId, cancellationToken);
        if (overview is null || client is null)
        {
            return NotFound(clientId);
        }

        var plan = await store.FindPlanAsync(planId, cancellationToken);
        var changed = client.ChangePlan(plan);
        if (!changed.IsSuccess)
        {
            return Result<ClientOverview>.Failure(changed.Error);
        }

        if (overview.PlanId == planId)
        {
            return Result<ClientOverview>.Success(overview);
        }

        store.Add(by.Audit(
            clock.GetUtcNow(),
            AuditEntry.ClientPlanChanged,
            AuditEntry.ClientTarget(clientId),
            $"Moved the client '{clientId}' from the plan '{overview.PlanName}' to the plan '{plan!.Name}'."));

        return await SaveAsync(overview with { PlanId = plan.Id, PlanName = plan.Name }, cancellationToken);
    }

    /// <summary>
    /// Removes the client <paramref name="clientId"/> on behalf of <paramref name="by"/>.
    /// Its usage stays with the tenant it was recorded for.
    /// </summary>
    public async Task<Result<ClientOverview>> RemoveAsync(string clientId, Operator by, CancellationToken cancellationToken)
    {
        var overview = await store.FindOverviewAsync(clientId, cancellationToken);
        var client = await store.FindAsync(clientId, cancellationToken);
        if (overview is null || client is null)
        {
            return NotFound(clientId);
        }

        store.Remove(client);
        store.Add(by.Audit(
            clock.GetUtcNow(),
            AuditEntry.ClientRemoved,
            AuditEntry.ClientTarget(clientId),
            $"Removed the client '{clientId}' of the tenant '{overview.TenantName}'."));

        return await SaveAsync(overview, cancellationToken);
    }

    private async Task<Result<ClientOverview>> SaveAsync(ClientOverview client, CancellationToken cancellationToken)
    {
        // The client id was free and the plan there when they were checked;
        // another change may have registered the id or deleted the plan since,
        // which the store's primary key and foreign key catch.
        return await store.TrySaveAsync(cancellationToken) switch
        {
            ClientSaveOutcome.Saved => Result<ClientOverview>.Success(client),
            ClientSaveOutcome.ClientIdTaken => Result<ClientOverview>.Failure(Client.ClientIdTaken(client.ClientId)),
            ClientSaveOutcome.PlanUnknown => Result<ClientOverview>.Failure(Client.PlanUnknown()),
            var outcome => throw new InvalidOperationException($"Storing a client ended in the unknown outcome {outcome}."),
        };
    }

    private static Result<ClientOverview> NotFound(string clientId)
    {
        return Result<ClientOverview>.Failure(new Error(NotFoundCode, $"The client '{clientId}' is not registered."));
    }
}
