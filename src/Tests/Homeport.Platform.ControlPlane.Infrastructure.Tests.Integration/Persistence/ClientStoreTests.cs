// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Clients;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

public sealed class ClientStoreTests
{
    // Each test names its tenants, plans and clients after a fresh id, so no two tests collide.
    [Collection(ControlPlaneDatabase.Collection)]
    public sealed class ListAsync(ControlPlaneDatabase database)
    {
        [Fact]
        public async Task Returns_Only_The_Clients_Of_The_Tenant_Ordered_By_Client_Id()
        {
            var (tenant, plan) = await AddTenantAndPlanAsync(database);
            var (otherTenant, _) = await AddTenantAndPlanAsync(database);
            await AddClientAsync(database, "b-client", tenant.Id, plan.Id);
            await AddClientAsync(database, "a-client", tenant.Id, plan.Id);
            await AddClientAsync(database, "c-client", otherTenant.Id, plan.Id);

            await using var scope = database.CreateScope();
            var clients = await scope.ServiceProvider.GetRequiredService<IClientStore>().ListAsync(tenant.Id, CancellationToken.None);

            Assert.Equal(
                [$"a-client {tenant.Id}", $"b-client {tenant.Id}"],
                clients.Select(c => c.ClientId));
            Assert.All(clients, c => Assert.Equal((tenant.Name, plan.Name), (c.TenantName, c.PlanName)));
        }
    }

    [Collection(ControlPlaneDatabase.Collection)]
    public sealed class TrySaveAsync(ControlPlaneDatabase database)
    {
        [Fact]
        public async Task Stores_The_Client_Together_With_Its_Audit_Entry()
        {
            var (tenant, plan) = await AddTenantAndPlanAsync(database);
            var client = new Client { ClientId = $"client-{Guid.NewGuid()}", TenantId = tenant.Id, PlanId = plan.Id };
            var entry = EntryFor(client.ClientId);

            var outcome = await AddAsync(client, entry);

            Assert.Equal(ClientSaveOutcome.Saved, outcome);
            Assert.True(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Answers_Client_Id_Taken_And_Stores_No_Audit_Entry_When_The_Id_Was_Registered_Meanwhile()
        {
            var (tenant, plan) = await AddTenantAndPlanAsync(database);
            var clientId = $"client-{Guid.NewGuid()}";
            await AddAsync(new Client { ClientId = clientId, TenantId = tenant.Id, PlanId = plan.Id }, EntryFor(clientId));
            var entry = EntryFor(clientId);

            var outcome = await AddAsync(new Client { ClientId = clientId, TenantId = tenant.Id, PlanId = plan.Id }, entry);

            Assert.Equal(ClientSaveOutcome.ClientIdTaken, outcome);
            Assert.False(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Answers_Plan_Unknown_And_Stores_No_Audit_Entry_When_The_Plan_Was_Deleted_Meanwhile()
        {
            var (tenant, _) = await AddTenantAndPlanAsync(database);
            var clientId = $"client-{Guid.NewGuid()}";
            var entry = EntryFor(clientId);

            var outcome = await AddAsync(new Client { ClientId = clientId, TenantId = tenant.Id, PlanId = Guid.NewGuid() }, entry);

            Assert.Equal(ClientSaveOutcome.PlanUnknown, outcome);
            Assert.False(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Stores_An_Audit_Entry_Whose_Target_Names_The_Longest_Client_Id()
        {
            var (tenant, plan) = await AddTenantAndPlanAsync(database);
            var clientId = Guid.NewGuid() + new string('a', Client.MaxClientIdLength - 36);
            var entry = EntryFor(clientId);

            var outcome = await AddAsync(new Client { ClientId = clientId, TenantId = tenant.Id, PlanId = plan.Id }, entry);

            Assert.Equal(ClientSaveOutcome.Saved, outcome);
        }

        private static AuditEntry EntryFor(string clientId)
        {
            return new AuditEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.Parse("2026-11-14T12:00:00Z"),
                OperatorSubject = "operator-subject",
                OperatorName = "Dev Operator",
                Action = AuditEntry.ClientRegistered,
                Target = AuditEntry.ClientTarget(clientId),
                Summary = "Registered a client.",
            };
        }

        private async Task<ClientSaveOutcome> AddAsync(Client client, AuditEntry entry)
        {
            await using var scope = database.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IClientStore>();
            store.Add(client);
            store.Add(entry);
            return await store.TrySaveAsync(CancellationToken.None);
        }

        private async Task<bool> ExistsAsync(Guid entryId)
        {
            await using var scope = database.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>().AuditEntries.AnyAsync(e => e.Id == entryId);
        }
    }

    private static async Task<(Tenant Tenant, Plan Plan)> AddTenantAndPlanAsync(ControlPlaneDatabase database)
    {
        await using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = $"tenant {Guid.NewGuid()}" };
        var plan = new Plan { Id = Guid.NewGuid(), Name = $"plan {Guid.NewGuid()}", RequestsPerSecond = 10, Burst = 20 };
        context.Tenants.Add(tenant);
        context.Plans.Add(plan);
        await context.SaveChangesAsync();
        return (tenant, plan);
    }

    // The client id carries the tenant's id, so it is unique per test.
    private static async Task AddClientAsync(ControlPlaneDatabase database, string name, Guid tenantId, Guid planId)
    {
        await using var scope = database.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
        context.Clients.Add(new Client { ClientId = $"{name} {tenantId}", TenantId = tenantId, PlanId = planId });
        await context.SaveChangesAsync();
    }
}
