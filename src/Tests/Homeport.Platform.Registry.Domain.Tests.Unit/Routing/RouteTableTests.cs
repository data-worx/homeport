// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Domain.Tests.Unit.Routing;

public sealed class RouteTableTests
{
    private static readonly Uri FirstAddress = new("http://things-1:8080");
    private static readonly Uri SecondAddress = new("http://things-2:8080");

    private static readonly ManifestRoute Echo = new("POST", "/v1/echo", "things.write", "requests", 1);
    private static readonly ManifestRoute Read = new("GET", "/v1/things", "things.read", "requests", 1);
    private static readonly ManifestRoute ReadV2 = new("GET", "/v2/things", "things.read", "requests", 1);

    private static Instance Instance(string instanceId, Uri address, string version, params ManifestRoute[] routes)
    {
        return Instance("things", instanceId, address, version, routes);
    }

    private static Instance Instance(string moduleId, string instanceId, Uri address, string version, params ManifestRoute[] routes)
    {
        var manifest = new Manifest(moduleId, "Things", version, 1, routes, ["things.read", "things.write"], ["requests", "documents"], "/openapi/v1.json");
        return new Instance(instanceId, address, manifest);
    }

    public sealed class Build
    {
        [Fact]
        public void Returns_Nothing_Without_Instances()
        {
            var table = RouteTable.Build([]);

            Assert.Empty(table);
        }

        [Fact]
        public void Sends_Each_Route_Only_To_The_Instances_That_Declare_It()
        {
            var table = RouteTable.Build(
            [
                Instance("a", FirstAddress, "1.0.0", Read),
                Instance("b", SecondAddress, "2.0.0", ReadV2),
            ]);

            var routes = Assert.Single(table).Routes;
            Assert.Equal([FirstAddress], routes.Single(route => route.Path == Read.Path).Instances);
            Assert.Equal([SecondAddress], routes.Single(route => route.Path == ReadV2.Path).Instances);
        }

        [Fact]
        public void Sends_A_Route_Two_Instances_Declare_To_Both()
        {
            var table = RouteTable.Build(
            [
                Instance("b", SecondAddress, "1.0.0", Read),
                Instance("a", FirstAddress, "1.0.0", Read),
            ]);

            var route = Assert.Single(Assert.Single(table).Routes);
            Assert.Equal([FirstAddress, SecondAddress], route.Instances);
        }

        [Fact]
        public void Tells_Routes_Apart_By_Method()
        {
            var write = Read with { Method = "POST", Scope = "things.write" };

            var table = RouteTable.Build([Instance("a", FirstAddress, "1.0.0", Read, write)]);

            Assert.Equal(["GET", "POST"], Assert.Single(table).Routes.Select(route => route.Method));
        }

        [Fact]
        public void Applies_The_Attributes_Of_The_Instance_With_The_Highest_Version()
        {
            var table = RouteTable.Build(
            [
                Instance("a", FirstAddress, "1.10.0", Read with { Meter = "documents", Quantity = 5 }),
                Instance("b", SecondAddress, "1.9.0", Read),
            ]);

            var route = Assert.Single(Assert.Single(table).Routes);
            Assert.Equal(("documents", 5), (route.Meter, route.Quantity));
        }

        [Fact]
        public void Names_The_Document_Of_The_Instance_With_The_Highest_Version()
        {
            var table = RouteTable.Build(
            [
                Instance("a", FirstAddress, "1.9.0", Read),
                Instance("b", SecondAddress, "1.10.0", Read),
            ]);

            var route = Assert.Single(Assert.Single(table).Routes);
            Assert.Equal(("1.10.0", new Uri("http://things-2:8080/openapi/v1.json")), (route.Version, route.OpenApiUrl));
        }

        [Fact]
        public void Ranks_A_Release_Above_Its_Pre_Release()
        {
            var table = RouteTable.Build(
            [
                Instance("a", FirstAddress, "2.0.0-rc.1", Read with { Scope = "things.write" }),
                Instance("b", SecondAddress, "2.0.0", Read),
            ]);

            Assert.Equal("things.read", Assert.Single(Assert.Single(table).Routes).Scope);
        }

        [Fact]
        public void Decides_Between_Equal_Versions_By_The_Lowest_Instance_Id()
        {
            var table = RouteTable.Build(
            [
                Instance("b", SecondAddress, "1.0.0", Read with { Quantity = 2 }),
                Instance("a", FirstAddress, "1.0.0", Read with { Quantity = 3 }),
            ]);

            Assert.Equal(3, Assert.Single(Assert.Single(table).Routes).Quantity);
        }

        [Fact]
        public void Keeps_Modules_Apart()
        {
            var table = RouteTable.Build(
            [
                Instance("things", "a", FirstAddress, "1.0.0", Read),
                Instance("echo", "b", SecondAddress, "1.0.0", Echo),
            ]);

            Assert.Equal(["echo", "things"], table.Select(module => module.ModuleId));
            Assert.Equal([SecondAddress], Assert.Single(table[0].Routes).Instances);
        }

        [Fact]
        public void Orders_Routes_By_Path_Then_Method()
        {
            var write = Read with { Method = "POST" };

            var table = RouteTable.Build([Instance("a", FirstAddress, "1.0.0", ReadV2, write, Echo, Read)]);

            Assert.Equal(
                ["POST /v1/echo", "GET /v1/things", "POST /v1/things", "GET /v2/things"],
                Assert.Single(table).Routes.Select(route => $"{route.Method} {route.Path}"));
        }
    }
}
