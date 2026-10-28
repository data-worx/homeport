# AGENTS.md — homeport

Binding rules for anyone working in this repository, human or agent.
**It prescribes, it does not describe** — explanatory prose belongs in `docs/`,
and this file links to it.

## Shared rules

These files come from [raisr/devkit](https://github.com/raisr/devkit) and are
updated by `/devkit-sync`. Do not edit them here; change them upstream.

The `@` lines below are imports, not links: they pull the file into the
session. Turning one into a Markdown link silently switches the rules off.

@AGENTS.core.md — rules that hold in every repository
@AGENTS.dotnet.md — rules for dotnet
@AGENTS.dotnet-core.md — rules for dotnet-core
@AGENTS.github.md — conventions for github

@AGENTS.local.md — personal, machine-specific, git-ignored; wins on conflicts.
It is imported the same way, and the import is simply ignored where the file
does not exist.

## Overview

Homeport is a self-hosted platform for deep business modules: an operator runs
it in their own environment, and it provides gateway, multi-tenancy,
authentication, throttling, usage metering, observability and an admin portal.
Each module covers one thick business domain in depth.

Two facts a newcomer gets wrong:

- **Not a hosted SaaS.** Business data never leaves the operator's
  infrastructure. Nothing may phone home, and nothing may depend on a service
  operated by data-worx.
- **Not an invoicing system.** Homeport meters and exports usage for showback
  and chargeback. It never creates invoices.

Background: [docs/architecture/vision.md](docs/architecture/vision.md).

## Decisions

The architecture decisions are recorded in
[docs/architecture/adr/](docs/architecture/adr/README.md). They are binding.

- **Do not change a decision silently.** To change one, propose a new ADR that
  supersedes it and get the maintainer's approval before writing code.
- **An ADR is required** when a decision is hard to reverse or crosses a
  boundary: the module contract, a public API, the data model of tenant-owned
  data, a new infrastructure component (broker, store, identity provider), a
  security mechanism, the licence or the distribution model.
- **No ADR** for anything else — a library inside one service, naming,
  internal structure. That is decided in the pull request.

## Documentation

`docs/` is arranged by reader. A document goes into the directory of the
reader it is written for:

| Directory | Reader |
|---|---|
| `docs/operators/` | Installing, configuring and running Homeport |
| `docs/module-developers/` | Building a module on the contract and the SDK |
| `docs/api-consumers/` | Obtaining a token, calling modules, reading usage |
| `docs/reference/` | Normative reference, starting with the module contract |
| `docs/architecture/` | Vision, architecture and the ADRs |

- A slice adds or updates the pages of every reader it affects, in the same
  pull request.
- A directory is created with its first document, never empty.
- English only. Every document is listed in [docs/README.md](docs/README.md).

## Tech stack

- .NET 10, ASP.NET Core, C# latest.
- The .NET Aspire AppHost is the source of truth for the topology. Compose
  files are generated from it, never edited by hand.
- PostgreSQL through EF Core. RabbitMQ through Wolverine. Valkey for
  distributed counters. YARP for the gateway. Blazor with MudBlazor for the
  portal. Keycloak only in the dev/test compose.
- Tests: xUnit, Shouldly, Testcontainers for PostgreSQL, RabbitMQ and Valkey,
  Aspire testing for end-to-end.
- Central package management: every package version lives in
  `Directory.Packages.props`.
- Use `TimeProvider`, never `DateTime.Now` or `DateTime.UtcNow`.
- Keep data access provider-neutral. Vendor-specific SQL is allowed only for
  the row-level security setup.

## Architecture

**Provisional** — see the `dotnet.layout` row under *Deviations*. Revisit with
the first service tickets.

```text
Homeport.slnx
src/
  Aspire/Homeport.Aspire.AppHost
  Shared/Homeport.Shared.ServiceDefaults
  Shared/Homeport.Shared.Authentication
  Shared/Homeport.Shared.Contracts
  Sdk/Homeport.Sdk
  Platform/<Service>/Homeport.Platform.<Service>
  Platform/<Service>/Homeport.Platform.<Service>.Domain | .Application | .Infrastructure | .Api [| .Worker]
  Modules/<Module>/Homeport.Modules.<Module>.Domain | .Application | .Infrastructure | .Api [| .Worker]
  Tools/Homeport.Tools.<Tool>
  Tests/<project under test>.Tests.Unit | .Tests.Integration
  Tests/Architecture.Tests
```

- **One module = one API service plus an optional worker.** Features inside a
  module are components, not services.
- **The layer rule of `dotnet.layout` applies per service.** A service is one
  project while it carries no business rules. Once it does, it is cut into
  Domain, Application, Infrastructure and a host project that is its
  composition root. Business modules are cut from the start.
- A service never references another service's projects. Services talk over
  HTTP or messages; shared types live in `Homeport.Shared.Contracts`.
- Modules are reachable only through the gateway.
- A project is named `Homeport`, then every grouping directory between `src/`
  and the project, then its own name, as in the tree above
  (`dotnet.layout`). Its directory, assembly and root namespace carry the
  same name. `src/Tests/` is not a grouping directory: a test project is
  named after the project under test (`dotnet.tests`).
- Inside a service or module the arrow points inwards: Domain references
  nothing beyond the BCL and the exceptions listed in
  `src/Tests/Architecture.Tests/DomainExceptions.cs`; Application its own
  Domain; Infrastructure its own Application and Domain; a host (`.Api`,
  `.Worker`) its own inner layers. All but Domain may reference
  `src/Shared/` and `src/Sdk/`.
- `src/Shared/` and `src/Sdk/` reference no service or module.
- `src/Aspire/` references hosts and `src/Shared/` only — never an inner
  layer.
- Tools and tests are unrestricted.
- `Architecture.Tests` asserts the layout, naming and reference rules above,
  each also against a fixture tree that breaks it.
- Messages between services (for example `UsageRecorded`) are contracts in
  `Homeport.Shared.Contracts`. Changing one is a breaking change for every consumer.

Current state: [docs/architecture/architecture.md](docs/architecture/architecture.md).

### Module contract

The contract is normative in
[docs/reference/module-contract/](docs/reference/module-contract/README.md).
Any change to it bumps `contractVersion` and updates the SDK, the documentation
and the Echo module **in the same pull request**.

## Multi-tenancy

Security-critical. No exception without an ADR.

- Every tenant-owned table has a non-null `TenantId` (uuid).
- EF Core global query filters on every tenant-owned entity.
  `IgnoreQueryFilters()` only in code paths explicitly marked as platform
  admin.
- PostgreSQL row-level security on every tenant-owned table, driven by the
  session setting `homeport.tenant_id`, which an interceptor sets per
  connection or transaction.
- Platform-admin access uses a dedicated database role and is audited.
- Every feature that touches tenant data has a **cross-tenant leakage test**.

## Gateway

- Modules are reached under `/modules/{moduleId}/{path}`, platform
  endpoints under `/platform/{path}`. The gateway strips the prefix, and the
  module id `platform` is reserved. See
  [ADR-0013](docs/architecture/adr/0013-url-scheme-and-api-versioning.md).
- The gateway strips every incoming `X-Homeport-*` header.
- The gateway calls a module with its own short-lived signed token: audience
  the module id, claims tenant, client, subject and scopes. A module trusts
  only that token, validates it against the gateway's JWKS and never talks to
  the identity provider. See ADR-0011.
- The gateway calls platform services with service tokens, as
  *Service-to-service* and
  [ADR-0014](docs/architecture/adr/0014-authentication-between-services.md)
  prescribe.
- W3C `traceparent` is propagated to the module.
- If the registry is unavailable, the gateway keeps serving the last known
  routes.

## Service-to-service

Security-critical. No exception without an ADR. See
[ADR-0014](docs/architecture/adr/0014-authentication-between-services.md).

- Every call between components — HTTP, registration and messaging — is
  authenticated as its concrete caller. Reaching the internal network
  grants nothing.
- No secret is shared between callers. Tokens are audience-bound and
  short-lived. A long-lived secret belongs to exactly one caller, serves
  one purpose and rotates without downtime.
- In v0.1 only the gateway calls platform services, with a service token it
  issues with the same key as module tokens: audience the called component,
  subject `homeport-gateway`, lifetime at most 5 minutes. A service rejects
  a request without a valid token for its own audience with `401`.
- Any other caller gets its mechanism through an ADR that meets these
  rules.
- Traffic between components is not encrypted by Homeport until mTLS is
  decided; that is left to the operator's network.

## Throttling

- Two layers, both per tenant and plan: a rate limit (token bucket, requests
  per second) and a quota (per meter, per day or month).
- Counters live in Valkey, so limits hold across gateway replicas.
- Exceeding a limit returns `429` with `Retry-After`. Remaining limits are
  exposed through RateLimit headers.

## Metering

- `UsageRecorded` carries `eventId` (idempotency key), `occurredAt`,
  `tenantId`, `clientId`, `moduleId`, `route`, `meter`, `quantity`,
  `statusCode`, `durationMs`, `traceId`.
- The consumer is idempotent: a unique constraint on `eventId`. A duplicate is
  never counted twice.
- Aggregates per tenant, module, meter, hour and day. Export as CSV and JSON;
  a webhook fires when a period closes.

## Observability

- Every service uses `Homeport.Shared.ServiceDefaults`.
- `service.name` is `homeport-<component>` or `homeport-module-<id>`.
- Custom attributes: `homeport.tenant.id`, `homeport.client.id`,
  `homeport.module.id`, `homeport.meter`.
- Metric names start with `homeport.` (`homeport.gateway.requests`,
  `homeport.gateway.throttled`, `homeport.metering.events`).
- No high-cardinality metric labels: no request ids, trace ids or user input.
- One trace spans gateway → module → RabbitMQ → metering.

## Licensing and dependencies

- Every source file starts with `SPDX-License-Identifier: AGPL-3.0-or-later`
  in the comment syntax of its language (`// …` in C#).
- Only dependencies under AGPL-compatible permissive licences: MIT,
  Apache-2.0, BSD, ISC, PostgreSQL, and SIL OFL for fonts. Check the licence
  **before** adding the package and record it in `THIRD-PARTY-NOTICES.md`.
- No commercial or source-available packages. This excludes MassTransit v9+
  and the commercial versions of MediatR, AutoMapper and FluentAssertions.
- Contributions require the signed CLA — see [CONTRIBUTING.md](CONTRIBUTING.md).
- The names "Homeport" and "data-worx" and their logos are not licensed with
  the code — see [TRADEMARKS.md](TRADEMARKS.md).

## Supply chain

- CI builds, tests, creates an SBOM and scans **every image in the compose
  files** — own and third-party — with Trivy. High or Critical fails the build.
- Exceptions only in `.trivyignore`, each with a reason and an expiry date.
- A nightly scheduled re-scan covers all images.
- Third-party images are pinned by digest. Dependabot proposes updates.
- Own images use chiseled .NET base images, run as non-root, and are signed
  with cosign on release.

## Work organisation

- Every issue belongs to a milestone. A pull request carries the milestone of
  the issue it closes.
- Pull requests are squash-merged. The title becomes the commit on `main` and
  follows `forge.commits`.
- Open questions and ideas live in GitHub Discussions, not in issues.
- Everything stays runnable through the AppHost.

## Domain terms

| Term | Means |
|---|---|
| Operator | The company that runs a Homeport installation in its own environment |
| Tenant | A department or customer of the operator; the unit of isolation and of metering |
| Client | A machine identity of a tenant (client credentials, later API key) |
| Plan | The limits a tenant gets: rate limit and quotas per meter |
| Module | One deep business domain: an API service plus an optional worker |
| Catalog module | A module shipped with Homeport and signed by data-worx |
| Internal module | A module the operator builds on the same contract |
| Manifest | What a module declares about itself at `/.well-known/homeport/manifest` |
| Registry | The service modules register with and send heartbeats to |
| Meter | A unit usage is counted in (`requests`, `messages`, `documents`) |
| Quota | A limit on a meter per day or month |
| Showback / chargeback | Showing or allocating usage to tenants; Homeport exports, it never invoices |

## Deviations from the shared rules

Rules from `AGENTS.core.md` or a stack pack that deliberately do not apply here.
Every row was decided once and is recorded in `devkit.lock.json`, so
`/devkit-sync` does not ask again until the upstream rule changes.

| Rule | Deviation | Why |
|---|---|---|
| `dotnet.layout` | **Provisional.** The layer cut applies per service, not to one application: services under `src/Platform/<Service>/`, modules under `src/Modules/<Module>/`, plus `src/Aspire/`, `src/Shared/` and `src/Sdk/`. A service without business rules is a single project. Project names follow the grouping rule of `dotnet.layout`, such as `Homeport.Platform.<Service>.Domain`. See *Architecture* | Homeport is a set of independently deployable services, not one application. Revisit with the first service tickets |
