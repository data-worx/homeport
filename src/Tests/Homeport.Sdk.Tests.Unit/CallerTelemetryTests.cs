// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Security.Claims;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Sdk.Tests.Unit;

public sealed class CallerTelemetryTests
{
    private const string TenantId = "3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61";

    public sealed class Tag
    {
        [Fact]
        public void Tags_Module_Tenant_And_Client_Of_A_Module_Token()
        {
            using var activity = new Activity("request");
            var user = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenant_id", TenantId), new Claim("client_id", "local-client")],
                authenticationType: "Bearer"));

            CallerTelemetry.Tag(activity, "echo", user);

            Assert.Equal(
                ("echo", TenantId, "local-client"),
                (activity.GetTagItem(TelemetryAttributes.ModuleId), activity.GetTagItem(TelemetryAttributes.TenantId), activity.GetTagItem(TelemetryAttributes.ClientId)));
        }

        [Fact]
        public void Tags_Only_The_Module_Of_An_Anonymous_Caller()
        {
            using var activity = new Activity("request");

            CallerTelemetry.Tag(activity, "echo", new ClaimsPrincipal(new ClaimsIdentity()));

            Assert.Equal([TelemetryAttributes.ModuleId], activity.TagObjects.Select(tag => tag.Key));
        }
    }
}
