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
| `Homeport.Aspire.AppHost` | The topology and its single source of truth: declares PostgreSQL, RabbitMQ, Valkey, Keycloak, the gateway and the Echo module, and generates `deploy/compose/` | `Homeport.Platform.Gateway.Api`, `Homeport.Modules.Echo.Api`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Shared.ServiceDefaults` | What every service sets up the same way: OpenTelemetry over OTLP, health endpoints, service discovery, resilient HTTP clients | nothing |
| `Homeport.Shared.Authentication` | Validates the tokens the gateway issues: issuer, audience, expiry and ES256 signature against the gateway's JWKS, and the JWKS wire format both sides share | nothing |
| `Homeport.Shared.Contracts` | The types several services, the SDK and a Domain share: so far the result type, a value or an error with a business code | nothing |
| `Homeport.Sdk` | What a .NET module needs to meet the module contract: the manifest, its validation, the manifest, OpenAPI and health endpoints, and error responses as Problem Details | `Homeport.Shared.ServiceDefaults`, `Homeport.Shared.Contracts` |
| `Homeport.Platform.Gateway.Domain` | Empty: the gateway decides nothing yet | nothing |
| `Homeport.Platform.Gateway.Application` | Empty: the gateway has no use case beyond forwarding | `Homeport.Platform.Gateway.Domain` |
| `Homeport.Platform.Gateway.Infrastructure` | Loads and checks the signing key, builds the JWKS and issues service tokens, which an HTTP client attaches to its calls to a platform service | `Homeport.Platform.Gateway.Application`, `Homeport.Shared.Authentication` |
| `Homeport.Platform.Gateway.Api` | The one entry point: forwards `/modules/{id}/{path}` with YARP on a static route to Echo, strips `X-Homeport-*` headers, propagates `traceparent` and serves the JWKS at `/.well-known/jwks.json` | `Homeport.Platform.Gateway.Infrastructure`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Modules.Echo.Domain` | Empty: Echo has no business rules | nothing |
| `Homeport.Modules.Echo.Application` | Empty: Echo has no use case beyond returning the request body | `Homeport.Modules.Echo.Domain` |
| `Homeport.Modules.Echo.Infrastructure` | Empty: Echo reaches nothing outside its process | `Homeport.Modules.Echo.Application` |
| `Homeport.Modules.Echo.Api` | The reference module: declares its manifest, serves `POST /v1/echo`, declares its problem types and shows the headers it received at `GET /v1/request` | `Homeport.Modules.Echo.Infrastructure`, `Homeport.Sdk` |
| `Architecture.Tests` | Asserts the layout, naming and reference rules of `dotnet.layout` as `AGENTS.md` narrows them, each also against a fixture tree that breaks it (solution folder `/Tests/`) | — |
| `Homeport.Aspire.AppHost.Tests.Integration` | Starts the AppHost, waits for every resource to become healthy, reads Echo's manifest, calls Echo through the gateway and reads the gateway's JWKS end to end; needs Docker | `Homeport.Aspire.AppHost` |
| `Homeport.Modules.Echo.Api.Tests.Integration` | Runs Echo in memory and asserts its manifest, health, echo and request endpoints | `Homeport.Modules.Echo.Api` |
| `Homeport.Platform.Gateway.Api.Tests.Integration` | Runs the gateway in memory against a stand-in module and asserts what it forwards, the JWKS it serves and that it does not start without a signing key | `Homeport.Platform.Gateway.Api` |
| `Homeport.Platform.Gateway.Infrastructure.Tests.Unit` | Asserts the checks on the signing configuration, the service tokens and their renewal | `Homeport.Platform.Gateway.Infrastructure` |
| `Homeport.Sdk.Tests.Unit` | Asserts the manifest validation, the module version and the problem type rules | `Homeport.Sdk` |
| `Homeport.Sdk.Tests.Integration` | Runs a module built with the SDK in memory and asserts its endpoints and error responses | `Homeport.Sdk` |
| `Homeport.Shared.Authentication.Tests.Unit` | Asserts which tokens the validation accepts and rejects, and that the JWKS carries public keys only | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Authentication.Tests.Integration` | Runs a service that accepts gateway tokens against a stand-in JWKS, including a key published after it was read | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Contracts.Tests.Unit` | Asserts the result type | `Homeport.Shared.Contracts` |
| `Homeport.Shared.ServiceDefaults.Tests.Integration` | Asserts the health endpoints outside Development | `Homeport.Shared.ServiceDefaults` |

The gateway is the only platform service so far. The target picture is in
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
  Platform/
    Gateway/
      Homeport.Platform.Gateway.Domain/
      Homeport.Platform.Gateway.Application/
      Homeport.Platform.Gateway.Infrastructure/
      Homeport.Platform.Gateway.Api/
  Sdk/
    Homeport.Sdk/
  Shared/
    Homeport.Shared.Authentication/
    Homeport.Shared.Contracts/
    Homeport.Shared.ServiceDefaults/
  Tests/
    Architecture.Tests/
    Homeport.Aspire.AppHost.Tests.Integration/
    Homeport.Modules.Echo.Api.Tests.Integration/
    Homeport.Platform.Gateway.Api.Tests.Integration/
    Homeport.Platform.Gateway.Infrastructure.Tests.Unit/
    Homeport.Sdk.Tests.Integration/
    Homeport.Sdk.Tests.Unit/
    Homeport.Shared.Authentication.Tests.Integration/
    Homeport.Shared.Authentication.Tests.Unit/
    Homeport.Shared.Contracts.Tests.Unit/
    Homeport.Shared.ServiceDefaults.Tests.Integration/
```

## Why it is cut this way

- Services and modules are separate deployables, so each is a stack of its
  own in the sense of `dotnet.layout`, grouped under `src/Platform/` and
  `src/Modules/`
  ([ADR-0003](adr/0003-service-granularity-and-module-contract.md)). A
  service without business rules stays a single project, which keeps the
  infrastructure-only services (gateway, registry) small.
- A business module is cut from the start, even when a layer is still empty,
  as in Echo. The cut is the shape every module takes, and Echo is the
  module others copy.
- The SDK depends on the service defaults, so a module built with it gets
  the same telemetry and health endpoints as a platform service without
  wiring them itself.
- Validating gateway tokens lives in `src/Shared/`, not in the gateway: the
  platform services and, through the SDK, the modules validate the same way
  against the same constants, while only the gateway holds a signing key.
- The types a Domain shares with the SDK and the other services live in
  `Homeport.Shared.Contracts`: it references nothing beyond the BCL, so every
  project may reference it, a Domain included, and it ships with the SDK
  because the SDK references it.
- Only a host references the SDK. A Domain, Application or Infrastructure
  reaches what it needs through `Homeport.Shared.Contracts` or an interface,
  so the inner layers stay free of the module contract's wiring.
