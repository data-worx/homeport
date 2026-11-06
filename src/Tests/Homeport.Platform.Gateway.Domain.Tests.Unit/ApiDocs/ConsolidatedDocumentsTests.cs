// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Domain.ApiDocs;

namespace Homeport.Platform.Gateway.Domain.Tests.Unit.ApiDocs;

public sealed class ConsolidatedDocumentsTests
{
    private const string Prefix = "/modules/echo";

    private static readonly Uri FirstUrl = new("http://echo-1:8080/openapi/v1.json");
    private static readonly Uri SecondUrl = new("http://echo-2:8080/openapi/v1.json");

    private static readonly JsonObject Source = Parse("""
        {
          "openapi": "3.1.1",
          "info": { "title": "Echo", "version": "1.0.0" },
          "servers": [ { "url": "http://echo-1:8080" } ],
          "paths": {
            "/v1/echo": {
              "post": {
                "operationId": "Echo",
                "tags": [ "Echo" ],
                "security": [ { "internal": [] } ],
                "responses": { "200": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/EchoResponse" } } } } }
              }
            },
            "/v1/things/{id}": {
              "parameters": [ { "name": "id", "in": "path", "required": true } ],
              "get": { "operationId": "Thing" }
            },
            "/v1/hidden": {
              "get": { "operationId": "Hidden", "responses": { "200": { "content": { "application/json": { "schema": { "$ref": "#/components/schemas/Hidden" } } } } } }
            }
          },
          "components": {
            "schemas": {
              "EchoResponse": { "type": "object", "properties": { "body": { "$ref": "#/components/schemas/Body" } } },
              "Body": { "type": "string" },
              "Hidden": { "type": "integer" }
            },
            "securitySchemes": { "internal": { "type": "http", "scheme": "basic" } }
          },
          "tags": [ { "name": "Echo", "description": "Echoes" }, { "name": "Unused" } ]
        }
        """);

    private static readonly DocumentedRoute EchoRoute = new("POST", "/v1/echo", "1.0.0", FirstUrl);

    private static JsonObject Parse(string json)
    {
        return JsonNode.Parse(json)!.AsObject();
    }

    private static JsonObject Consolidate(IReadOnlyList<DocumentedRoute> routes, IReadOnlyDictionary<Uri, JsonObject> sources)
    {
        return ConsolidatedDocuments.ForModule("echo", Prefix, routes, sources);
    }

    private static JsonObject Consolidate(params DocumentedRoute[] routes)
    {
        return Consolidate(routes, new Dictionary<Uri, JsonObject> { [FirstUrl] = Source });
    }

    private static IEnumerable<string> Paths(JsonObject document)
    {
        return document["paths"]!.AsObject().Select(path => path.Key);
    }

    private static IEnumerable<string> Schemas(JsonObject document)
    {
        return document["components"]?["schemas"]?.AsObject().Select(schema => schema.Key) ?? [];
    }

    public sealed class ForModule
    {
        [Fact]
        public void Puts_Every_Path_Under_The_Public_Prefix()
        {
            var document = Consolidate(EchoRoute, new DocumentedRoute("GET", "/v1/things/{id}", "1.0.0", FirstUrl));

            Assert.Equal(["/modules/echo/v1/echo", "/modules/echo/v1/things/{id}"], Paths(document));
        }

        [Fact]
        public void Names_The_Gateway_As_The_Only_Server()
        {
            var document = Consolidate(EchoRoute);

            Assert.Equal("""[{"url":"/"}]""", document["servers"]!.ToJsonString());
        }

        [Fact]
        public void Leaves_Out_An_Operation_No_Route_Declares()
        {
            var document = Consolidate(EchoRoute);

            Assert.DoesNotContain("/modules/echo/v1/hidden", Paths(document));
            Assert.DoesNotContain("Hidden", Schemas(document));
        }

        [Fact]
        public void Leaves_Out_A_Method_No_Route_Declares_On_A_Declared_Path()
        {
            var document = Consolidate(EchoRoute with { Method = "PUT" });

            Assert.Empty(Paths(document));
        }

        [Fact]
        public void Matches_A_Route_Template_With_A_Constraint()
        {
            var document = Consolidate(new DocumentedRoute("GET", "/v1/things/{id:guid}", "1.0.0", FirstUrl));

            Assert.Equal(["/modules/echo/v1/things/{id}"], Paths(document));
        }

        [Fact]
        public void Keeps_The_Parameters_Of_The_Path()
        {
            var document = Consolidate(new DocumentedRoute("GET", "/v1/things/{id}", "1.0.0", FirstUrl));

            Assert.Equal("id", document["paths"]!["/modules/echo/v1/things/{id}"]!["parameters"]![0]!["name"]!.GetValue<string>());
        }

        [Fact]
        public void Takes_Along_The_Schemas_An_Operation_References_Transitively()
        {
            var document = Consolidate(EchoRoute);

            Assert.Equal(["Body", "EchoResponse"], Schemas(document).Order(StringComparer.Ordinal));
        }

        [Fact]
        public void Requires_A_Bearer_Token_Instead_Of_The_Module_Security()
        {
            var document = Consolidate(EchoRoute);

            Assert.Equal("""[{"bearer":[]}]""", document["security"]!.ToJsonString());
            Assert.Null(document["paths"]!["/modules/echo/v1/echo"]!["post"]!["security"]);
            Assert.Equal([ConsolidatedDocuments.SecuritySchemeName], document["components"]!["securitySchemes"]!.AsObject().Select(scheme => scheme.Key));
        }

        [Fact]
        public void Links_The_Bearer_Scheme_To_How_A_Token_Is_Obtained()
        {
            var document = Consolidate(EchoRoute);

            Assert.Contains(
                $"]({ConsolidatedDocuments.ObtainingATokenUrl})",
                document["components"]!["securitySchemes"]!["bearer"]!["description"]!.GetValue<string>(),
                StringComparison.Ordinal);
        }

        [Fact]
        public void Keeps_Only_The_Tags_The_Kept_Operations_Use()
        {
            var document = Consolidate(EchoRoute);

            Assert.Equal("""[{"name":"Echo","description":"Echoes"}]""", document["tags"]!.ToJsonString());
        }

        [Fact]
        public void Takes_Each_Route_From_The_Document_The_Route_Names()
        {
            var newer = Source.DeepClone().AsObject();
            newer["paths"]!["/v1/echo"]!["post"]!["operationId"] = "EchoV2";

            var document = Consolidate(
                [EchoRoute with { Version = "2.0.0", OpenApiUrl = SecondUrl }],
                new Dictionary<Uri, JsonObject> { [FirstUrl] = Source, [SecondUrl] = newer });

            Assert.Equal("EchoV2", document["paths"]!["/modules/echo/v1/echo"]!["post"]!["operationId"]!.GetValue<string>());
        }

        [Fact]
        public void Shares_A_Schema_Two_Documents_Define_Alike()
        {
            var newer = Source.DeepClone().AsObject();
            newer["paths"]!["/v2/echo"] = Source["paths"]!["/v1/echo"]!.DeepClone();

            var document = Consolidate(
                [EchoRoute, new DocumentedRoute("POST", "/v2/echo", "2.0.0", SecondUrl)],
                new Dictionary<Uri, JsonObject> { [FirstUrl] = Source, [SecondUrl] = newer });

            Assert.Equal(["Body", "EchoResponse"], Schemas(document).Order(StringComparer.Ordinal));
        }

        [Fact]
        public void Prefixes_A_Colliding_Schema_With_The_Version_And_Rewrites_Its_References()
        {
            var newer = Source.DeepClone().AsObject();
            newer["paths"]!.AsObject().Remove("/v1/echo");
            newer["paths"]!["/v2/echo"] = Source["paths"]!["/v1/echo"]!.DeepClone();
            newer["components"]!["schemas"]!["Body"] = Parse("""{ "type": "integer" }""");

            var document = Consolidate(
                [EchoRoute, new DocumentedRoute("POST", "/v2/echo", "2.0.0", SecondUrl)],
                new Dictionary<Uri, JsonObject> { [FirstUrl] = Source, [SecondUrl] = newer });

            var schemas = document["components"]!["schemas"]!;
            Assert.Equal(["Body", "EchoResponse", "v2.0.0_Body"], Schemas(document).Order(StringComparer.Ordinal));
            Assert.Equal("integer", schemas["v2.0.0_Body"]!["type"]!.GetValue<string>());
            Assert.Equal(
                "#/components/schemas/EchoResponse",
                document["paths"]!["/modules/echo/v2/echo"]!["post"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>());
        }

        [Fact]
        public void Leaves_Out_A_Route_Whose_Document_Is_Missing()
        {
            var document = Consolidate([EchoRoute with { OpenApiUrl = SecondUrl }], new Dictionary<Uri, JsonObject> { [FirstUrl] = Source });

            Assert.Empty(Paths(document));
        }

        [Fact]
        public void Names_The_Module_And_Its_Versions()
        {
            var document = Consolidate(
                [EchoRoute, new DocumentedRoute("GET", "/v1/things/{id}", "2.0.0", SecondUrl)],
                new Dictionary<Uri, JsonObject> { [FirstUrl] = Source });

            Assert.Equal("""{"title":"echo","version":"1.0.0, 2.0.0"}""", document["info"]!.ToJsonString());
        }
    }

    public sealed class ForPlatform
    {
        [Fact]
        public void Names_The_Platform_With_A_Bearer_Scheme_And_No_Paths()
        {
            var document = ConsolidatedDocuments.ForPlatform("0.1.0");

            Assert.Equal(ConsolidatedDocuments.PlatformTitle, document["info"]!["title"]!.GetValue<string>());
            Assert.Empty(Paths(document));
            Assert.Equal("""[{"bearer":[]}]""", document["security"]!.ToJsonString());
        }
    }
}
