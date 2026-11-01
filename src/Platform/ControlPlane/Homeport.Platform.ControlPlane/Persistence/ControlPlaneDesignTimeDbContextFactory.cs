// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Homeport.Platform.ControlPlane.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without starting the
/// service. Adding a migration never connects, so the connection string is a
/// placeholder.
/// </summary>
internal sealed class ControlPlaneDesignTimeDbContextFactory : IDesignTimeDbContextFactory<ControlPlaneDbContext>
{
    private const string PlaceholderConnectionString = "Host=localhost";

    public ControlPlaneDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ControlPlaneDbContext>();
        ControlPlanePersistenceExtensions.ConfigureNpgsql(options.UseNpgsql(PlaceholderConnectionString));
        return new ControlPlaneDbContext(options.Options);
    }
}
