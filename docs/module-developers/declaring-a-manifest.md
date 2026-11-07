# Declaring a manifest with the SDK

Every module tells Homeport what it is at
`GET /.well-known/homeport/manifest`: its id, version, routes, scopes and
meters. The [module contract](../reference/module-contract/README.md) defines
the fields. A module written in .NET declares its manifest in code, and the
SDK, `Homeport.Sdk`, serves it together with the other endpoints the contract
requires. The Echo module in `src/Modules/Echo/` is the reference.

The SDK is not published as a NuGet package yet; reference the project
`src/Sdk/Homeport.Sdk` directly.

`ModuleManifest` and `ManifestRoute` are types of the module contract, so
they live in `Homeport.Shared.Client`, namespace
`Homeport.Shared.Client.ModuleContract`, next to the wire types callers use.
The SDK references that project, so a module that references the SDK has
them.

## Declare the manifest

```csharp
var manifest = new ModuleManifest(
    Id: "contracts",
    Name: "Contracts",
    Version: ModuleVersion.Of(typeof(Program).Assembly),
    Routes:
    [
        new ManifestRoute("GET", "/v1/contracts", Scope: "contracts.read", Meter: "requests", Quantity: 1),
        new ManifestRoute("POST", "/v1/contracts/scan", Scope: "contracts.write", Meter: "documents", Quantity: 1),
    ],
    Scopes: ["contracts.read", "contracts.write"],
    Meters: ["requests", "documents"]);
```

- **`Id`** is the module's stable name. It appears in the URL callers use,
  `/modules/{id}/…`, in metrics and in `service.name`. Lower-case letters,
  digits and `-`, starting with a letter; `platform` is reserved.
- **`Version`** is the module's own SemVer version. `ModuleVersion.Of` reads
  it from the assembly, so it follows the build's `Version` and carries the
  commit as build metadata, for example `0.1.0+c1fef1c…`.
- **`Routes`** lists every endpoint the module serves. A path starts with
  `/v{major}/`. The scope is what a caller needs, the meter and quantity are
  what one request counts.
- **`Scopes`** and **`Meters`** list every scope and meter the routes use.

`contractVersion` and `openApiUrl` are not declared: the SDK sets them to
the contract version it implements and to the OpenAPI document it serves.

## Serve it

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddHomeportModule(manifest);
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();
app.MapHomeportModule();
app.MapControllers();

app.Run();
```

The module's own endpoints are controllers, one action per route in the
manifest:

```csharp
[ApiController]
public sealed class ContractsController : ControllerBase
{
    [HttpGet("/v1/contracts")]
    public IActionResult List() { … }
}
```

`AddHomeportModule` adds the Homeport service defaults — OpenTelemetry,
health checks and service discovery — and registers the module at the
registry once it has started; see [registering a module](registering-a-module.md).
The registry checks the manifest against the contract. When it breaks it, the
module logs every problem, for example a route whose scope is not in
`Scopes`, and stops.

`MapHomeportModule` maps, all of them anonymous — every other endpoint needs
the gateway's module token; see [the module token](module-token.md):

| Endpoint | Serves |
|---|---|
| `GET /.well-known/homeport/manifest` | The manifest |
| `GET /openapi/v1.json` | The OpenAPI document of the module's endpoints |
| `GET /health/live` | Whether the process is alive |
| `GET /health/ready` | Whether the module can take traffic |

Together they also answer every error as Problem Details; see
[answering errors](answering-errors.md). `AddOpenApi` is the module's own
call, so the XML docs of its controllers reach the OpenAPI document; see
[documenting endpoints](documenting-endpoints.md).

## Check it

Run the module and read the manifest back. For Echo, through the AppHost:
open the dashboard, follow the endpoint of `echo` and append
`/.well-known/homeport/manifest`. The response looks like this:

```json
{
  "id": "echo",
  "name": "Echo",
  "version": "0.1.0+c1fef1c…",
  "routes": [
    { "method": "POST", "path": "/v1/echo", "scope": "echo.invoke", "meter": "requests", "quantity": 1 },
    { "method": "GET", "path": "/v1/request", "scope": "echo.invoke", "meter": "requests", "quantity": 1 },
    { "method": "GET", "path": "/v1/caller", "scope": "echo.invoke", "meter": "requests", "quantity": 1 }
  ],
  "scopes": ["echo.invoke"],
  "meters": ["requests"],
  "contractVersion": 1,
  "openApiUrl": "/openapi/v1.json"
}
```

A module is reachable only inside the AppHost or compose network. Callers
reach it through the gateway under `/modules/{id}/{path}`, on the routes it
registered; see [registering a module](registering-a-module.md). The gateway
lets a request through only when the caller's token grants the route's
`scope`.
