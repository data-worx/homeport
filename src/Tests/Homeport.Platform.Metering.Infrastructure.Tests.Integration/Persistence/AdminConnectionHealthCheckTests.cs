// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

public sealed class AdminConnectionHealthCheckTests
{
    [Collection(MeteringDatabase.Collection)]
    public sealed class CheckHealthAsync(MeteringDatabase database)
    {
        [Fact]
        public async Task Returns_Healthy_When_The_Admin_Role_Signs_In()
        {
            var result = await CheckAsync(database.AdminConnectionString);

            Assert.Equal(HealthStatus.Healthy, result.Status);
        }

        [Fact]
        public async Task Returns_The_Refused_Password_As_Cause_When_The_Admin_Role_Cannot_Sign_In()
        {
            var wrongPassword = new NpgsqlConnectionStringBuilder(database.AdminConnectionString) { Password = "wrong-password" }.ConnectionString;

            var result = await CheckAsync(wrongPassword);

            Assert.Equal(
                (HealthStatus.Unhealthy, PostgresErrorCodes.InvalidPassword),
                (result.Status, Assert.IsType<PostgresException>(result.Exception).SqlState));
        }

        private static async Task<HealthCheckResult> CheckAsync(string connectionString)
        {
            var options = (DbContextOptions<MeteringDbContext>)MeteringPersistenceExtensions
                .ConfigureNpgsql(new DbContextOptionsBuilder<MeteringDbContext>().UseNpgsql(connectionString)).Options;
            await using var context = new MeteringDbContext(options, new CurrentTenant());
            var check = new AdminConnectionHealthCheck(context);
            var registration = new HealthCheckRegistration(AdminConnectionHealthCheck.Name, check, HealthStatus.Unhealthy, null);

            return await check.CheckHealthAsync(new HealthCheckContext { Registration = registration });
        }
    }
}
