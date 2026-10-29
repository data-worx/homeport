// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace Homeport.Sdk.Tests.Unit;

public sealed class ModuleOpenApiTests
{
    public sealed class Validate
    {
        [Fact]
        public void Returns_Nothing_For_A_Documented_Operation()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things":{"post":{"summary":"Creates a thing.","operationId":"createThing","responses":{"201":{}}}}}}
                """));

            Assert.Empty(problems);
        }

        [Fact]
        public void Reports_An_Operation_Without_A_Summary()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things":{"post":{"operationId":"createThing","responses":{"201":{}}}}}}
                """));

            Assert.Equal(["POST /v1/things has no summary."], problems);
        }

        [Fact]
        public void Reports_An_Operation_With_An_Empty_Summary()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things":{"post":{"summary":" ","operationId":"createThing","responses":{"201":{}}}}}}
                """));

            Assert.Equal(["POST /v1/things has no summary."], problems);
        }

        [Fact]
        public void Reports_An_Operation_Without_An_Operation_Id()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things":{"get":{"summary":"Lists things.","responses":{"200":{}}}}}}
                """));

            Assert.Equal(["GET /v1/things has no operationId."], problems);
        }

        [Fact]
        public void Reports_An_Operation_Without_A_Success_Response()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things":{"get":{"summary":"Lists things.","operationId":"listThings","responses":{"500":{}}}}}}
                """));

            Assert.Equal(["GET /v1/things has no success response."], problems);
        }

        [Fact]
        public void Ignores_The_Keys_Of_A_Path_That_Are_No_Operation()
        {
            var problems = ModuleOpenApi.Validate(Parse("""
                {"paths":{"/v1/things/{id}":{"parameters":[],"summary":"One thing."}}}
                """));

            Assert.Empty(problems);
        }

        [Fact]
        public void Reports_A_Document_Without_Paths()
        {
            var problems = ModuleOpenApi.Validate(Parse("""{"openapi":"3.1.1"}"""));

            Assert.Equal(["The document has no paths."], problems);
        }

        private static JsonElement Parse(string json)
        {
            return JsonDocument.Parse(json).RootElement;
        }
    }
}
