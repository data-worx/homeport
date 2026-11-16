// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using Homeport.Platform.Portal.AdminApi;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

public sealed class AdminApiClientTests
{
    private static readonly Uri AdminEndpoint = new("http://gateway:8000");

    private static readonly Guid TenantId = Guid.Parse("0193a5f0-0000-7000-8000-000000000001");

    private static readonly DateTimeOffset Day = new(2026, 11, 4, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid PlanId = Guid.Parse("0193a5f0-0000-7000-8000-0000000000b1");

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

    public sealed class DeletePlanAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Calls_Delete_On_The_Plan_And_Returns_Its_Id_On_No_Content()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

            var result = await ClientOf().DeletePlanAsync(PlanId, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                (HttpMethod.Delete, $"http://gateway:8000/platform/v1/admin/plans/{PlanId}", PlanId),
                (request.Method, request.RequestUri!.ToString(), result.Value));
        }

        [Fact]
        public async Task Returns_The_Problem_Details_Of_A_Plan_In_Use()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    """{"type":"https://homeport.data-worx.de/problems/plan-in-use","title":"The plan is in use.","status":409,"traceId":"0af7651916cd43dd8448eb211c80319c"}""",
                    Encoding.UTF8,
                    MediaTypeNames.Application.ProblemJson),
            };

            var result = await ClientOf().DeletePlanAsync(PlanId, CancellationToken.None);

            Assert.Equal(new Uri("https://homeport.data-worx.de/problems/plan-in-use"), result.Problem!.Type);
        }

        private AdminApiClient ClientOf()
        {
            var valid = identityProvider.Clock.GetUtcNow().AddMinutes(5);
            var tokens = new OperatorTokens(new SignedInOperator("operator-token", "refresh-token", valid), identityProvider.Refresher(), identityProvider.Clock);
            return new AdminApiClient(new HttpClient(gateway) { BaseAddress = AdminEndpoint }, tokens, NullLogger<AdminApiClient>.Instance);
        }
    }

    public sealed class GetClientsAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Asks_For_The_Clients_Of_The_Tenant_With_The_Tenant_Query()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ClientsResponse([])) };

            await ClientOf(identityProvider, gateway).GetClientsAsync(TenantId, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal($"http://gateway:8000/platform/v1/admin/clients?tenantId={TenantId}", request.RequestUri!.ToString());
        }

        [Fact]
        public async Task Asks_For_Every_Client_Without_A_Tenant()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new ClientsResponse([])) };

            await ClientOf(identityProvider, gateway).GetClientsAsync(tenantId: null, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal("http://gateway:8000/platform/v1/admin/clients", request.RequestUri!.ToString());
        }
    }

    public sealed class RemoveClientAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Calls_Delete_On_The_Escaped_Client_Id_And_Returns_It_On_No_Content()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.NoContent);

            var result = await ClientOf(identityProvider, gateway).RemoveClientAsync("billing service", CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                (HttpMethod.Delete, "http://gateway:8000/platform/v1/admin/clients/billing%20service", "billing service"),
                (request.Method, request.RequestUri!.AbsoluteUri, result.Value));
        }
    }

    public sealed class GetUsageAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Asks_For_The_Period_Granularity_And_Every_Filter_Set()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new AdminUsageResponse(DateTime.UnixEpoch, DateTime.UnixEpoch, UsageApi.Day, [])) };
            var filter = new UsageFilter(Day, Day.AddDays(1), UsageApi.Day, TenantId, "echo", "requests");

            await ClientOf(identityProvider, gateway).GetUsageAsync(filter, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                $"http://gateway:8000/platform/v1/admin/usage?from=2026-11-04T00:00:00Z&to=2026-11-05T00:00:00Z&granularity=day&tenant={TenantId}&module=echo&meter=requests",
                Uri.UnescapeDataString(request.RequestUri!.AbsoluteUri));
        }

        [Fact]
        public async Task Leaves_Out_The_Filters_That_Are_Not_Set()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new AdminUsageResponse(DateTime.UnixEpoch, DateTime.UnixEpoch, UsageApi.Hour, [])) };
            var filter = new UsageFilter(Day, Day.AddDays(1), UsageApi.Hour, Tenant: null, Module: null, Meter: null);

            await ClientOf(identityProvider, gateway).GetUsageAsync(filter, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                "http://gateway:8000/platform/v1/admin/usage?from=2026-11-04T00:00:00Z&to=2026-11-05T00:00:00Z&granularity=hour",
                Uri.UnescapeDataString(request.RequestUri!.AbsoluteUri));
        }
    }

    public sealed class ExportUsageAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();
        private readonly StandInHandler gateway = new();

        [Fact]
        public async Task Asks_For_The_Media_Type_And_Returns_The_Body_As_It_Is()
        {
            gateway.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("period,tenant,module,meter,quantity\r\n", Encoding.UTF8, UsageApi.CsvMediaType) };
            var filter = new UsageFilter(Day, Day.AddDays(1), UsageApi.Hour, Tenant: null, Module: null, Meter: null);

            var result = await ClientOf(identityProvider, gateway).ExportUsageAsync(filter, UsageApi.CsvMediaType, CancellationToken.None);

            var (request, _) = Assert.Single(gateway.Received);
            Assert.Equal(
                (UsageApi.CsvMediaType, "period,tenant,module,meter,quantity\r\n"),
                (Assert.Single(request.Headers.Accept).MediaType, Encoding.UTF8.GetString(result.Value!)));
        }
    }

    private static AdminApiClient ClientOf(StandInIdentityProvider identityProvider, StandInHandler gateway)
    {
        var valid = identityProvider.Clock.GetUtcNow().AddMinutes(5);
        var tokens = new OperatorTokens(new SignedInOperator("operator-token", "refresh-token", valid), identityProvider.Refresher(), identityProvider.Clock);
        return new AdminApiClient(new HttpClient(gateway) { BaseAddress = AdminEndpoint }, tokens, NullLogger<AdminApiClient>.Instance);
    }
}
