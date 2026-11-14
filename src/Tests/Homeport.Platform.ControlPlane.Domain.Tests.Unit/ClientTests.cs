// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain.Tests.Unit;

public sealed class ClientTests
{
    private static readonly Tenant Sales = new() { Id = Guid.Parse("0193a5f0-0000-7000-8000-0000000000a1"), Name = "Sales" };

    private static readonly Plan Gold = new() { Id = Guid.Parse("0193a5f0-0000-7000-8000-0000000000b1"), Name = "Gold", RequestsPerSecond = 50, Burst = 100 };

    private static readonly Plan Silver = new() { Id = Guid.Parse("0193a5f0-0000-7000-8000-0000000000b2"), Name = "Silver", RequestsPerSecond = 10, Burst = 20 };

    public sealed class Register
    {
        [Fact]
        public void Returns_The_Client_Of_The_Tenant_On_The_Plan()
        {
            var result = Client.Register("billing-service", Sales, Gold, registered: false);

            Assert.True(result.IsSuccess);
            Assert.Equal(("billing-service", Sales.Id, Gold.Id), (result.Value.ClientId, result.Value.TenantId, result.Value.PlanId));
        }

        [Fact]
        public void Keeps_The_Client_Id_Exactly_As_Given()
        {
            var result = Client.Register(" billing-service ", Sales, Gold, registered: false);

            Assert.Equal(" billing-service ", result.Value.ClientId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Fails_With_Client_Id_Required_When_The_Client_Id_Is_Empty(string clientId)
        {
            var result = Client.Register(clientId, Sales, Gold, registered: false);

            Assert.Equal(Client.ClientIdRequiredCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Client_Id_Too_Long_When_The_Client_Id_Exceeds_The_Maximum()
        {
            var result = Client.Register(new string('a', Client.MaxClientIdLength + 1), Sales, Gold, registered: false);

            Assert.Equal(Client.ClientIdTooLongCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Client_Id_Taken_When_The_Client_Is_Already_Registered()
        {
            var result = Client.Register("billing-service", Sales, Gold, registered: true);

            Assert.Equal(Client.ClientIdTakenCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Tenant_Unknown_When_There_Is_No_Such_Tenant()
        {
            var result = Client.Register("billing-service", tenant: null, Gold, registered: false);

            Assert.Equal(Client.TenantUnknownCode, result.Error.Code);
        }

        [Fact]
        public void Fails_With_Plan_Unknown_When_There_Is_No_Such_Plan()
        {
            var result = Client.Register("billing-service", Sales, plan: null, registered: false);

            Assert.Equal(Client.PlanUnknownCode, result.Error.Code);
        }
    }

    public sealed class ChangePlan
    {
        [Fact]
        public void Holds_The_Client_To_The_New_Plan()
        {
            var client = new Client { ClientId = "billing-service", TenantId = Sales.Id, PlanId = Gold.Id };

            client.ChangePlan(Silver);

            Assert.Equal(Silver.Id, client.PlanId);
        }

        [Fact]
        public void Keeps_The_Plan_And_Fails_With_Plan_Unknown_When_There_Is_No_Such_Plan()
        {
            var client = new Client { ClientId = "billing-service", TenantId = Sales.Id, PlanId = Gold.Id };

            var result = client.ChangePlan(plan: null);

            Assert.Equal((Client.PlanUnknownCode, Gold.Id), (result.Error.Code, client.PlanId));
        }
    }
}
