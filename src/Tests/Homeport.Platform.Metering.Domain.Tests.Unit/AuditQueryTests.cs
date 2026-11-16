// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.Metering.Domain.Tests.Unit;

public sealed class AuditQueryTests
{
    public sealed class Create
    {
        private static readonly DateTimeOffset Moment = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Takes_The_Default_Limit_And_No_Filter_When_Nothing_Is_Given()
        {
            var query = AuditQuery.Create(null, null, null, null, null, null).Value;

            Assert.Equal(
                (null, null, null, null, null, AuditQuery.DefaultLimit),
                (query.From, query.To, query.Actor, query.Target, query.Before, query.Limit));
        }

        [Fact]
        public void Treats_Empty_Filters_As_None()
        {
            var query = AuditQuery.Create(null, null, string.Empty, string.Empty, string.Empty, null).Value;

            Assert.Equal((null, null, null), (query.Actor, query.Target, query.Before));
        }

        [Fact]
        public void Converts_The_Period_To_Utc()
        {
            var query = AuditQuery.Create(Moment.ToOffset(TimeSpan.FromHours(1)), null, null, null, null, null).Value;

            Assert.Equal(TimeSpan.Zero, query.From!.Value.Offset);
        }

        [Fact]
        public void Reads_The_Cursor_A_Page_Handed_Out()
        {
            var cursor = new AuditCursor(Moment, Guid.NewGuid());

            var query = AuditQuery.Create(null, null, null, null, cursor.ToString(), null).Value;

            Assert.Equal(cursor, query.Before);
        }

        [Fact]
        public void Fails_When_The_Period_Is_Empty()
        {
            AssertFails(AuditQuery.EmptyPeriodCode, AuditQuery.Create(Moment, Moment, null, null, null, null));
        }

        [Fact]
        public void Fails_When_The_Actor_Is_Longer_Than_A_Subject()
        {
            AssertFails(AuditQuery.ActorTooLongCode, AuditQuery.Create(null, null, new string('a', AuditEntry.MaxFieldLength + 1), null, null, null));
        }

        [Fact]
        public void Fails_When_The_Target_Is_Longer_Than_A_Target()
        {
            AssertFails(AuditQuery.TargetTooLongCode, AuditQuery.Create(null, null, null, new string('t', AuditEntry.MaxFieldLength + 1), null, null));
        }

        [Theory]
        [InlineData("not-a-cursor")]
        [InlineData("123")]
        [InlineData("-1.0123456789abcdef0123456789abcdef")]
        [InlineData("99999999999999999999.0123456789abcdef0123456789abcdef")]
        [InlineData("123.not-a-guid")]
        public void Fails_When_The_Cursor_Is_Not_One_A_Page_Handed_Out(string before)
        {
            AssertFails(AuditQuery.InvalidCursorCode, AuditQuery.Create(null, null, null, null, before, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(AuditQuery.MaxLimit + 1)]
        public void Fails_When_The_Limit_Is_Out_Of_Range(int limit)
        {
            AssertFails(AuditQuery.LimitOutOfRangeCode, AuditQuery.Create(null, null, null, null, null, limit));
        }

        private static void AssertFails(string code, Result<AuditQuery> result)
        {
            Assert.Equal((false, code), (result.IsSuccess, result.Error.Code));
        }
    }
}
