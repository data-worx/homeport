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
| `Homeport.Aspire.AppHost` | The topology and its single source of truth: declares PostgreSQL, RabbitMQ, Valkey, Keycloak, the gateway, the registry and the Echo module with its registration key, and generates `deploy/compose/` | `Homeport.Platform.Gateway.Api`, `Homeport.Platform.Registry.Api`, `Homeport.Modules.Echo.Api`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Shared.ServiceDefaults` | What every service sets up the same way: OpenTelemetry over OTLP, health endpoints, service discovery, resilient HTTP clients | nothing |
| `Homeport.Shared.Authentication` | Validates the tokens the gateway issues: issuer, audience, expiry and ES256 signature against the gateway's JWKS, and the JWKS wire format both sides share | nothing |
| `Homeport.Shared.Contracts` | The types several services, the SDK and a Domain share: so far the result type, a value or an error with a business code | nothing |
| `Homeport.Shared.Client` | The wire types Homeport ships: the manifest and the registration of the module contract, the read model and constants of the error format, and the response of Echo's `GET /v1/request` | nothing |
| `Homeport.Shared.Internal` | What services share and Homeport never ships: the route table the registry serves the gateway | nothing |
| `Homeport.Sdk` | What a .NET module needs to meet the module contract: the manifest, OpenAPI and health endpoints, registration and heartbeats at the registry, error responses as Problem Details, the common errors on every OpenAPI operation and a check of the OpenAPI document | `Homeport.Shared.Client`, `Homeport.Shared.ServiceDefaults`, `Homeport.Shared.Contracts` |
| `Homeport.Platform.Gateway.Domain` | Empty: the gateway decides nothing yet | nothing |
| `Homeport.Platform.Gateway.Application` | Empty: the gateway has no use case beyond forwarding | `Homeport.Platform.Gateway.Domain` |
| `Homeport.Platform.Gateway.Infrastructure` | Loads and checks the signing key, builds the JWKS and issues service tokens, which an HTTP client attaches to its calls to a platform service; reads the route table from the registry | `Homeport.Platform.Gateway.Application`, `Homeport.Shared.Authentication`, `Homeport.Shared.Internal` |
| `Homeport.Platform.Gateway.Api` | The one entry point: polls the registry for the routes and forwards `/modules/{id}/{path}` with YARP on them, strips `X-Homeport-*` headers, propagates `traceparent`, holds every error response to Problem Details and serves the JWKS at `/.well-known/jwks.json` | `Homeport.Platform.Gateway.Infrastructure`, `Homeport.Shared.Client`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Platform.Registry.Domain` | The rules of registration: the manifest against the module contract, the supported contract versions, SemVer precedence, merging the routes of the live instances and comparing registration keys | nothing |
| `Homeport.Platform.Registry.Application` | Registering an instance, renewing it and building the routes, on a store of instances and a source of keys it declares | `Homeport.Platform.Registry.Domain` |
| `Homeport.Platform.Registry.Infrastructure` | Keeps the instances in Valkey with a time to live and reads the registration keys from configuration | `Homeport.Platform.Registry.Application` |
| `Homeport.Platform.Registry.Api` | Serves registration and heartbeat, authenticated by registration key, and the route table, authenticated by a gateway service token | `Homeport.Platform.Registry.Infrastructure`, `Homeport.Shared.Authentication`, `Homeport.Shared.Client`, `Homeport.Shared.Internal`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Modules.Echo.Domain` | Empty: Echo has no business rules | nothing |
| `Homeport.Modules.Echo.Application` | Empty: Echo has no use case beyond returning the request body | `Homeport.Modules.Echo.Domain` |
| `Homeport.Modules.Echo.Infrastructure` | Empty: Echo reaches nothing outside its process | `Homeport.Modules.Echo.Application` |
| `Homeport.Modules.Echo.Api` | The reference module: declares its manifest, serves `POST /v1/echo`, declares its problem types and shows the headers it received at `GET /v1/request` | `Homeport.Modules.Echo.Infrastructure`, `Homeport.Sdk`, `Homeport.Shared.Client` |
| `Architecture.Tests` | Asserts the layout, naming and reference rules of `dotnet.layout` as `AGENTS.md` narrows them, each also against a fixture tree that breaks it (solution folder `/Tests/`) | — |
| `Homeport.Aspire.AppHost.Tests.Integration` | Starts the AppHost, waits for every resource to become healthy, reads Echo's manifest, calls Echo through the gateway once it has registered, reads the gateway's JWKS, and stops Echo or the registry to assert the routes end to end; needs Docker | `Homeport.Aspire.AppHost` |
| `Homeport.Modules.Echo.Api.Tests.Integration` | Runs Echo in memory and asserts its manifest, health, echo and request endpoints and its OpenAPI document, including the descriptions of its `Homeport.Shared.Client` types | `Homeport.Modules.Echo.Api` |
| `Homeport.Platform.Gateway.Api.Tests.Integration` | Runs the gateway in memory against a stand-in module and a stand-in registry and asserts what it forwards on which routes, how it polls the registry, the error responses it answers with, the JWKS it serves, that its errors read as `ProblemResponse` without loss, and that it does not start without a signing key | `Homeport.Platform.Gateway.Api` |
| `Homeport.Platform.Gateway.Infrastructure.Tests.Unit` | Asserts the checks on the signing configuration, the service tokens and their renewal | `Homeport.Platform.Gateway.Infrastructure` |
| `Homeport.Platform.Registry.Domain.Tests.Unit` | Asserts the manifest rules, SemVer precedence, how the routes of several instances merge and the key comparison | `Homeport.Platform.Registry.Domain` |
| `Homeport.Platform.Registry.Application.Tests.Unit` | Asserts registering, renewing and building the routes on an in-memory store | `Homeport.Platform.Registry.Application` |
| `Homeport.Platform.Registry.Infrastructure.Tests.Unit` | Asserts the checks on the configured registration keys | `Homeport.Platform.Registry.Infrastructure` |
| `Homeport.Platform.Registry.Infrastructure.Tests.Integration` | Asserts keeping, renewing and expiring instances in a throwaway Valkey; needs Docker | `Homeport.Platform.Registry.Infrastructure` |
| `Homeport.Platform.Registry.Api.Tests.Integration` | Runs the registry in memory against a throwaway Valkey and asserts registration, heartbeat and the route table with their keys, tokens and error responses; needs Docker | `Homeport.Platform.Registry.Api` |
| `Homeport.Sdk.Tests.Unit` | Asserts the registration settings, the module version, the problem type rules and the OpenAPI document check | `Homeport.Sdk` |
| `Homeport.Sdk.Tests.Integration` | Runs a module built with the SDK in memory and asserts its endpoints, error responses, that they read as `ProblemResponse` without loss, the errors its OpenAPI document lists, and its registration and heartbeats against a stand-in registry | `Homeport.Sdk` |
| `Homeport.Shared.Authentication.Tests.Unit` | Asserts which tokens the validation accepts and rejects, and that the JWKS carries public keys only | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Authentication.Tests.Integration` | Runs a service that accepts gateway tokens against a stand-in JWKS, including a key published after it was read | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Contracts.Tests.Unit` | Asserts the result type | `Homeport.Shared.Contracts` |
| `Homeport.Shared.ServiceDefaults.Tests.Integration` | Asserts the health endpoints outside Development | `Homeport.Shared.ServiceDefaults` |

The gateway and the registry are the only platform services so far. The target picture is in
[vision.md](vision.md); the binding layout is in
[`AGENTS.md`](../../AGENTS.md#architecture).

## Directory layout

```text
Homeport.slnx
Directory.Build.targets   adds the public API analyzer where a project opts in
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
    Registry/
      Homeport.Platform.Registry.Domain/
      Homeport.Platform.Registry.Application/
      Homeport.Platform.Registry.Infrastructure/
      Homeport.Platform.Registry.Api/
  Sdk/
    Homeport.Sdk/
  Shared/
    Homeport.Shared.Authentication/
    Homeport.Shared.Client/
    Homeport.Shared.Contracts/
    Homeport.Shared.Internal/
    Homeport.Shared.ServiceDefaults/
  Tests/
    Architecture.Tests/
    Homeport.Aspire.AppHost.Tests.Integration/
    Homeport.Modules.Echo.Api.Tests.Integration/
    Homeport.Platform.Gateway.Api.Tests.Integration/
    Homeport.Platform.Gateway.Infrastructure.Tests.Unit/
    Homeport.Platform.Registry.Api.Tests.Integration/
    Homeport.Platform.Registry.Application.Tests.Unit/
    Homeport.Platform.Registry.Domain.Tests.Unit/
    Homeport.Platform.Registry.Infrastructure.Tests.Integration/
    Homeport.Platform.Registry.Infrastructure.Tests.Unit/
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
  service without business rules may stay a single project. The registry
  has rules of its own — checking a manifest, merging the manifests of the
  live instances — so it is cut into layers; the gateway is cut ahead of
  the rules throttling brings.
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
- The route table the registry serves the gateway lives in
  `Homeport.Shared.Internal`: both services need it, no caller outside
  Homeport ever does, so it is never shipped.
- The registry's domain is the single source of the manifest rules. A
  module learns about a broken manifest from the registry's answer, so the
  rules cannot drift between the SDK and the registry, and a module in
  another language is checked the same way.
- The wire types callers and modules both need live in
  `Homeport.Shared.Client`, which references nothing, so a caller takes them
  without taking ASP.NET or the SDK
  ([ADR-0018](adr/0018-where-wire-types-live.md)). The SDK, every Shared
  project it references and `Homeport.Shared.Client` list their public API in
  `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`, so every change to
  what Homeport ships is a line in a pull request.
- Only a host references the SDK. A Domain, Application or Infrastructure
  reaches what it needs through `Homeport.Shared.Contracts` or an interface,
  so the inner layers stay free of the module contract's wiring.
