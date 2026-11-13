// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="AuditEntry"/> onto the table
/// <see cref="ControlPlaneDbContext.AuditEntriesTable"/>, which the app role
/// may only add to and read (<see cref="AuditPrivileges"/>).
/// </summary>
internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> entry)
    {
        entry.ToTable(ControlPlaneDbContext.AuditEntriesTable);
        entry.Property(e => e.Id).ValueGeneratedNever();
        entry.Property(e => e.OperatorSubject).HasMaxLength(AuditEntry.MaxFieldLength);
        entry.Property(e => e.OperatorName).HasMaxLength(AuditEntry.MaxFieldLength);
        entry.Property(e => e.Action).HasMaxLength(AuditEntry.MaxFieldLength);
        entry.Property(e => e.Target).HasMaxLength(AuditEntry.MaxFieldLength);
        entry.Property(e => e.Summary).HasMaxLength(AuditEntry.MaxSummaryLength);
        entry.HasIndex(e => e.OccurredAt);
    }
}
