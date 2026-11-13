// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Platform.ControlPlane.Infrastructure.Seeding;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration;

public sealed class MigrateCommandTests
{
    // The fixture has run migrate once already.
    public sealed class RunAsync(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Theory]
        [InlineData("Tenants")]
        [InlineData("Plans")]
        [InlineData("Clients")]
        public async Task Leaves_Exactly_One_Seeded_Row_When_Run_Twice(string table)
        {
            await controlPlane.MigrateAsync();

            var count = await controlPlane.ExecuteScalarAsAppRoleAsync($"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{table}\"");

            Assert.Equal(1L, count);
        }

        [Fact]
        public async Task Succeeds_When_Run_Twice()
        {
            var exitCode = await controlPlane.MigrateAsync();

            Assert.Equal(0, exitCode);
        }

        [Fact]
        public async Task Fails_Without_A_Seed_Client()
        {
            var exitCode = await controlPlane.MigrateAsync($"--{SeedOptions.SectionName}:{nameof(SeedOptions.ClientId)}=");

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task Fails_With_A_Seed_Burst_Of_Zero()
        {
            var exitCode = await controlPlane.MigrateAsync($"--{SeedOptions.SectionName}:{nameof(SeedOptions.Burst)}=0");

            Assert.Equal(1, exitCode);
        }

        [Fact]
        public async Task Succeeds_With_A_Seed_Burst_Below_The_Rate()
        {
            var exitCode = await controlPlane.MigrateAsync(
                $"--{SeedOptions.SectionName}:{nameof(SeedOptions.RequestsPerSecond)}=20",
                $"--{SeedOptions.SectionName}:{nameof(SeedOptions.Burst)}=5");

            Assert.Equal(0, exitCode);
        }
    }
}
