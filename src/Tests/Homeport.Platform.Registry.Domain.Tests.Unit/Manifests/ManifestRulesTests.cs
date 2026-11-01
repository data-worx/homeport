// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;

namespace Homeport.Platform.Registry.Domain.Tests.Unit.Manifests;

public sealed class ManifestRulesTests
{
    private const string ModuleId = "things";

    private static readonly ManifestRoute ValidRoute = new("POST", "/v1/things", "things.write", "requests", 1);

    private static readonly Manifest Valid = new(
        Id: ModuleId,
        Name: "Things",
        Version: "1.2.3",
        ContractVersion: 0,
        Routes: [ValidRoute],
        Scopes: ["things.write"],
        Meters: ["requests"]);

    public sealed class Validate
    {
        [Fact]
        public void Returns_Nothing_For_A_Valid_Manifest()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid);

            Assert.Empty(errors);
        }

        [Fact]
        public void Reports_An_Unsupported_Contract_Version_Naming_The_Supported_Ones()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { ContractVersion = 99 });

            Assert.Equal(["Contract version 99 is not supported; supported versions: 0."], errors["contractVersion"]);
        }

        [Theory]
        [InlineData("Things")]
        [InlineData("1things")]
        [InlineData("things_v2")]
        [InlineData("")]
        public void Reports_An_Id_That_Is_Not_Lower_Case_Letters_Digits_And_Dashes(string id)
        {
            var errors = ManifestRules.Validate(id, Valid with { Id = id });

            Assert.Contains(errors["id"], message => message.StartsWith($"Id '{id}' must start", StringComparison.Ordinal));
        }

        [Fact]
        public void Reports_The_Reserved_Id_Platform()
        {
            var errors = ManifestRules.Validate("platform", Valid with { Id = "platform" });

            Assert.Equal(["Id 'platform' is reserved."], errors["id"]);
        }

        [Fact]
        public void Reports_An_Id_Other_Than_The_Module_Id_Of_The_Registration()
        {
            var errors = ManifestRules.Validate("other", Valid);

            Assert.Equal(["Id 'things' does not match the module id 'other' the registration is made for."], errors["id"]);
        }

        [Fact]
        public void Reports_An_Empty_Name()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Name = " " });

            Assert.Equal(["Name must not be empty."], errors["name"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1.2")]
        [InlineData("v1.2.3")]
        public void Reports_A_Version_That_Is_Not_Semantic(string version)
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Version = version });

            Assert.Equal([$"Version '{version}' is not a semantic version."], errors["version"]);
        }

        [Fact]
        public void Reports_A_Manifest_Without_Routes()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [] });

            Assert.Equal(["At least one route must be declared."], errors["routes"]);
        }

        [Theory]
        [InlineData("post")]
        [InlineData("HEAD")]
        public void Reports_A_Route_With_An_Unsupported_Method(string method)
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute with { Method = method }] });

            Assert.Equal(["Method must be one of GET, POST, PUT, PATCH, DELETE."], errors["routes[0].method"]);
        }

        [Theory]
        [InlineData("/things")]
        [InlineData("/v0/things")]
        [InlineData("/v1")]
        [InlineData("v1/things")]
        public void Reports_A_Route_Path_Without_A_Major_Version(string path)
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute with { Path = path }] });

            Assert.Equal([$"Path '{path}' must start with /v{{major}}/."], errors["routes[0].path"]);
        }

        [Fact]
        public void Reports_A_Route_Scope_The_Manifest_Does_Not_Declare()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute with { Scope = "things.read" }] });

            Assert.Equal(["Scope 'things.read' is not declared in the manifest's scopes."], errors["routes[0].scope"]);
        }

        [Fact]
        public void Reports_A_Route_Meter_The_Manifest_Does_Not_Declare()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute with { Meter = "documents" }] });

            Assert.Equal(["Meter 'documents' is not declared in the manifest's meters."], errors["routes[0].meter"]);
        }

        [Fact]
        public void Reports_A_Route_Quantity_Below_One()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute with { Quantity = 0 }] });

            Assert.Equal(["Quantity must be at least 1."], errors["routes[0].quantity"]);
        }

        [Fact]
        public void Reports_A_Route_By_Its_Index()
        {
            var errors = ManifestRules.Validate(ModuleId, Valid with { Routes = [ValidRoute, ValidRoute with { Quantity = 0 }] });

            Assert.Equal(["routes[1].quantity"], errors.Keys);
        }
    }
}
