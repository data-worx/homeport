// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Tenants;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

public sealed class TenantStoreTests
{
    // Each test names its tenants after a fresh id, so no two tests collide.
    [Collection(ControlPlaneDatabase.Collection)]
    public sealed class TrySaveAsync(ControlPlaneDatabase database)
    {
        [Fact]
        public async Task Stores_The_Tenant_Together_With_Its_Audit_Entry()
        {
            var tenant = NewTenant();
            var entry = EntryFor(tenant.Id);

            var saved = await SaveAsync(tenant, entry);

            Assert.True(saved);
            Assert.True(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Stores_No_Audit_Entry_When_Another_Tenant_Took_The_Name_Meanwhile()
        {
            var first = NewTenant();
            await SaveAsync(first, EntryFor(first.Id));
            var second = new Tenant { Id = Guid.NewGuid(), Name = first.Name };
            var entry = EntryFor(second.Id);

            var saved = await SaveAsync(second, entry);

            Assert.False(saved);
            Assert.False(await ExistsAsync(entry.Id));
        }

        [Fact]
        public async Task Stores_Two_Tenants_Whose_Names_Differ_Only_In_Case()
        {
            var first = NewTenant();
            await SaveAsync(first, EntryFor(first.Id));
            var second = new Tenant { Id = Guid.NewGuid(), Name = first.Name.ToUpperInvariant() };

            var saved = await SaveAsync(second, EntryFor(second.Id));

            Assert.True(saved);
        }

        private static Tenant NewTenant()
        {
            var id = Guid.NewGuid();
            return new Tenant { Id = id, Name = $"tenant {id}" };
        }

        private static AuditEntry EntryFor(Guid tenantId)
        {
            return new AuditEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.Parse("2026-11-13T12:00:00Z"),
                OperatorSubject = "operator-subject",
                OperatorName = "Dev Operator",
                Action = AuditEntry.TenantCreated,
                Target = AuditEntry.TenantTarget(tenantId),
                Summary = "Created a tenant.",
            };
        }

        private async Task<bool> SaveAsync(Tenant tenant, AuditEntry entry)
        {
            await using var scope = database.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<ITenantStore>();
            store.Add(tenant);
            store.Add(entry);
            return await store.TrySaveAsync(CancellationToken.None);
        }

        private async Task<bool> ExistsAsync(Guid entryId)
        {
            await using var scope = database.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>().AuditEntries.AnyAsync(e => e.Id == entryId);
        }
    }
}
