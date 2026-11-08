// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Api.Tests.Integration.ApiDocs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Controllers;

public sealed class ApiDocsControllerTests
{
    public sealed class Get : IAsyncLifetime
    {
        private const string PollerCategory = "Homeport.Platform.Gateway.Api.Routing.RouteTablePoller";
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan PlatformInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

        private readonly DocumentingGateway docs = new();

        public Task InitializeAsync() => docs.InitializeAsync();

        public Task DisposeAsync() => docs.DisposeAsync();

        [Fact]
        public async Task Puts_Every_Path_Of_A_Module_Under_Its_Public_Prefix()
        {
            var document = await docs.DocumentAsync("echo");

            Assert.All(Paths(document), path => Assert.StartsWith("/modules/echo/", path, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Leaves_Out_An_Operation_No_Route_Declares()
        {
            var document = await docs.DocumentAsync("echo");

            Assert.DoesNotContain("/modules/echo" + DocumentingGateway.UndeclaredPath, Paths(document));
        }

        [Fact]
        public async Task Shows_Each_Route_From_The_Instance_With_The_Highest_Version_That_Declares_It()
        {
            var document = await docs.DocumentAsync("echo");

            Assert.Equal(
                ("Old" + DocumentingGateway.OldVersion, "Shared" + DocumentingGateway.NewVersion),
                (OperationId(document, DocumentingGateway.OldPath), OperationId(document, DocumentingGateway.SharedPath)));
        }

        [Fact]
        public async Task Serves_The_Platform_Document()
        {
            var document = await docs.DocumentAsync("platform");

            Assert.Equal("Homeport platform", document["info"]!["title"]!.GetValue<string>());
        }

        [Fact]
        public async Task Lists_The_Usage_Export_In_The_Platform_Document_Under_Its_Public_Path()
        {
            var document = await docs.DocumentAsync("platform");

            Assert.Equal(["/platform/v1/usage"], Paths(document));
        }

        [Fact]
        public async Task Takes_The_Query_Parameters_And_Media_Types_Of_The_Usage_Export_From_Metering()
        {
            var operation = (await docs.DocumentAsync("platform"))["paths"]!["/platform/v1/usage"]!["get"]!;

            Assert.Equal(
                ("from", "application/json, text/csv"),
                (operation["parameters"]![0]!["name"]!.GetValue<string>(), string.Join(", ", operation["responses"]!["200"]!["content"]!.AsObject().Select(entry => entry.Key))));
        }

        [Fact]
        public async Task Leaves_Out_A_Platform_Operation_The_Gateway_Does_Not_Route()
        {
            var document = await docs.DocumentAsync("platform");

            Assert.DoesNotContain("/platform" + DocumentingGateway.UnroutedPlatformPath, Paths(document));
        }

        [Fact]
        public void Reads_The_Platform_Document_With_A_Service_Token_For_Metering()
        {
            var token = new JsonWebToken(docs.MeteringToken);

            Assert.Equal(
                ("homeport-metering", "homeport-gateway"),
                (Assert.Single(token.Audiences), token.Subject));
        }

        // A failure no resilience handler retries, so the test does not wait out a backoff.
        [Fact]
        public async Task Keeps_The_Last_Platform_Document_While_Metering_Fails_To_Serve_Its_Own()
        {
            docs.MeteringDocumentFails = true;

            docs.PlatformTime.Advance(PlatformInterval);
            await docs.WaitForUnreadDocumentsAsync(1);

            Assert.Equal(["/platform/v1/usage"], Paths(await docs.DocumentAsync("platform")));
        }

        [Fact]
        public async Task Answers_Not_Found_As_Problem_For_An_Unknown_Document()
        {
            var response = await docs.Gateway.CreateClient().GetAsync("/platform/docs/openapi/unknown.json");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
        }

        // Failures no resilience handler retries, so the test does not wait out a backoff.
        [Fact]
        public async Task Keeps_The_Last_Documents_While_The_Registry_Fails()
        {
            docs.Registry.FailureStatus = StatusCodes.Status401Unauthorized;

            docs.Time.Advance(PollInterval);
            await WaitUntil(() => docs.Gateway.Services.GetFakeLogCollector().GetSnapshot()
                .Any(entry => entry.Category == PollerCategory && entry.Level == LogLevel.Warning));

            Assert.Equal(2, Paths(await docs.DocumentAsync("echo")).Count);
        }

        [Fact]
        public async Task Keeps_The_Last_Read_Document_Of_An_Instance_That_Fails_To_Serve_It()
        {
            docs.OldDocumentFails = true;
            docs.Registry.Table = docs.Table(DocumentingGateway.UndeclaredPath);

            docs.Time.Advance(PollInterval);
            await docs.WaitForDocsAsync(2);

            Assert.Equal("Old" + DocumentingGateway.OldVersion, OperationId(await docs.DocumentAsync("echo"), DocumentingGateway.OldPath));
        }

        private static List<string> Paths(JsonObject document) =>
            document["paths"]!.AsObject().Select(path => path.Key).ToList();

        private static string? OperationId(JsonObject document, string path) =>
            document["paths"]?["/modules/echo" + path]?["get"]?["operationId"]?.GetValue<string>();

        private static async Task WaitUntil(Func<bool> condition)
        {
            using var timeout = new CancellationTokenSource(WaitTimeout);
            while (!condition())
            {
                await Task.Delay(WaitPoll, timeout.Token);
            }
        }
    }
}
