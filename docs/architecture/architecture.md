# Architecture

The projects that exist in homeport today, how they are laid out, and
why they are cut this way. This document describes the current state. The
binding rules — the dependency direction, who may reference what — live in the
*Architecture* section of [`AGENTS.md`](../../AGENTS.md#architecture).

## Projects

<!-- One row per project. "May depend on" names the projects it references,
     or "nothing" for the centre. A project that is not in this table does not
     exist yet. -->

| Project | Role | May depend on |
|---|---|---|
| `Homeport.Aspire.AppHost` | The topology and its single source of truth: declares PostgreSQL, RabbitMQ, Valkey, Keycloak and the Echo module, and generates `deploy/compose/` | `Homeport.Modules.Echo.Api` |
| `Homeport.Shared.ServiceDefaults` | What every service sets up the same way: OpenTelemetry over OTLP, health endpoints, service discovery, resilient HTTP clients | nothing |
| `Homeport.Sdk` | What a .NET module needs to meet the module contract: the manifest, its validation, and the manifest, OpenAPI and health endpoints | `Homeport.Shared.ServiceDefaults` |
| `Homeport.Modules.Echo.Domain` | Empty: Echo has no business rules | nothing |
| `Homeport.Modules.Echo.Application` | Empty: Echo has no use case beyond returning the request body | `Homeport.Modules.Echo.Domain` |
| `Homeport.Modules.Echo.Infrastructure` | Empty: Echo reaches nothing outside its process | `Homeport.Modules.Echo.Application` |
| `Homeport.Modules.Echo.Api` | The reference module: declares its manifest and serves `POST /v1/echo` | `Homeport.Modules.Echo.Infrastructure`, `Homeport.Sdk` |
| `Architecture.Tests` | Asserts the per-service layout, naming and reference rules of `AGENTS.md`, each also against a fixture tree that breaks it (solution folder `/Tests/`) | — |
| `Homeport.Aspire.AppHost.Tests.Integration` | Starts the AppHost, waits for every resource to become healthy and reads Echo's manifest; needs Docker | `Homeport.Aspire.AppHost` |
| `Homeport.Modules.Echo.Api.Tests.Integration` | Runs Echo in memory and asserts its manifest, health and echo endpoints | `Homeport.Modules.Echo.Api` |
| `Homeport.Sdk.Tests.Unit` | Asserts the manifest validation and the module version | `Homeport.Sdk` |
| `Homeport.Sdk.Tests.Integration` | Runs a module built with the SDK in memory and asserts its endpoints | `Homeport.Sdk` |
| `Homeport.Shared.ServiceDefaults.Tests.Integration` | Asserts the health endpoints outside Development | `Homeport.Shared.ServiceDefaults` |

No platform service exists yet. The target picture is in
[vision.md](vision.md); the binding layout is in
[`AGENTS.md`](../../AGENTS.md#architecture).

## Directory layout

```text
Homeport.slnx
Directory.Packages.props
deploy/
  compose/                generated from the AppHost, never edited
src/
  Directory.Build.props
  Aspire/
    Homeport.Aspire.AppHost/
  Modules/
    Echo/
      Homeport.Modules.Echo.Domain/
      Homeport.Modules.Echo.Application/
      Homeport.Modules.Echo.Infrastructure/
      Homeport.Modules.Echo.Api/
  Sdk/
    Homeport.Sdk/
  Shared/
    Homeport.Shared.ServiceDefaults/
  Tests/
    Architecture.Tests/
    Homeport.Aspire.AppHost.Tests.Integration/
    Homeport.Modules.Echo.Api.Tests.Integration/
    Homeport.Sdk.Tests.Integration/
    Homeport.Sdk.Tests.Unit/
    Homeport.Shared.ServiceDefaults.Tests.Integration/
```

## Why it is cut this way

- Services and modules are separate deployables, so the layer rule of
  `dotnet.layout` is applied per service instead of to one application
  ([ADR-0003](adr/0003-service-granularity-and-module-contract.md)). A
  service without business rules stays a single project, which keeps the
  infrastructure-only services (gateway, registry) small.
- A business module is cut from the start, even when a layer is still empty,
  as in Echo. The cut is the shape every module takes, and Echo is the
  module others copy.
- The SDK depends on the service defaults, so a module built with it gets
  the same telemetry and health endpoints as a platform service without
  wiring them itself.
