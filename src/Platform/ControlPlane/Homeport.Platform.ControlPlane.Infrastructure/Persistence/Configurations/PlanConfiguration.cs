// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Plan"/> onto the table <c>Plans</c>. The name is unique
/// as the database's deterministic collation compares, which is
/// case-sensitive: it catches two changes that each found the name free.
/// </summary>
internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> plan)
    {
        plan.Property(p => p.Id).ValueGeneratedNever();
        plan.Property(p => p.Name).HasMaxLength(Plan.MaxNameLength);
        plan.HasIndex(p => p.Name).IsUnique();
    }
}
