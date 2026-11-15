// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the
/// service. Adding a migration never connects, so the connection string is a
/// placeholder.
/// </summary>
internal sealed class MeteringDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MeteringDbContext>
{
    private const string PlaceholderConnectionString = "Host=localhost";

    /// <summary>
    /// Creates the context for the EF tools, with a placeholder connection string.
    /// </summary>
    public MeteringDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MeteringDbContext>();
        MeteringPersistenceExtensions.ConfigureNpgsql(options.UseNpgsql(PlaceholderConnectionString));
        return new MeteringDbContext(options.Options, new CurrentTenant());
    }
}
