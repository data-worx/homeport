// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

namespace Homeport.Sdk.Tests.Unit;

public sealed class HomeportCallerTests
{
    private const string TenantId = "3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61";

    private static ClaimsPrincipal ModuleTokenUser(string scope = "things.write things.read", string tenantId = TenantId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "service-account-local-client"),
                new Claim("tenant_id", tenantId),
                new Claim("client_id", "local-client"),
                new Claim("scope", scope),
            ],
            authenticationType: "Bearer"));
    }

    public sealed class From
    {
        [Fact]
        public void Reads_Tenant_Client_And_Subject_Of_The_Module_Token()
        {
            var caller = HomeportCaller.From(ModuleTokenUser());

            Assert.Equal(Guid.Parse(TenantId), caller.TenantId);
            Assert.Equal("local-client", caller.ClientId);
            Assert.Equal("service-account-local-client", caller.Subject);
        }

        [Fact]
        public void Splits_The_Scopes_At_Spaces()
        {
            var caller = HomeportCaller.From(ModuleTokenUser(scope: "things.write  things.read"));

            Assert.Equal(["things.write", "things.read"], caller.Scopes);
        }

        [Fact]
        public void Throws_For_An_Anonymous_Request()
        {
            Assert.Throws<InvalidOperationException>(() => HomeportCaller.From(new ClaimsPrincipal(new ClaimsIdentity())));
        }

        [Fact]
        public void Throws_For_A_Tenant_Id_That_Is_Not_A_Uuid()
        {
            Assert.Throws<InvalidOperationException>(() => HomeportCaller.From(ModuleTokenUser(tenantId: "acme")));
        }
    }

    public sealed class HasScope
    {
        [Fact]
        public void Is_True_For_A_Granted_Scope()
        {
            Assert.True(HomeportCaller.From(ModuleTokenUser()).HasScope("things.read"));
        }

        [Fact]
        public void Is_False_For_A_Scope_That_Only_Prefixes_A_Granted_One()
        {
            Assert.False(HomeportCaller.From(ModuleTokenUser()).HasScope("things"));
        }
    }
}
