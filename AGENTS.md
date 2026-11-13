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

Homeport is a self-hosted platform for business modules: an operator runs
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
- A migration updates its schema's ER diagram,
  `docs/architecture/data-model-<schema>.drawio.svg` with `-` for `_`, in
  the same pull request. See [the data model](docs/architecture/data-model.md#schemas).
- A change to the path of a call to a module, of a platform request, of an
  admin request or of a module's registration updates its sequence diagram,
  `docs/architecture/request-flow-<flow>.drawio.svg` or the one in
  `docs/module-developers/registering-a-module.md`, in the same pull
  request. See [the request flow](docs/architecture/request-flow.md).

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
  the row-level security setup and for the schema and role setup in
  `deploy/postgres/init/`.
- Code outside C# only where an atomic operation must run inside a data
  store: row-level security SQL in PostgreSQL, counter scripts in Valkey.
  Each one is a file in an Infrastructure project and has an integration
  test. The init scripts in `deploy/` are the one exception: they set up a
  store before any service runs.
- Every service owns one schema in the one Homeport database, with an owner
  role that runs its migrations and an app role without DDL rights that the
  service runs as. Migrations run as a one-shot `migrate` command of the
  service's own image, never at service start.

## Architecture

The stack layout of `dotnet.layout` and `dotnet-core.aspire`: every service
and every module is a stack, grouped under `src/Platform/` and
`src/Modules/`. The rules below narrow it.

```text
Homeport.slnx
src/
  Aspire/Homeport.Aspire.AppHost
  Shared/Homeport.Shared.Contracts
  Shared/Homeport.Shared.Client
  Shared/Homeport.Shared.Internal
  Shared/Homeport.Shared.ServiceDefaults
  Shared/Homeport.Shared.Authentication
  Sdk/Homeport.Sdk
  Platform/<Service>/Homeport.Platform.<Service>
  Platform/<Service>/Homeport.Platform.<Service>.Domain | .Application | .Infrastructure | .Api [| .Worker]
  Modules/<Module>/Homeport.Modules.<Module>.Domain | .Application | .Infrastructure | .Api [| .Worker]
  Tools/Homeport.Tools.<Tool>
  Tests/<project under test>.Tests.Unit | .Tests.Integration
  Tests/Architecture.Tests
  Tests/Homeport.Testing
```

- **One module = one API service plus an optional worker.** Features inside a
  module are components, not services.
- **A service is one project while it carries no business rules.** Once it
  does, it is cut into Domain, Application, Infrastructure and a host project
  that is its composition root. Business modules are cut from the start.
- A service never references another service's projects, its Contracts
  included, and no service or module has a Contracts project of its own.
  Services talk over HTTP or messages; shared types live in `src/Shared/`
  as *Shared types* below prescribes.
- Modules are reachable only through the gateway.
- A project is named `Homeport`, then every grouping directory between `src/`
  and the project, then its own name, as in the tree above
  (`dotnet.layout`). Its directory, assembly and root namespace carry the
  same name. `src/Tests/` is not a grouping directory: a test project is
  named after the project under test (`dotnet.tests`).
- Inside a service or module the arrow points inwards: Domain references
  `Homeport.Shared.Contracts` and nothing else beyond the BCL but the
  exceptions listed in `src/Tests/Architecture.Tests/DomainExceptions.cs`;
  Application its own Domain; Infrastructure its own Application and Domain;
  a host (`.Api`, `.Worker`) its own inner layers. All but Domain may
  reference `src/Shared/`; only a host references `src/Sdk/`.
- `src/Shared/` and `src/Sdk/` reference nothing outside `src/Shared/`.
- `src/Aspire/` holds only the AppHost, which references hosts and Shared
  libraries only — never an inner layer. Only tests reference it.
- `src/Sdk/` holds only `Homeport.Sdk`.
- Tools and tests are unrestricted.
- `Architecture.Tests` asserts the layout, naming and reference rules above,
  each also against a fixture tree that breaks it.

Current state: [docs/architecture/architecture.md](docs/architecture/architecture.md).

### Shared types

See [ADR-0018](docs/architecture/adr/0018-where-wire-types-live.md).

- `Homeport.Shared.Contracts` holds the generic core several components
  share, such as the result type, and references nothing beyond the BCL — no
  package, framework or project. It is shipped with the SDK.
- `Homeport.Shared.Client` holds the request and response types of every
  endpoint a consumer reaches through the gateway — platform APIs and catalog
  modules — the types of the module contract, and the Problem Details read
  model. It references nothing. Namespaces mirror `src/` and the API major
  version: `Homeport.Shared.Client.Modules.Echo.V1`; `ModuleContract` and
  `Problems` carry no version.
- `Homeport.Shared.Internal` holds what services share and Homeport never
  ships: messages between services (for example `UsageRecorded`),
  integration events and the like. Changing one is a breaking change for
  every consumer. The project is created with its first type.
- Domain references neither `Homeport.Shared.Client` nor
  `Homeport.Shared.Internal`. Infrastructure and a `.Worker` do not reference
  `Homeport.Shared.Client`. `Homeport.Sdk` references
  `Homeport.Shared.Client` and never `Homeport.Shared.Internal`, and only the
  Shared projects listed in `src/Tests/Architecture.Tests/SdkReferences.cs`.
- No business logic in `src/Shared/`. A rule an external standard defines,
  such as an IBAN check, is a package or a BCL-only `Homeport.Shared.<Topic>`
  that knows no Homeport concept, listed in `DomainExceptions.cs`.
- `Homeport.Sdk` and every project it references run
  `Microsoft.CodeAnalysis.PublicApiAnalyzers`; every public member is listed
  in `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt`. A project switches
  it on with `<TrackPublicApi>true</TrackPublicApi>`, and
  `Directory.Build.targets` adds the analyzer. It flows to no caller, so it
  is not a reference in the sense of the rules above.
- `Homeport.Shared.Client` is published with `Homeport.Sdk`, under the same
  version.
- The code follows these rules from #105 on.

### Module contract

The contract is normative in
[docs/reference/module-contract/](docs/reference/module-contract/README.md).
Any change to it bumps `contractVersion` and updates the SDK, the documentation
and the Echo module **in the same pull request**.

## Multi-tenancy

Security-critical. No exception without an ADR.

- A table is either tenant-owned — its rows are a tenant's data — or a
  platform table the operator owns, such as the control plane's tenants,
  plans and clients. A platform table has no query filter and no row-level
  security, even where it names a tenant, as `Clients` does. See
  [the data model](docs/architecture/data-model.md).
- Every tenant-owned table has a non-null `TenantId` (uuid).
- EF Core global query filters on every tenant-owned entity.
  `IgnoreQueryFilters()` only in code paths explicitly marked as platform
  admin.
- PostgreSQL row-level security on every tenant-owned table, driven by the
  session setting `homeport.tenant_id`, which an interceptor sets per
  connection or transaction.
- Platform-admin access uses a dedicated database role per service,
  `<schema>_admin`, which reads every tenant's rows through a row-level
  security policy for that role, never `BYPASSRLS`. It gets `SELECT` on what
  it reads and `INSERT` and `SELECT` on the service's audit table, nothing
  else. See
  [ADR-0020](docs/architecture/adr/0020-the-operator-as-a-caller.md).
- Every admin change and every admin read of tenant data writes an entry
  into the platform table `<schema>.audit_entries` of the owning service,
  in the same transaction, with the operator's subject and display name.
  The app role has `INSERT` and `SELECT` on it, nothing else, and an
  integration test asserts that. Homeport never deletes an entry.
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
- A client request the gateway forwards to a platform service, such as
  `/platform/v1/usage`, carries a caller token instead: the claims of a
  module token, audience the service. Platform routes are configured in
  the gateway, never registered. See
  [ADR-0019](docs/architecture/adr/0019-tokens-for-forwarded-platform-requests.md).
- The admin API, `/platform/v1/admin/...`, answers only on the gateway's
  second, admin endpoint; on the public endpoint it does not exist. The
  compose file does not publish the admin port. See
  [ADR-0020](docs/architecture/adr/0020-the-operator-as-a-caller.md).
- W3C `traceparent` is propagated to the module.
- If the registry is unavailable, the gateway keeps serving the last known
  routes.
- The gateway serves the consolidated API docs at `/platform/docs` with
  Scalar, its assets served by the gateway, never from a CDN: one document
  per module plus one for the platform. A module's document shows exactly
  the routes in the routing table, each from the instance with the highest
  `version` that declares it, with servers and paths rewritten to the public
  path. The last known documents are kept while the registry is
  unavailable. See
  [ADR-0017](docs/architecture/adr/0017-consolidated-api-docs.md).
- `Gateway:Docs:Enabled` switches the docs on, by default only in
  Development. They are anonymous; an authenticated view needs a new ADR.
  *Try it* takes a pasted bearer token, never a client secret.

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
- A service token carries no tenant. An endpoint that serves a tenant's
  data requires a caller token's tenant claim and scope, and serves that
  tenant only (ADR-0019).
- The operator is a caller through the portal (ADR-0020). The portal calls
  the admin API from its server only, with the operator's access token from
  OIDC authorization code with PKCE. The gateway checks the configured admin
  claim and forwards with a caller token: audience the service, subject the
  operator, scope `admin`, no tenant claim, lifetime at most 5 minutes. A
  service serves its admin endpoints on that scope only.
- Any other caller gets its mechanism through an ADR that meets these
  rules.
- Traffic between components is not encrypted by Homeport until mTLS is
  decided; that is left to the operator's network.

## Error responses

See [ADR-0016](docs/architecture/adr/0016-error-responses.md).

- Every 4xx and 5xx response that leaves Homeport is Problem Details
  (RFC 9457), media type `application/problem+json`. A module answers every
  4xx and 5xx that way; the SDK does it by default.
- `type` is an absolute URI. Homeport's own types live under
  `https://homeport.data-worx.de/problems/<name>`, each with a reference
  page. A module uses a prefix of its own.
- Every error response carries the extension `traceId`, set by the gateway.
- Validation errors use the type `…/problems/validation`, status 400, and the
  extension `errors` as `{ "<field path>": ["<message>", …] }`, with field
  paths in the request's JSON notation (`items[0].name`).
- `title` and `detail` are English. No localisation.
- The gateway passes a conforming 4xx through and adds `traceId`. It replaces
  every non-conforming error body, every 5xx body from a module, and answers
  its own errors (401, 429, 502, 503, 504, unknown module) with a Problem
  Details body of its own.
- A business error is a result value inside a service. The API layer maps it
  onto a problem type; the domain knows no HTTP status.
- The one exception: the portal answers a browser's request with an HTML
  error page that shows the trace id. It reads the admin API's Problem
  Details with the `Problems` read model of `Homeport.Shared.Client`
  (ADR-0020).

## Throttling

- Two layers, both per tenant and plan: a rate limit (token bucket, requests
  per second) and a quota (per meter, per day or month).
- Counters live in Valkey, so limits hold across gateway replicas.
- Exceeding a limit returns `429` with `Retry-After`. Remaining limits are
  exposed through RateLimit headers.
- Valkey unreachable: fail open. Requests pass unthrottled, counted by
  `homeport.gateway.ratelimit.unavailable`, with a rate-limited warning.

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

## Tests

A test that passes or fails depending on timing is a broken test. See
[docs/architecture/testing.md](docs/architecture/testing.md).

- A fake clock goes only to the component under test. It is never
  registered as `TimeProvider` in a host or container, where HTTP clients,
  resilience and hosting pick it up too. No test waits for real time to
  pass.
- An asynchronous assertion waits for a signal, or polls with a timeout. No
  fixed `Task.Delay` stands in for a wait.
- A stand-in for another component records a request only after it has
  answered it, so a test that saw the request never tears down a response
  still on its way.
- Every test host is stopped before it is disposed.
- The end-to-end tests in `Homeport.Aspire.AppHost.Tests.Integration` show
  only what the running AppHost alone can show — resources start healthy,
  start order, what is exposed, and that the components are wired to each
  other — on one stack shared by the whole run. A behaviour of one component
  is tested in that component's tests.

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

See [ADR-0009](docs/architecture/adr/0009-supply-chain-security.md) and
[the supply chain](docs/architecture/supply-chain.md).

- The *Supply chain* workflow scans **every image in the compose files** —
  own and third-party — with Trivy, on every pull request, every push to
  `main` and nightly. High or Critical fails the run. It writes a CycloneDX
  SBOM per own image as a build artifact.
- Every image of the compose files is in the scan: a new own image is added
  to the table in `.github/scripts/own-images.sh`, or the scan and the
  release fail.
- Findings are removed at the source first: a patched version, a newer
  digest, or a leaner variant of the image.
- Exceptions only in `.trivyignore`, each entry with `exp:YYYY-MM-DD` in a
  block that opens with a comment giving the reason. A missing reason or
  expiry date, or an expired entry, fails the run.
- Third-party images are pinned as `tag@digest` in the AppHost. Dependabot
  proposes updates for NuGet packages, GitHub Actions and the digests in
  `deploy/compose/`; a digest update is copied into the AppHost and into
  `ContainerImages` of `Homeport.Testing` in the same pull request.
- Actions in workflows are pinned by commit SHA, the version in a comment.
- Own images use chiseled .NET base images, run as non-root, and are signed
  with cosign on release.
- A new own image's package in GHCR is created private by its first
  release. The maintainer makes it public by hand in the package settings
  right after that release.
- The *Release* workflow signs with the key pair whose public half is
  `cosign.pub`, without Sigstore's transparency log, so an operator verifies
  offline. The private key exists only as a repository secret and in the
  maintainer's secret store.
- One `THIRD-PARTY-NOTICES.md` in the repository root covers every project
  that becomes an own image: each is listed in `NOTICES_PROJECTS` in
  `.devkit/gates.sh`, sets `ShipsThirdPartyNotices`, and carries the file at
  `/app/THIRD-PARTY-NOTICES.md`. Third-party images are not in it — Homeport
  does not redistribute them — and the content of the base images is covered
  by the SBOM, not by this file.

## Releasing

See [releasing](docs/releasing.md).

- Release model: **trunk with tags** (`core.release-models`). Candidates
  and releases are tagged on `main`; `release/X.Y` only for a fix to a line
  that is no longer the latest.
- Artifacts: **web or service** (`core.release-service`). Every own image
  of the compose files, as `ghcr.io/data-worx/<image>:<version>`, plus a
  GitHub release carrying the compose file pinned to them by digest, the
  deploy bundle `homeport-deploy-<version>.tar.gz` and one SBOM per image.
- `src/Directory.Build.props` carries the placeholder version `0.0.0-dev`.
  The *Release* workflow passes the version from the tag to the build.
- `.github/scripts/check-release.sh` refuses any tag but `vX.Y.Z` and
  `vX.Y.Z-rc.N` on `main` or `release/X.Y`, and any without its section
  `## [X.Y.Z]` in `CHANGELOG.md`. The workflow runs the gates on the
  tagged commit before anything is pushed.

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

None. Homeport follows every shared rule; the *Architecture* section only
narrows `dotnet.layout`, which needs no deviation.
