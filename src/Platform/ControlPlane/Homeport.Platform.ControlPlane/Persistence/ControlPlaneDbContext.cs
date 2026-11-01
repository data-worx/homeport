// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.ControlPlane.Persistence;

/// <summary>
/// The tenants, plans and clients in the schema <see cref="Schema"/>. They are
/// platform tables the operator owns, not tenant-owned data, so they carry no
/// row-level security.
/// </summary>
public sealed class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    /// <summary>The schema the control plane owns; the migration history lives there too.</summary>
    public const string Schema = "control_plane";

    /// <summary>The longest name of a tenant or plan.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The longest client id; the identity providers Homeport targets stay well below.</summary>
    public const int MaxClientIdLength = 255;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.Property(t => t.Id).ValueGeneratedNever();
            tenant.Property(t => t.Name).HasMaxLength(MaxNameLength);
        });

        modelBuilder.Entity<Plan>(plan =>
        {
            plan.Property(p => p.Id).ValueGeneratedNever();
            plan.Property(p => p.Name).HasMaxLength(MaxNameLength);
        });

        modelBuilder.Entity<Client>(client =>
        {
            client.HasKey(c => c.ClientId);
            client.Property(c => c.ClientId).HasMaxLength(MaxClientIdLength);
            client.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
            client.HasOne(c => c.Plan).WithMany().HasForeignKey(c => c.PlanId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
