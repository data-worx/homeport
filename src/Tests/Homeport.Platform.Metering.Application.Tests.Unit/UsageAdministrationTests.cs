// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Platform.Metering.Application.Tests.Unit;

public sealed class UsageAdministrationTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-15T20:00:00Z");

    private static readonly Guid TenantId = Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61");

    private static readonly Operator Operator = new("operator-subject", "Olivia Operator");

    private static readonly UsageQuery Days = UsageQuery.Create(
        DateTimeOffset.Parse("2026-11-01T00:00:00Z"),
        DateTimeOffset.Parse("2026-12-01T00:00:00Z"),
        UsageQuery.DayName,
        moduleId: null,
        meter: null).Value;

    private static readonly UsageQuery EchoRequestHours = UsageQuery.Create(
        DateTimeOffset.Parse("2026-11-04T13:00:00Z"),
        DateTimeOffset.Parse("2026-11-04T14:00:00Z"),
        UsageQuery.HourName,
        "echo",
        "requests").Value;

    public sealed class ReadAsync
    {
        [Fact]
        public async Task Returns_What_The_Reader_Read()
        {
            var total = new TenantUsageTotal(TenantId, Now, "echo", "requests", 3);
            var reader = new RecordingReader([total]);

            var totals = await Administration(reader).ReadAsync(null, Days, Operator, CancellationToken.None);

            Assert.Equal([total], totals);
        }

        [Fact]
        public async Task Passes_The_Tenant_And_The_Query_To_The_Reader()
        {
            var reader = new RecordingReader([]);

            await Administration(reader).ReadAsync(TenantId, Days, Operator, CancellationToken.None);

            Assert.Equal((TenantId, Days), (reader.TenantId, reader.Query));
        }

        [Fact]
        public async Task Audits_The_Read_By_The_Operator_At_The_Time()
        {
            var reader = new RecordingReader([]);

            await Administration(reader).ReadAsync(null, Days, Operator, CancellationToken.None);

            Assert.Equal(
                (Now, "operator-subject", "Olivia Operator", AuditEntry.UsageRead),
                (reader.Audit!.OccurredAt, reader.Audit.OperatorSubject, reader.Audit.OperatorName, reader.Audit.Action));
        }

        [Fact]
        public async Task Audits_A_Read_Of_Every_Tenant_With_Its_Filters()
        {
            var reader = new RecordingReader([]);

            await Administration(reader).ReadAsync(null, Days, Operator, CancellationToken.None);

            Assert.Equal(
                (AuditEntry.AllTenantsTarget, "Read the daily usage of every tenant from 2026-11-01T00:00:00Z to 2026-12-01T00:00:00Z."),
                (reader.Audit!.Target, reader.Audit.Summary));
        }

        [Fact]
        public async Task Audits_A_Read_Of_One_Tenant_With_Its_Filters()
        {
            var reader = new RecordingReader([]);

            await Administration(reader).ReadAsync(TenantId, EchoRequestHours, Operator, CancellationToken.None);

            Assert.Equal(
                ($"tenants/{TenantId}", $"Read the hourly usage of the tenant {TenantId} of the module 'echo' in the meter 'requests' from 2026-11-04T13:00:00Z to 2026-11-04T14:00:00Z."),
                (reader.Audit!.Target, reader.Audit.Summary));
        }

        private static UsageAdministration Administration(RecordingReader reader) => new(reader, new FakeTimeProvider(Now));
    }

    /// <summary>Records what it was asked and answers with fixed totals.</summary>
    private sealed class RecordingReader(IReadOnlyList<TenantUsageTotal> totals) : IAdminUsageReader
    {
        public Guid? TenantId { get; private set; }

        public UsageQuery? Query { get; private set; }

        public AuditEntry? Audit { get; private set; }

        public Task<IReadOnlyList<TenantUsageTotal>> ReadAsync(Guid? tenantId, UsageQuery query, AuditEntry audit, CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            Query = query;
            Audit = audit;
            return Task.FromResult(totals);
        }
    }
}
