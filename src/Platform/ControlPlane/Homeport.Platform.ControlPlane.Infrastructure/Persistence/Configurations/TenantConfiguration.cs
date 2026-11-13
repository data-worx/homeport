// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Tenant"/> onto the table <c>Tenants</c>. The name is
/// unique as the database's deterministic collation compares, which is
/// case-sensitive: it catches two changes that each found the name free.
/// </summary>
internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> tenant)
    {
        tenant.Property(t => t.Id).ValueGeneratedNever();
        tenant.Property(t => t.Name).HasMaxLength(Tenant.MaxNameLength);
        tenant.HasIndex(t => t.Name).IsUnique();
    }
}
