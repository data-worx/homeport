// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Homeport.Shared.ServiceDefaults.Tests.Integration;

public sealed class ServiceDefaultsExtensionsTests
{
    public sealed class MapDefaultEndpoints
    {
        [Theory]
        [InlineData(ServiceDefaultsExtensions.LivenessPath)]
        [InlineData(ServiceDefaultsExtensions.ReadinessPath)]
        public async Task Answers_Healthy_Outside_Development(string path)
        {
            await using var app = await TestHost.StartAsync();

            var response = await app.GetTestClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Answers_Unavailable_When_A_Readiness_Check_Fails()
        {
            await using var app = await TestHost.StartAsync(HealthStatus.Unhealthy);

            var response = await app.GetTestClient().GetAsync(ServiceDefaultsExtensions.ReadinessPath);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task Hides_The_Detail_Of_A_Failing_Check()
        {
            await using var app = await TestHost.StartAsync(HealthStatus.Unhealthy);

            var response = await app.GetTestClient().GetAsync(ServiceDefaultsExtensions.ReadinessPath);

            Assert.Equal("Unhealthy", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Stays_Live_When_A_Readiness_Check_Fails()
        {
            await using var app = await TestHost.StartAsync(HealthStatus.Unhealthy);

            var response = await app.GetTestClient().GetAsync(ServiceDefaultsExtensions.LivenessPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
