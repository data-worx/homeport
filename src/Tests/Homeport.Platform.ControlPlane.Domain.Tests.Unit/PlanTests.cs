// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain.Tests.Unit;

public sealed class PlanTests
{
    private static readonly Guid Id = Guid.Parse("0193a5f0-0000-7000-8000-0000000000b1");

    public sealed class Create
    {
        [Fact]
        public void Returns_The_Plan_When_The_Name_Is_Free_And_The_Limits_Positive()
        {
            var result = Plan.Create(Id, "Gold", 50, 100, ["Silver"]);

            Assert.True(result.IsSuccess);
            Assert.Equal(("Gold", 50, 100), (result.Value.Name, result.Value.RequestsPerSecond, result.Value.Burst));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Fails_With_Name_Required_When_The_Name_Is_Empty(string name)
        {
            var result = Plan.Create(Id, name, 10, 10, []);

            Assert.Equal(Plan.NameRequiredCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Name_Too_Long_When_The_Name_Exceeds_The_Maximum()
        {
            var result = Plan.Create(Id, new string('a', Plan.MaxNameLength + 1), 10, 10, []);

            Assert.Equal(Plan.NameTooLongCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Name_Taken_When_Another_Plan_Has_The_Name()
        {
            var result = Plan.Create(Id, "Gold", 10, 10, ["Gold"]);

            Assert.Equal(Plan.NameTakenCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Name_Taken_When_The_Name_Differs_From_A_Taken_One_Only_In_Surrounding_Whitespace()
        {
            var result = Plan.Create(Id, " Gold ", 10, 10, ["Gold"]);

            Assert.Equal(Plan.NameTakenCode, result.Error.Code);
        }

        [Fact]
        public void Accepts_A_Name_That_Differs_From_A_Taken_One_Only_In_Case()
        {
            var result = Plan.Create(Id, "gold", 10, 10, ["Gold"]);

            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Fails_With_Requests_Per_Second_Not_Positive_When_The_Rate_Is_Not_Positive(int requestsPerSecond)
        {
            var result = Plan.Create(Id, "Gold", requestsPerSecond, 10, []);

            Assert.Equal(Plan.RequestsPerSecondNotPositiveCode, result.Error.Code);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Fails_With_Burst_Not_Positive_When_The_Burst_Is_Not_Positive(int burst)
        {
            var result = Plan.Create(Id, "Gold", 10, burst, []);

            Assert.Equal(Plan.BurstNotPositiveCode, result.Error.Code);
        }

        [Fact]
        public void Accepts_A_Burst_Below_The_Rate()
        {
            var result = Plan.Create(Id, "Gold", 100, 1, []);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Stores_The_Name_Without_Surrounding_Whitespace()
        {
            var result = Plan.Create(Id, "  Gold ", 10, 10, []);

            Assert.Equal("Gold", result.Value.Name);
        }
    }

    public sealed class Change
    {
        [Fact]
        public void Changes_The_Name_And_Limits_When_They_Are_Valid()
        {
            var plan = NewPlan();

            var result = plan.Change("Platinum", 200, 400, ["Silver"]);

            Assert.True(result.IsSuccess);
            Assert.Equal(("Platinum", 200, 400), (plan.Name, plan.RequestsPerSecond, plan.Burst));
        }

        [Fact]
        public void Keeps_The_Plan_When_The_New_Name_Is_Empty()
        {
            var plan = NewPlan();

            var result = plan.Change(" ", 200, 400, []);

            Assert.Equal(Plan.NameRequiredCode, result.Error.Code);
            Assert.Equal(("Gold", 50, 100), (plan.Name, plan.RequestsPerSecond, plan.Burst));
        }

        [Fact]
        public void Keeps_The_Plan_When_Another_Plan_Has_The_New_Name()
        {
            var plan = NewPlan();

            var result = plan.Change("Silver", 200, 400, ["Silver"]);

            Assert.Equal(Plan.NameTakenCode, result.Error.Code);
            Assert.Equal(("Gold", 50, 100), (plan.Name, plan.RequestsPerSecond, plan.Burst));
        }

        [Fact]
        public void Keeps_The_Plan_When_The_Rate_Is_Zero()
        {
            var plan = NewPlan();

            var result = plan.Change("Gold", 0, 400, []);

            Assert.Equal(Plan.RequestsPerSecondNotPositiveCode, result.Error.Code);
            Assert.Equal(("Gold", 50, 100), (plan.Name, plan.RequestsPerSecond, plan.Burst));
        }

        [Fact]
        public void Keeps_The_Plan_When_The_Burst_Is_Zero()
        {
            var plan = NewPlan();

            var result = plan.Change("Gold", 200, 0, []);

            Assert.Equal(Plan.BurstNotPositiveCode, result.Error.Code);
            Assert.Equal(("Gold", 50, 100), (plan.Name, plan.RequestsPerSecond, plan.Burst));
        }
    }

    public sealed class CheckDeletable
    {
        [Fact]
        public void Returns_The_Plan_When_No_Client_Uses_It()
        {
            var plan = NewPlan();

            var result = plan.CheckDeletable(clientCount: 0);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Fails_With_In_Use_When_A_Client_Uses_It()
        {
            var plan = NewPlan();

            var result = plan.CheckDeletable(clientCount: 1);

            Assert.Equal(Plan.InUseCode, result.Error.Code);
        }
    }

    private static Plan NewPlan()
    {
        return new Plan { Id = Id, Name = "Gold", RequestsPerSecond = 50, Burst = 100 };
    }
}
