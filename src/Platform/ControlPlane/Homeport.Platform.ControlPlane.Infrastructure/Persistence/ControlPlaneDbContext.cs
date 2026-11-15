// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>
/// The tenants, plans and clients in the schema <see cref="Schema"/>, and the
/// audit entries of their changes. They are platform tables the operator
/// owns, not tenant-owned data, so they carry no row-level security.
/// </summary>
public sealed class ControlPlaneDbContext(DbContextOptions<ControlPlaneDbContext> options) : DbContext(options)
{
    /// <summary>The schema the control plane owns; the migration history lives there too.</summary>
    public const string Schema = "control_plane";

    /// <summary>The table of the audit entries, named as ADR-0020 names it.</summary>
    public const string AuditEntriesTable = "audit_entries";

    /// <summary>The tenants.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>The plans.</summary>
    public DbSet<Plan> Plans => Set<Plan>();

    /// <summary>The clients.</summary>
    public DbSet<Client> Clients => Set<Client>();

    /// <summary>The audit entries of every admin change.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>
    /// Applies the control plane's schema and every configuration of this assembly.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ControlPlaneDbContext).Assembly);
    }
}
