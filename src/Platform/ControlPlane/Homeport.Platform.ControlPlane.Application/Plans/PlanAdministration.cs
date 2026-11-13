// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Application.Plans;

/// <summary>
/// The operator lists, creates, changes and deletes plans. Every change is
/// stored together with its audit entry (ADR-0020).
/// </summary>
public sealed class PlanAdministration(IPlanStore store, TimeProvider clock)
{
    /// <summary>The plan does not exist.</summary>
    public const string NotFoundCode = "plan.not-found";

    /// <summary>Every plan, ordered by name.</summary>
    public Task<IReadOnlyList<PlanOverview>> ListAsync(CancellationToken cancellationToken)
    {
        return store.ListAsync(cancellationToken);
    }

    /// <summary>The plan <paramref name="id"/>, or <see cref="NotFoundCode"/>.</summary>
    public async Task<Result<PlanOverview>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var plan = await store.FindOverviewAsync(id, cancellationToken);

        return plan is null ? NotFound(id) : Result<PlanOverview>.Success(plan);
    }

    /// <summary>Creates a plan on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<PlanOverview>> CreateAsync(string name, int requestsPerSecond, int burst, Operator by, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var created = Plan.Create(Guid.CreateVersion7(now), name, requestsPerSecond, burst, await store.NamesAsync(except: null, cancellationToken));
        if (!created.IsSuccess)
        {
            return Result<PlanOverview>.Failure(created.Error);
        }

        var plan = created.Value;
        store.Add(plan);
        store.Add(by.Audit(now, AuditEntry.PlanCreated, AuditEntry.PlanTarget(plan.Id), $"Created the plan {Describe(plan)}."));

        return await SaveAsync(plan, clientCount: 0, cancellationToken);
    }

    /// <summary>Changes the name and limits of the plan <paramref name="id"/> on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<PlanOverview>> ChangeAsync(Guid id, string name, int requestsPerSecond, int burst, Operator by, CancellationToken cancellationToken)
    {
        var overview = await store.FindOverviewAsync(id, cancellationToken);
        var plan = await store.FindAsync(id, cancellationToken);
        if (overview is null || plan is null)
        {
            return NotFound(id);
        }

        var before = Describe(plan);
        var changed = plan.Change(name, requestsPerSecond, burst, await store.NamesAsync(except: id, cancellationToken));
        if (!changed.IsSuccess)
        {
            return Result<PlanOverview>.Failure(changed.Error);
        }

        var after = Describe(plan);
        if (after == before)
        {
            return Result<PlanOverview>.Success(overview);
        }

        store.Add(by.Audit(clock.GetUtcNow(), AuditEntry.PlanChanged, AuditEntry.PlanTarget(id), $"Changed the plan {before} to {after}."));

        return await SaveAsync(plan, overview.ClientCount, cancellationToken);
    }

    /// <summary>Deletes the plan <paramref name="id"/> on behalf of <paramref name="by"/>, while no client uses it.</summary>
    public async Task<Result<PlanOverview>> DeleteAsync(Guid id, Operator by, CancellationToken cancellationToken)
    {
        var overview = await store.FindOverviewAsync(id, cancellationToken);
        var plan = await store.FindAsync(id, cancellationToken);
        if (overview is null || plan is null)
        {
            return NotFound(id);
        }

        var deletable = plan.CheckDeletable(overview.ClientCount);
        if (!deletable.IsSuccess)
        {
            return Result<PlanOverview>.Failure(deletable.Error);
        }

        store.Remove(plan);
        store.Add(by.Audit(clock.GetUtcNow(), AuditEntry.PlanDeleted, AuditEntry.PlanTarget(id), $"Deleted the plan {Describe(plan)}."));

        return await SaveAsync(plan, clientCount: 0, cancellationToken);
    }

    private async Task<Result<PlanOverview>> SaveAsync(Plan plan, int clientCount, CancellationToken cancellationToken)
    {
        // The name was free and the plan unused when they were checked;
        // another change may have taken the name or assigned the plan since,
        // which the store's unique name and foreign key catch.
        return await store.TrySaveAsync(cancellationToken) switch
        {
            PlanSaveOutcome.Saved => Result<PlanOverview>.Success(new PlanOverview(plan.Id, plan.Name, plan.RequestsPerSecond, plan.Burst, clientCount)),
            PlanSaveOutcome.NameTaken => Result<PlanOverview>.Failure(Plan.NameTaken(plan.Name)),
            PlanSaveOutcome.InUse => Result<PlanOverview>.Failure(Plan.InUse(plan.Name)),
            var outcome => throw new InvalidOperationException($"Storing a plan ended in the unknown outcome {outcome}."),
        };
    }

    private static string Describe(Plan plan)
    {
        return $"'{plan.Name}' with {plan.RequestsPerSecond} requests per second and a burst of {plan.Burst}";
    }

    private static Result<PlanOverview> NotFound(Guid id)
    {
        return Result<PlanOverview>.Failure(new Error(NotFoundCode, $"There is no plan {id}."));
    }
}
