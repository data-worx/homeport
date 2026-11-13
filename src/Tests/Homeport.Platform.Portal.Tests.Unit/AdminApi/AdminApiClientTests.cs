// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

public sealed class AdminApiClientTests
{
    private static readonly Uri AdminEndpoint = new("http://gateway:8000");

    private static readonly Guid TenantId = Guid.Parse("0193a5f0-0000-7000-8000-000000000001");

    public sealed class RenameTenantAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Calls_The_Gateway_Below_The_Platform_Prefix_With_The_Operators_Token()
        {
            gateway.Respond = _ => Json(HttpStatusCode.OK, new TenantResponse(TenantId, "Sales", 1));

            await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                (HttpMethod.Put, $"http://gateway:8000/platform/v1/admin/tenants/{TenantId}", "Bearer operator-token"),
                (request.Method, request.RequestUri!.ToString(), request.Headers.Authorization!.ToString()));
        }

        [Fact]
        public async Task Returns_The_Renamed_Tenant()
        {
            var renamed = new TenantResponse(TenantId, "Sales", 1);
            gateway.Respond = _ => Json(HttpStatusCode.OK, renamed);

            var result = await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            Assert.Equal(renamed, result.Value);
        }

        [Fact]
        public async Task Returns_The_Problem_Details_Of_An_Error()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"type":"https://homeport.data-worx.de/problems/validation","title":"One or more validation errors occurred.","status":400,"traceId":"0af7651916cd43dd8448eb211c80319c","errors":{"name":["A tenant needs a name."]}}""",
                    Encoding.UTF8,
                    MediaTypeNames.Application.ProblemJson),
            };

            var result = await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest(" "), CancellationToken.None);

            Assert.Equal(
                ("0af7651916cd43dd8448eb211c80319c", "A tenant needs a name."),
                (result.Problem!.TraceId, Assert.Single(result.Problem.Errors![TenantsApi.NameField])));
        }

        [Fact]
        public async Task Describes_An_Error_Without_Problem_Details_By_Its_Status()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>bad gateway</html>") };

            var result = await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            Assert.Equal((new Uri("about:blank"), 502), (result.Problem!.Type, result.Problem.Status));
        }

        [Fact]
        public async Task Describes_An_Unreachable_Admin_Api_As_A_Problem()
        {
            gateway.Failure = new HttpRequestException("Connection refused");

            var result = await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            Assert.Equal(AdminApiClient.UnreachableTitle, result.Problem!.Title);
        }

        [Fact]
        public async Task Asks_The_Operator_To_Sign_In_Again_When_The_Gateway_Refuses_The_Token()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.Unauthorized);

            var result = await ClientOf("operator-token").RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            Assert.True(result.SignInRequired);
        }

        [Fact]
        public async Task Asks_The_Operator_To_Sign_In_Again_Without_A_Token_And_Calls_Nothing()
        {
            var expired = identityProvider.Clock.GetUtcNow().AddMinutes(-1);
            var tokens = new OperatorTokens(new SignedInOperator("operator-token", refreshToken: null, expired), identityProvider.Refresher(), identityProvider.Clock);
            var client = new AdminApiClient(new HttpClient(gateway) { BaseAddress = AdminEndpoint }, tokens, NullLogger<AdminApiClient>.Instance);

            var result = await client.RenameTenantAsync(TenantId, new RenameTenantRequest("Sales"), CancellationToken.None);

            Assert.True(result.SignInRequired);
            Assert.Empty(gateway.Received);
        }

        private AdminApiClient ClientOf(string accessToken)
        {
            var valid = identityProvider.Clock.GetUtcNow().AddMinutes(5);
            var tokens = new OperatorTokens(new SignedInOperator(accessToken, "refresh-token", valid), identityProvider.Refresher(), identityProvider.Clock);
            return new AdminApiClient(new HttpClient(gateway) { BaseAddress = AdminEndpoint }, tokens, NullLogger<AdminApiClient>.Instance);
        }

        private static HttpResponseMessage Json<TValue>(HttpStatusCode status, TValue value)
        {
            return new HttpResponseMessage(status) { Content = JsonContent.Create(value) };
        }
    }
}
