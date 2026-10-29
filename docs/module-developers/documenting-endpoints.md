# Documenting endpoints

A module serves an OpenAPI document of its endpoints at `/openapi/v1.json`.
Callers read it to learn what each endpoint does, and generate clients from
it. A module built with `Homeport.Sdk` writes it from the code: the XML docs
and attributes of its controllers, plus the errors every module endpoint can
answer with. The Echo module in `src/Modules/Echo/` is the reference.

## Set it up

The module's API project switches on its XML docs and references
`Microsoft.AspNetCore.OpenApi` directly:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
</ItemGroup>
```

and calls `AddOpenApi` itself, next to `AddHomeportModule`:

```csharp
builder.AddHomeportModule(manifest);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
```

Both are needed because of how ASP.NET reads XML docs: a source generator
intercepts the `AddOpenApi` call and takes in the XML docs of the project it
is called from. The direct package reference switches that generator on. A
call inside the SDK would only see the SDK's own docs. `CS1591` stays off,
so a public member without an XML doc does not fail the build; the rule
covers endpoints, not every type.

## Document an action

```csharp
/// <summary>Returns the request body unchanged.</summary>
/// <remarks>The response carries the content type of the request.</remarks>
/// <response code="200">The request body.</response>
/// <response code="400">The request has no body.</response>
[HttpPost(EchoManifest.EchoPath, Name = EchoOperation)]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
public IActionResult Echo() { … }
```

| Source | Becomes |
|---|---|
| `<summary>` | The operation's `summary`: one line on what it does |
| `<remarks>` | The operation's `description` |
| `<param>` | The description of a parameter |
| `<response code="…">` | The description of a response |
| `Name =` on the route attribute | The `operationId` |
| `[ProducesResponseType]` | A response with its status, body type and media type |

- **Every action names its `operationId`.** A client generator turns it into
  a method name, so it stays stable when the action is renamed. Declare it
  as a constant, such as `getRequest`.
- **Every status the action answers with is declared** with
  `[ProducesResponseType]`. An error declares `ProblemDetails` with
  `MediaTypeNames.Application.ProblemJson`; see
  [answering errors](answering-errors.md).
- Write `<summary>` and `<response>` as plain text. A `<see cref="…"/>`
  reaches the document as the type's name, which means nothing to a caller.

## What the SDK adds

Every operation lists the errors any module endpoint can answer with, as
Problem Details with `application/problem+json`:

| Status | Means |
|---|---|
| `401` | The request carries no valid token |
| `403` | The token lacks the scope the operation requires |
| `429` | A rate limit or a quota of the tenant is exhausted |
| `500` | The request failed for a reason the caller cannot influence |

A status the action declares itself is kept as the action describes it.

## Check it in a test

`ModuleOpenApi.Validate` checks the document for what a caller needs from
every operation: a `summary`, an `operationId` and a success response. It
returns one line per problem, so a test asserts that there is none:

```csharp
[Fact]
public async Task Documents_Every_Operation_For_A_Caller()
{
    var document = await module.CreateClient().GetStringAsync(ModuleManifest.OpenApiPath);

    Assert.Empty(ModuleOpenApi.Validate(JsonDocument.Parse(document).RootElement));
}
```

Every module has this test, so an undocumented endpoint fails the build
before it reaches a caller. A failing run lists the problems, such as
`GET /v1/request has no operationId.`
