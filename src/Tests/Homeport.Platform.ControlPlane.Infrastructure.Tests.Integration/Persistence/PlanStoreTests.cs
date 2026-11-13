// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Plans;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

public sealed class PlanStoreTests
{
    // Each test names its plans after a fresh id, so no two tests collide.
    [Collection(ControlPlaneDatabase.Collection)]
    public sealed class TrySaveAsync(ControlPlaneDatabase database)
    {
        [Fact]
        public async Task Stores_The_Plan_Together_With_Its_Audit_Entry()
        {
            var plan = NewPlan();
            var entry = EntryFor(plan.Id);

            var outcome = await AddAsync(plan, entry);

            Assert.Equal(PlanSaveOutcome.Saved, outcome);
            Assert.True(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Stores_No_Audit_Entry_When_Another_Plan_Took_The_Name_Meanwhile()
        {
            var first = NewPlan();
            await AddAsync(first, EntryFor(first.Id));
            var second = new Plan { Id = Guid.NewGuid(), Name = first.Name, RequestsPerSecond = 1, Burst = 1 };
            var entry = EntryFor(second.Id);

            var outcome = await AddAsync(second, entry);

            Assert.Equal(PlanSaveOutcome.NameTaken, outcome);
            Assert.False(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Stores_Two_Plans_Whose_Names_Differ_Only_In_Case()
        {
            var first = NewPlan();
            await AddAsync(first, EntryFor(first.Id));
            var second = new Plan { Id = Guid.NewGuid(), Name = first.Name.ToUpperInvariant(), RequestsPerSecond = 1, Burst = 1 };

            var outcome = await AddAsync(second, EntryFor(second.Id));

            Assert.Equal(PlanSaveOutcome.Saved, outcome);
        }

        [Fact]
        public async Task Answers_In_Use_And_Stores_No_Audit_Entry_When_A_Client_Uses_The_Plan()
        {
            var plan = NewPlan();
            await AddAsync(plan, EntryFor(plan.Id));
            await AddClientAsync(plan.Id);
            var entry = EntryFor(plan.Id);

            var outcome = await RemoveAsync(plan.Id, entry);

            Assert.Equal(PlanSaveOutcome.InUse, outcome);
            Assert.False(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Deletes_A_Plan_No_Client_Uses()
        {
            var plan = NewPlan();
            await AddAsync(plan, EntryFor(plan.Id));

            var outcome = await RemoveAsync(plan.Id, EntryFor(plan.Id));

            Assert.Equal(PlanSaveOutcome.Saved, outcome);
        }

        private static Plan NewPlan()
        {
            var id = Guid.NewGuid();
            return new Plan { Id = id, Name = $"plan {id}", RequestsPerSecond = 10, Burst = 20 };
        }

        private static AuditEntry EntryFor(Guid planId)
        {
            return new AuditEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
                OperatorSubject = "operator-subject",
                OperatorName = "Dev Operator",
                Action = AuditEntry.PlanCreated,
                Target = AuditEntry.PlanTarget(planId),
                Summary = "Created a plan.",
            };
        }

        private async Task<PlanSaveOutcome> AddAsync(Plan plan, AuditEntry entry)
        {
            await using var scope = database.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IPlanStore>();
            store.Add(plan);
            store.Add(entry);
            return await store.TrySaveAsync(CancellationToken.None);
        }

        // Found before the client is added, as the plan administration finds
        // it before it checks whether a client uses it.
        private async Task<PlanSaveOutcome> RemoveAsync(Guid planId, AuditEntry entry)
        {
            await using var scope = database.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IPlanStore>();
            var plan = await store.FindAsync(planId, CancellationToken.None);
            store.Remove(plan!);
            store.Add(entry);
            return await store.TrySaveAsync(CancellationToken.None);
        }

        private async Task AddClientAsync(Guid planId)
        {
            await using var scope = database.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"tenant {Guid.NewGuid()}" };
            context.Tenants.Add(tenant);
            context.Clients.Add(new Client { ClientId = $"client-{Guid.NewGuid()}", TenantId = tenant.Id, PlanId = planId });
            await context.SaveChangesAsync();
        }

        private async Task<bool> ExistsAsync(Guid entryId)
        {
            await using var scope = database.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>().AuditEntries.AnyAsync(e => e.Id == entryId);
        }
    }
}
