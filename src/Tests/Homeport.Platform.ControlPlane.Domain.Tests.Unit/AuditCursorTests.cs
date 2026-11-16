// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain.Tests.Unit;

public sealed class AuditCursorTests
{
    public sealed class TryParse
    {
        [Fact]
        public void Reads_What_ToString_Wrote()
        {
            var cursor = new AuditCursor(new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero).AddTicks(1230), Guid.NewGuid());

            var read = AuditCursor.TryParse(cursor.ToString(), out var parsed);

            Assert.Equal((true, cursor), (read, parsed));
        }
    }

    public sealed class After
    {
        [Fact]
        public void Points_At_The_Moment_And_Id_Of_The_Entry()
        {
            var entry = new AuditEntry
            {
                Id = Guid.NewGuid(),
                OccurredAt = new DateTimeOffset(2026, 11, 16, 10, 0, 0, TimeSpan.Zero),
                OperatorSubject = "operator",
                OperatorName = "Operator",
                Action = AuditEntry.TenantCreated,
                Target = AuditEntry.TenantTarget(Guid.NewGuid()),
                Summary = "Created the tenant.",
            };

            Assert.Equal(new AuditCursor(entry.OccurredAt, entry.Id), AuditCursor.After(entry));
        }
    }
}
