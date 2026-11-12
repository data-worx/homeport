// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Client"/> onto the table <c>Clients</c>. Neither its tenant
/// nor its plan can be deleted while it exists.
/// </summary>
internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> client)
    {
        client.HasKey(c => c.ClientId);
        client.Property(c => c.ClientId).HasMaxLength(Client.MaxClientIdLength);
        client.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        client.HasOne(c => c.Plan).WithMany().HasForeignKey(c => c.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
