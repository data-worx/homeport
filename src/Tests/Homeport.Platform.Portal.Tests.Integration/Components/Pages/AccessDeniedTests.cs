// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;

namespace Homeport.Platform.Portal.Tests.Integration.Components.Pages;

public sealed class AccessDeniedTests
{
    public sealed class Page(PortalFactory portal) : IClassFixture<PortalFactory>
    {
        [Fact]
        public async Task Shows_The_Trace_Id_To_A_User_Without_The_Claim()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/access-denied");
            request.Headers.Add(PortalFactory.TestUser.Header, PortalFactory.TestUser.WithoutRole);
            request.Headers.Add(TraceParent.Header, TraceParent.Value);

            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await response.Content.ReadAsStringAsync();
            Assert.Contains("Your account has no Homeport role", page, StringComparison.Ordinal);
            Assert.Contains(TraceParent.TraceId, page, StringComparison.Ordinal);
        }
    }
}
