# Answering errors with the SDK

The [module contract](../reference/module-contract/README.md#errors) requires
a module to answer every 4xx and 5xx as Problem Details (RFC 9457). A module
built with `Homeport.Sdk` does so by default; this page shows what the SDK
does on its own and how a module answers with errors of its own. The Echo
module in `src/Modules/Echo/` is the reference.

## What the SDK does on its own

`AddHomeportModule` and `MapHomeportModule` (see
[declaring a manifest](declaring-a-manifest.md)) set up the error handling.
Call `MapHomeportModule` before `MapControllers`, so it sits in front of the
module's endpoints.

| Situation | Answer |
|---|---|
| An action throws | `500`, [`internal`](../reference/problems/internal.md), nothing of the exception; the exception is logged once at Error |
| A request fails model validation | `400`, [`validation`](../reference/problems/validation.md), `errors` keyed by camelCase field path |
| An error status without a body, such as `404` for an unknown path | Problem Details with ASP.NET's default type for the status |

Every one of them, and every problem a module answers with, carries the
extension `traceId`, the W3C trace id of the request, and the media type
`application/problem+json`.

Validation is ASP.NET's model validation of controllers with
`[ApiController]`: data annotations such as `[Required]` on the request
record, and a body that cannot be read as JSON. Field paths are the C# names
converted to camelCase, which matches the request's JSON as long as the
module keeps ASP.NET's default JSON naming; `[JsonPropertyName]` is not
honoured.

## Declare the module's problem types

A module declares its own problem types in one place, under a URI prefix of
its own:

```csharp
internal static class ContractsProblems
{
    private const string TypePrefix = "https://contracts.example/problems/";

    internal static readonly ProblemType NotFound = new(
        new Uri(TypePrefix + "not-found"),
        "The contract does not exist.",
        StatusCodes.Status404NotFound);
}
```

A `ProblemType` is an absolute URI, an English title that is the same for
every occurrence, and a 4xx or 5xx status; the constructor refuses anything
else. Once a type is published, it is part of the module's public API.

## Answer with a problem type

A controller answers with a type and an English detail for this occurrence:

```csharp
[HttpPost("/v1/echo")]
public IActionResult Echo()
{
    if (/* the request has no body */)
    {
        return EchoProblems.EmptyBody.ToResult("POST a body; Echo returns it unchanged.");
    }
    …
}
```

## Map a business error

A business error is a result value, not an exception: the use case returns a
`Result<T>` from `Homeport.Shared.Contracts` with an `Error` that carries a
business code. The domain knows no HTTP status, so the module maps the code
onto one of its problem types, next to the types:

```csharp
internal static ProblemType For(Error error)
{
    return error.Code switch
    {
        "contract.not-found" => NotFound,
        _ => HomeportProblems.Internal,
    };
}
```

The controller hands the result to the SDK, which answers `200` with the value
or the mapped problem with the error's message as `detail`:

```csharp
[HttpGet("/v1/contracts/{id}")]
public async Task<ActionResult<Contract>> Get(Guid id, CancellationToken cancellationToken)
{
    var result = await contracts.FindAsync(id, cancellationToken);
    return result.ToActionResult(ContractsProblems.For);
}
```

## Check it

Run Echo through the AppHost and `POST /v1/echo` without a body. The answer:

```json
{
  "type": "https://homeport.data-worx.de/modules/echo/problems/empty-body",
  "title": "The request has no body to echo.",
  "status": 400,
  "detail": "POST a body; Echo returns it unchanged.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```
