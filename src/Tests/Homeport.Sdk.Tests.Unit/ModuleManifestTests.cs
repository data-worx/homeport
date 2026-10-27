// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Sdk.Tests.Unit;

public sealed class ModuleManifestTests
{
    private static readonly ManifestRoute ValidRoute = new("POST", "/v1/things", "things.write", "requests", 1);

    private static readonly ModuleManifest Valid = new(
        Id: "things",
        Name: "Things",
        Version: "1.2.3",
        Routes: [ValidRoute],
        Scopes: ["things.write"],
        Meters: ["requests"]);

    public sealed class Validate
    {
        [Fact]
        public void Returns_Nothing_For_A_Valid_Manifest()
        {
            var problems = Valid.Validate();

            Assert.Empty(problems);
        }

        [Theory]
        [InlineData("Things")]
        [InlineData("1things")]
        [InlineData("things_v2")]
        [InlineData("")]
        public void Reports_An_Id_That_Is_Not_Lower_Case_Letters_Digits_And_Dashes(string id)
        {
            var problems = (Valid with { Id = id }).Validate();

            Assert.Contains(problems, problem => problem.StartsWith($"Id '{id}'", StringComparison.Ordinal));
        }

        [Fact]
        public void Reports_The_Reserved_Id_Platform()
        {
            var problems = (Valid with { Id = "platform" }).Validate();

            Assert.Contains("Id 'platform' is reserved.", problems);
        }

        [Fact]
        public void Reports_An_Empty_Name()
        {
            var problems = (Valid with { Name = " " }).Validate();

            Assert.Contains("Name must not be empty.", problems);
        }

        [Fact]
        public void Reports_An_Empty_Version()
        {
            var problems = (Valid with { Version = "" }).Validate();

            Assert.Contains("Version must not be empty.", problems);
        }

        [Fact]
        public void Reports_A_Manifest_Without_Routes()
        {
            var problems = (Valid with { Routes = [] }).Validate();

            Assert.Contains("At least one route must be declared.", problems);
        }

        [Theory]
        [InlineData("post")]
        [InlineData("HEAD")]
        public void Reports_A_Route_With_An_Unsupported_Method(string method)
        {
            var problems = (Valid with { Routes = [ValidRoute with { Method = method }] }).Validate();

            Assert.Contains(problems, problem => problem.EndsWith("method must be one of GET, POST, PUT, PATCH, DELETE.", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("/things")]
        [InlineData("/v0/things")]
        [InlineData("/v1")]
        [InlineData("v1/things")]
        public void Reports_A_Route_Path_Without_A_Major_Version(string path)
        {
            var problems = (Valid with { Routes = [ValidRoute with { Path = path }] }).Validate();

            Assert.Contains($"Route POST {path}: path must start with /v{{major}}/.", problems);
        }

        [Fact]
        public void Reports_A_Route_Scope_The_Manifest_Does_Not_Declare()
        {
            var problems = (Valid with { Routes = [ValidRoute with { Scope = "things.read" }] }).Validate();

            Assert.Contains("Route POST /v1/things: scope 'things.read' is not declared in the manifest's scopes.", problems);
        }

        [Fact]
        public void Reports_A_Route_Meter_The_Manifest_Does_Not_Declare()
        {
            var problems = (Valid with { Routes = [ValidRoute with { Meter = "documents" }] }).Validate();

            Assert.Contains("Route POST /v1/things: meter 'documents' is not declared in the manifest's meters.", problems);
        }

        [Fact]
        public void Reports_A_Route_Quantity_Below_One()
        {
            var problems = (Valid with { Routes = [ValidRoute with { Quantity = 0 }] }).Validate();

            Assert.Contains("Route POST /v1/things: quantity must be at least 1.", problems);
        }
    }
}
