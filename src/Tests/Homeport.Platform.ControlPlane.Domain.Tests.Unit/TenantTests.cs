// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain.Tests.Unit;

public sealed class TenantTests
{
    private static readonly Guid Id = Guid.Parse("7c1e5c58-2f0a-4c1b-9a43-1d5f0b6f6a01");

    public sealed class Create
    {
        [Fact]
        public void Returns_The_Tenant_When_The_Name_Is_Free()
        {
            var result = Tenant.Create(Id, "Sales", ["Finance"]);

            Assert.True(result.IsSuccess);
            Assert.Equal("Sales", result.Value.Name);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Fails_With_Name_Required_When_The_Name_Is_Empty(string name)
        {
            var result = Tenant.Create(Id, name, []);

            Assert.Equal(Tenant.NameRequiredCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Name_Too_Long_When_The_Name_Exceeds_The_Maximum()
        {
            var result = Tenant.Create(Id, new string('a', Tenant.MaxNameLength + 1), []);

            Assert.Equal(Tenant.NameTooLongCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Name_Taken_When_Another_Tenant_Has_The_Name()
        {
            var result = Tenant.Create(Id, "Sales", ["Sales"]);

            Assert.Equal(Tenant.NameTakenCode, result.Error.Code);
        }

        [Fact]
        public void Accepts_A_Name_That_Differs_From_A_Taken_One_Only_In_Case()
        {
            var result = Tenant.Create(Id, "sales", ["Sales"]);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Fails_With_Name_Taken_When_The_Name_Differs_From_A_Taken_One_Only_In_Surrounding_Whitespace()
        {
            var result = Tenant.Create(Id, " Sales ", ["Sales"]);

            Assert.Equal(Tenant.NameTakenCode, result.Error.Code);
        }

        [Fact]
        public void Stores_The_Name_Without_Surrounding_Whitespace()
        {
            var result = Tenant.Create(Id, "  Sales ", []);

            Assert.Equal("Sales", result.Value.Name);
        }
    }

    public sealed class Rename
    {
        [Fact]
        public void Changes_The_Name_When_It_Is_Free()
        {
            var tenant = new Tenant { Id = Id, Name = "Sales" };

            var result = tenant.Rename("Marketing", ["Finance"]);

            Assert.True(result.IsSuccess);
            Assert.Equal("Marketing", tenant.Name);
        }

        [Fact]
        public void Keeps_The_Name_When_The_New_One_Is_Empty()
        {
            var tenant = new Tenant { Id = Id, Name = "Sales" };

            var result = tenant.Rename(" ", []);

            Assert.Equal(Tenant.NameRequiredCode, result.Error.Code);
            Assert.Equal("Sales", tenant.Name);
        }

        [Fact]
        public void Keeps_The_Name_When_Another_Tenant_Has_The_New_One()
        {
            var tenant = new Tenant { Id = Id, Name = "Sales" };

            var result = tenant.Rename("Finance", ["Finance"]);

            Assert.Equal(Tenant.NameTakenCode, result.Error.Code);
            Assert.Equal("Sales", tenant.Name);
        }

        [Fact]
        public void Accepts_A_Name_That_Differs_From_Another_Tenants_Only_In_Case()
        {
            var tenant = new Tenant { Id = Id, Name = "Sales" };

            var result = tenant.Rename("finance", ["Finance"]);

            Assert.True(result.IsSuccess);
        }
    }
}
