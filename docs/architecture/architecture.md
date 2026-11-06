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
| `Homeport.Aspire.AppHost` | The topology and its single source of truth: declares PostgreSQL with its init script, RabbitMQ with its init script and a user per service, Valkey, Keycloak, the gateway as two replicas, the registry, the control plane and metering with their `migrate` steps and the Echo module with its registration key, imports the Keycloak realm with generated client secrets, links the gateway's API docs in the dashboard, and generates `deploy/compose/` | `Homeport.Platform.Gateway.Api`, `Homeport.Platform.Registry.Api`, `Homeport.Platform.ControlPlane`, `Homeport.Platform.Metering.Api`, `Homeport.Modules.Echo.Api`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Shared.ServiceDefaults` | What every service sets up the same way: OpenTelemetry over OTLP, including the service's own meter and Wolverine's traces and metrics, health endpoints, service discovery, resilient HTTP clients | nothing |
| `Homeport.Shared.Authentication` | Validates the tokens the gateway issues: issuer, audience, expiry and ES256 signature against the gateway's JWKS, and the JWKS wire format and the module token's claim names both sides share; ships with the SDK | nothing |
| `Homeport.Shared.Contracts` | The types several services, the SDK and a Domain share: so far the result type, a value or an error with a business code | nothing |
| `Homeport.Shared.Client` | The wire types Homeport ships: the manifest and the registration of the module contract, the header a module reports its usage in, the read model and constants of the error format, Echo's batch request and response, and the responses of Echo's `GET /v1/request` and `GET /v1/caller` | nothing |
| `Homeport.Shared.Internal` | What services share and Homeport never ships: the route table the registry serves the gateway, the client lookup of the control plane, and `UsageRecorded` with the exchange and queue it travels through | nothing |
| `Homeport.Sdk` | What a .NET module needs to meet the module contract: the manifest, OpenAPI and health endpoints, registration and heartbeats at the registry, the module token on every other endpoint with a policy per scope and the caller it names, error responses as Problem Details, reporting how much a request counts, the common errors on every OpenAPI operation and a check of the OpenAPI document | `Homeport.Shared.Authentication`, `Homeport.Shared.Client`, `Homeport.Shared.ServiceDefaults`, `Homeport.Shared.Contracts` |
| `Homeport.Platform.Gateway.Domain` | The rules for a quantity a module reports: one pair of the route's own meter and a positive integer; a plan's rate limit and what a request's draw from its bucket tells the caller: allowed or when to retry, the quota, window, remaining requests and seconds until full; the consolidated API documents: per module only the routes the gateway serves, each from the document of the instance that wins it, under public paths, with prefixed colliding component names and a bearer scheme | `Homeport.Shared.Contracts` |
| `Homeport.Platform.Gateway.Application` | Decides whether a tenant's request passes its rate limit, on the token buckets it declares, and lets it pass while they cannot be reached | `Homeport.Platform.Gateway.Domain` |
| `Homeport.Platform.Gateway.Infrastructure` | Loads and checks the signing key, builds the JWKS and issues service tokens, which an HTTP client attaches to its calls to a platform service, and module tokens; reads the route table from the registry; looks clients up at the control plane and keeps each answer for a configurable time; publishes usage events through Wolverine to RabbitMQ, retries a failed send and counts one it drops or a reported quantity it rejects; reads the OpenAPI documents of module instances; keeps every tenant's token bucket in Valkey and draws from it with a script that runs inside Valkey, counts a request that passes while Valkey cannot be reached and warns about it at most once a minute | `Homeport.Platform.Gateway.Application`, `Homeport.Shared.Authentication`, `Homeport.Shared.Internal` |
| `Homeport.Platform.Gateway.Api` | The one entry point: polls the registry for the routes and forwards `/modules/{id}/{path}` with YARP on them to clients with a valid token of the operator's identity provider that the control plane knows and that grants the route's scope, replaces the client's token with a module token, strips `X-Homeport-*` headers, applies forwarded headers only from trusted proxies, propagates `traceparent`, rejects a request over its tenant's rate limit with `429` and `Retry-After` before it is forwarded or metered and sets the RateLimit headers on every module response, publishes a usage event for every request it forwards with the quantity the module reported in `X-Homeport-Usage`, which it removes from the response, or the route's, holds every error response to Problem Details and serves the JWKS at `/.well-known/jwks.json`; when `Gateway:Docs:Enabled`, rebuilds the consolidated API docs whenever the routes change and serves them with Scalar at `/platform/docs` | `Homeport.Platform.Gateway.Infrastructure`, `Homeport.Shared.Client`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Platform.Registry.Domain` | The rules of registration: the manifest against the module contract, the supported contract versions, SemVer precedence, merging the routes of the live instances and comparing registration keys | nothing |
| `Homeport.Platform.Registry.Application` | Registering an instance, renewing it and building the routes, on a store of instances and a source of keys it declares | `Homeport.Platform.Registry.Domain` |
| `Homeport.Platform.Registry.Infrastructure` | Keeps the instances in Valkey with a time to live and reads the registration keys from configuration | `Homeport.Platform.Registry.Application` |
| `Homeport.Platform.Registry.Api` | Serves registration and heartbeat, authenticated by registration key, and the route table, authenticated by a gateway service token | `Homeport.Platform.Registry.Infrastructure`, `Homeport.Shared.Authentication`, `Homeport.Shared.Client`, `Homeport.Shared.Internal`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Platform.ControlPlane` | One project, as it carries no business rules yet: keeps tenants, plans and clients in the schema `control_plane` with EF Core, serves `GET /clients/{clientId}` authenticated by a gateway service token, and as `migrate` applies its migrations and seeds one tenant, plan and client | `Homeport.Shared.Authentication`, `Homeport.Shared.Client`, `Homeport.Shared.Internal`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Platform.Metering.Domain` | The usage event, its hourly and daily aggregates, and the rule that buckets usage by the UTC hour and day | nothing |
| `Homeport.Platform.Metering.Application` | Recording an event once and adding it to its aggregates, on a store it declares | `Homeport.Platform.Metering.Domain` |
| `Homeport.Platform.Metering.Infrastructure` | Keeps the events and aggregates in the schema `metering` with EF Core, filtered to one tenant by query filters and row-level security, records an event in one transaction that a duplicate leaves unchanged, and holds the migrations | `Homeport.Platform.Metering.Application` |
| `Homeport.Platform.Metering.Api` | Consumes `UsageRecorded` from RabbitMQ through Wolverine, acknowledging it only once recorded, counts consumed events in `homeport.metering.events`, and as `migrate` applies its migrations | `Homeport.Platform.Metering.Infrastructure`, `Homeport.Shared.Internal`, `Homeport.Shared.ServiceDefaults` |
| `Homeport.Modules.Echo.Domain` | The rule how much a batch counts: one per item, and at least one item | `Homeport.Shared.Contracts` |
| `Homeport.Modules.Echo.Application` | Returning a batch with the quantity it counts as | `Homeport.Modules.Echo.Domain` |
| `Homeport.Modules.Echo.Infrastructure` | Empty: Echo reaches nothing outside its process | `Homeport.Modules.Echo.Application` |
| `Homeport.Modules.Echo.Api` | The reference module: declares its manifest, serves `POST /v1/echo` and `POST /v1/echo/batch`, which reports one in the meter `items` per item, to callers with `echo.invoke`, declares its problem types, shows the headers it received at `GET /v1/request` and the caller the module token names at `GET /v1/caller` | `Homeport.Modules.Echo.Infrastructure`, `Homeport.Sdk`, `Homeport.Shared.Client` |
| `Architecture.Tests` | Asserts the layout, naming and reference rules of `dotnet.layout` as `AGENTS.md` narrows them, each also against a fixture tree that breaks it (solution folder `/Tests/`) | — |
| `Homeport.Aspire.AppHost.Tests.Integration` | Starts the AppHost, waits for every resource to become healthy and for `migrate` to complete before the control plane, reads Echo's manifest, gets tokens from Keycloak and calls Echo through the gateway with and without a token, without `echo.invoke` and as a client the control plane does not know, reads the tenant and client Echo's module token names, calls Echo directly without a token, reads the gateway's JWKS, opens the API docs the dashboard links the gateway to, reads the usage metering aggregated for a call to Echo and for a batch whose quantity Echo reports, checks that the report does not reach the caller, exceeds the seeded plan's rate limit through the gateway and through both replicas directly, and stops Echo or the registry to assert the routes end to end; needs Docker | `Homeport.Aspire.AppHost` |
| `Homeport.Modules.Echo.Api.Tests.Integration` | Runs Echo in memory behind a stand-in gateway's JWKS and asserts its manifest, health, echo, batch, request and caller endpoints with and without a module token and scope, and its OpenAPI document, including the descriptions of its `Homeport.Shared.Client` types | `Homeport.Modules.Echo.Api` |
| `Homeport.Modules.Echo.Domain.Tests.Unit` | Asserts how much a batch counts and that an empty one is rejected | `Homeport.Modules.Echo.Domain` |
| `Homeport.Modules.Echo.Application.Tests.Unit` | Asserts that a batch comes back in its order with its quantity, and that an empty one fails | `Homeport.Modules.Echo.Application` |
| `Homeport.Platform.ControlPlane.Tests.Integration` | Runs `migrate` and the control plane against a throwaway PostgreSQL prepared by the init script and asserts the client lookup with its token and error responses, that `migrate` seeds exactly once and fails on an invalid seed, and that the app role has no DDL rights; needs Docker | `Homeport.Platform.ControlPlane` |
| `Homeport.Platform.Gateway.Api.Tests.Integration` | Runs the gateway in memory against a stand-in module, registry, identity provider and control plane and asserts which tokens, clients and scopes it accepts, the module token it forwards, what it forwards on which routes, how it polls the registry, the error responses it answers with, whose forwarded headers it applies, the JWKS it serves, the usage events it publishes with the quantity a module reports or the route's, the reports it rejects and removes, the `429`, `Retry-After` and RateLimit headers it answers with, that it neither meters a rejected request nor throttles one while Valkey is unreachable, that its errors read as `ProblemResponse` without loss, the consolidated API docs it serves, keeps and switches off, and that it does not start without a signing key or an identity provider or with an unreadable trusted proxy | `Homeport.Platform.Gateway.Api` |
| `Homeport.Platform.Gateway.Application.Tests.Unit` | Asserts that a request is decided on the bucket it drew from, and passes undecided while the buckets are unavailable | `Homeport.Platform.Gateway.Application` |
| `Homeport.Platform.Gateway.Domain.Tests.Unit` | Asserts which reported quantities are accepted and why one is rejected, the limits a plan's rate limit takes and what a draw from its bucket tells the caller, and what a consolidated API document keeps, rewrites and leaves out | `Homeport.Platform.Gateway.Domain` |
| `Homeport.Platform.Gateway.Infrastructure.Tests.Unit` | Asserts the checks on the signing and module token configuration, the service tokens and their renewal, the module tokens, the cached client lookup at the control plane, the retry delays of usage events, and that an outage of Valkey is warned about at most once per interval | `Homeport.Platform.Gateway.Infrastructure` |
| `Homeport.Platform.Gateway.Infrastructure.Tests.Integration` | Publishes usage events to a throwaway RabbitMQ and asserts that they arrive and that one the broker does not take is counted as dropped; draws from token buckets in a throwaway Valkey and asserts that parallel draws never take more than a bucket holds, a bucket per tenant, its expiry, and that a stopped Valkey is reported and counted; needs Docker | `Homeport.Platform.Gateway.Infrastructure` |
| `Homeport.Platform.Metering.Domain.Tests.Unit` | Asserts the UTC hour and day buckets, either side of midnight, and what an event adds to its aggregates | `Homeport.Platform.Metering.Domain` |
| `Homeport.Platform.Metering.Application.Tests.Unit` | Asserts that recording hands the store the event with its increments and reports a duplicate | `Homeport.Platform.Metering.Application` |
| `Homeport.Platform.Metering.Infrastructure.Tests.Integration` | Asserts recording and duplicates in a throwaway PostgreSQL prepared by the init script, and the cross-tenant leakage tests through EF Core and raw SQL as the app role; needs Docker | `Homeport.Platform.Metering.Infrastructure` |
| `Homeport.Platform.Metering.Api.Tests.Integration` | Runs `migrate` and metering against a throwaway PostgreSQL and RabbitMQ and asserts that a published event is aggregated, that one delivered twice counts once, and what `homeport.metering.events` counts; needs Docker | `Homeport.Platform.Metering.Api` |
| `Homeport.Platform.Registry.Domain.Tests.Unit` | Asserts the manifest rules, SemVer precedence, how the routes of several instances merge and the key comparison | `Homeport.Platform.Registry.Domain` |
| `Homeport.Platform.Registry.Application.Tests.Unit` | Asserts registering, renewing and building the routes on an in-memory store | `Homeport.Platform.Registry.Application` |
| `Homeport.Platform.Registry.Infrastructure.Tests.Unit` | Asserts the checks on the configured registration keys | `Homeport.Platform.Registry.Infrastructure` |
| `Homeport.Platform.Registry.Infrastructure.Tests.Integration` | Asserts keeping, renewing and expiring instances in a throwaway Valkey; needs Docker | `Homeport.Platform.Registry.Infrastructure` |
| `Homeport.Platform.Registry.Api.Tests.Integration` | Runs the registry in memory against a throwaway Valkey and asserts registration, heartbeat and the route table with their keys, tokens and error responses; needs Docker | `Homeport.Platform.Registry.Api` |
| `Homeport.Sdk.Tests.Unit` | Asserts the registration settings, the module version, the problem type rules, the OpenAPI document check, reading the caller from a module token and reporting usage | `Homeport.Sdk` |
| `Homeport.Sdk.Tests.Integration` | Runs a module built with the SDK in memory and asserts its endpoints, which module tokens and scopes it accepts, error responses, that they read as `ProblemResponse` without loss, the errors its OpenAPI document lists, and its registration and heartbeats against a stand-in registry | `Homeport.Sdk` |
| `Homeport.Shared.Authentication.Tests.Unit` | Asserts which tokens the validation accepts and rejects, and that the JWKS carries public keys only | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Authentication.Tests.Integration` | Runs a service that accepts gateway tokens against a stand-in JWKS, including a key published after it was read | `Homeport.Shared.Authentication` |
| `Homeport.Shared.Contracts.Tests.Unit` | Asserts the result type | `Homeport.Shared.Contracts` |
| `Homeport.Shared.ServiceDefaults.Tests.Integration` | Asserts the health endpoints outside Development | `Homeport.Shared.ServiceDefaults` |

The gateway, the registry, the control plane and metering are the only platform services so far. The target picture is in
[vision.md](vision.md); the binding layout is in
[`AGENTS.md`](../../AGENTS.md#architecture).

## Directory layout

```text
Homeport.slnx
Directory.Build.targets   adds the public API analyzer where a project opts in
Directory.Packages.props
deploy/
  compose/                generated from the AppHost, never edited
  postgres/init/          schemas and roles, run once by PostgreSQL
  rabbitmq/init/          a broker user per service, written on every start
dotnet-tools.json         dotnet-ef for adding migrations
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
    ControlPlane/
      Homeport.Platform.ControlPlane/
    Gateway/
      Homeport.Platform.Gateway.Domain/
      Homeport.Platform.Gateway.Application/
      Homeport.Platform.Gateway.Infrastructure/
      Homeport.Platform.Gateway.Api/
    Metering/
      Homeport.Platform.Metering.Domain/
      Homeport.Platform.Metering.Application/
      Homeport.Platform.Metering.Infrastructure/
      Homeport.Platform.Metering.Api/
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
    Homeport.Modules.Echo.Application.Tests.Unit/
    Homeport.Modules.Echo.Domain.Tests.Unit/
    Homeport.Platform.ControlPlane.Tests.Integration/
    Homeport.Platform.Gateway.Api.Tests.Integration/
    Homeport.Platform.Gateway.Application.Tests.Unit/
    Homeport.Platform.Gateway.Domain.Tests.Unit/
    Homeport.Platform.Gateway.Infrastructure.Tests.Integration/
    Homeport.Platform.Gateway.Infrastructure.Tests.Unit/
    Homeport.Platform.Metering.Api.Tests.Integration/
    Homeport.Platform.Metering.Application.Tests.Unit/
    Homeport.Platform.Metering.Domain.Tests.Unit/
    Homeport.Platform.Metering.Infrastructure.Tests.Integration/
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
  service without business rules may stay a single project, as the control
  plane does while it only looks clients up. The registry
  has rules of its own — checking a manifest, merging the manifests of the
  live instances — so it is cut into layers, and so is the gateway, whose
  rules decide what a reported quantity is worth and what a tenant's rate
  limit tells a caller. Metering is cut from the start: bucketing
  usage and counting each event once are business rules.
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
  Homeport ever does, so it is never shipped. `UsageRecorded` lives there
  for the same reason: the gateway publishes it and metering consumes it.
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
