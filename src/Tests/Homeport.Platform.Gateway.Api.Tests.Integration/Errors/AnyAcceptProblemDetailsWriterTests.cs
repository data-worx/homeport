// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Errors;

public sealed class AnyAcceptProblemDetailsWriterTests
{
    // Each test sets the bucket it draws from; the tests of one class run one after another.
    public sealed class WriteAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string CsvMediaType = "text/csv";

        private const string ProblemTypePrefix = "https://homeport.data-worx.de/problems/";

        [Fact]
        public async Task Answers_A_Rejected_Request_Of_A_Caller_Accepting_Only_Csv_As_Rate_Limited_Problem()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            using var response = await SendAcceptingCsvAsync("/modules/echo" + ForwardingGateway.RequestPath);

            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.Equal(
                (HttpStatusCode.TooManyRequests, MediaTypeNames.Application.ProblemJson, ProblemTypePrefix + "rate-limited"),
                (response.StatusCode, response.Content.Headers.ContentType?.MediaType, problem?["type"]?.GetValue<string>()));
        }

        [Fact]
        public async Task Answers_An_Unknown_Module_For_A_Caller_Accepting_Only_Csv_As_Unknown_Module_Problem_With_Trace_Id()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            using var response = await SendAcceptingCsvAsync("/modules/unknown/v1/request");

            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.Equal(
                (HttpStatusCode.NotFound, MediaTypeNames.Application.ProblemJson, ProblemTypePrefix + "unknown-module", true),
                (response.StatusCode, response.Content.Headers.ContentType?.MediaType, problem?["type"]?.GetValue<string>(), problem?["traceId"] is not null));
        }

        private async Task<HttpResponseMessage> SendAcceptingCsvAsync(string path)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(CsvMediaType));

            return await gateway.CreateClient().SendAsync(request);
        }
    }
}
