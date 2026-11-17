// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Infrastructure.Persistence;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// Upgrading from the last release keeps every row it wrote: the migrations
/// since then neither drop nor change a column the release knew.
/// </summary>
public sealed class UpgradeTests
{
    private const string Schema = ControlPlaneDbContext.Schema;

    public sealed class FromV0_1_0(UpgradedDatabase database) : IClassFixture<UpgradedDatabase>
    {
        [Fact]
        public async Task Keeps_The_Plan()
        {
            var row = await database.ReadRowAsync(
                $"""SELECT "Name", "RequestsPerSecond", "Burst" FROM {Schema}."Plans" WHERE "Id" = $1""",
                UpgradedDatabase.PlanId);

            Assert.Equal([UpgradedDatabase.PlanName, UpgradedDatabase.RequestsPerSecond, UpgradedDatabase.Burst], row);
        }

        [Fact]
        public async Task Keeps_The_Tenant()
        {
            var row = await database.ReadRowAsync(
                $"""SELECT "Name" FROM {Schema}."Tenants" WHERE "Id" = $1""",
                UpgradedDatabase.TenantId);

            Assert.Equal([UpgradedDatabase.TenantName], row);
        }

        [Fact]
        public async Task Keeps_The_Client()
        {
            var row = await database.ReadRowAsync(
                $"""SELECT "TenantId", "PlanId" FROM {Schema}."Clients" WHERE "ClientId" = $1""",
                UpgradedDatabase.ClientId);

            Assert.Equal([UpgradedDatabase.TenantId, UpgradedDatabase.PlanId], row);
        }
    }
}
