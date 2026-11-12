// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>
/// The tenants, plans and clients in the schema <see cref="Schema"/>. They are
/// platform tables the operator owns, not tenant-owned data, so they carry no
/// row-level security.
/// </summary>
public sealed class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    /// <summary>The schema the control plane owns; the migration history lives there too.</summary>
    public const string Schema = "control_plane";

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlPlaneDbContext).Assembly);
    }
}
