# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- The platform document of the API docs at `/platform/docs` lists the
  platform APIs, starting with the usage export at `GET /platform/v1/usage`,
  with the parameters and responses the service behind each describes.
  Metering serves its OpenAPI document to the gateway; the gateway reads it
  every 30 seconds and keeps the last one while metering is unavailable. See
  [API docs](docs/api-consumers/api-docs.md).

- A tenant reads its own usage at `GET /platform/v1/usage`, per hour or per
  day and optionally per module and meter, as JSON or, with
  `Accept: text/csv`, as CSV with a header row, for showback and
  chargeback. The client needs the scope `usage.read`; the gateway forwards
  the call to metering with a token that names the client's tenant, and
  metering serves that tenant's rows only. The call is rate-limited but not
  metered. See [reading usage](docs/api-consumers/reading-usage.md).

- Homeport starts locally with its infrastructure — PostgreSQL, RabbitMQ,
  Valkey and Keycloak — either through the .NET Aspire AppHost or through a
  Docker Compose file generated from it. Every third-party image is pinned by
  digest. See [running Homeport locally](docs/operators/running-locally.md).
- The module SDK, `Homeport.Sdk`, and the Echo reference module built on it.
  A module declares its manifest in code; the SDK serves it with the health
  endpoints and an OpenAPI document. Echo returns whatever it is sent and runs in the AppHost and the
  compose file. See [declaring a manifest](docs/module-developers/declaring-a-manifest.md).
- The gateway, Homeport's single entry point. A request to
  `/modules/echo/v1/echo` reaches the Echo module as `/v1/echo`; incoming
  `X-Homeport-*` headers are removed and `traceparent` is passed on, so a
  caller can neither impersonate the platform nor lose its trace. The gateway is
  the only service with an external endpoint; modules stay on the internal
  network. Echo gains `GET /v1/request`, which shows the headers it received.
  See [calling a module](docs/api-consumers/calling-a-module.md).
- The gateway signs tokens. It loads an ES256 signing key from its
  configuration, refuses to start without one, and publishes the public keys
  at `/.well-known/jwks.json`. A second public key can be published next to
  it, so the key rotates without rejecting a single request. The gateway can
  issue short-lived service tokens for its calls to platform services, and a
  shared building block lets a service accept those tokens and nothing else.
  The AppHost generates a key for local runs; the compose file takes it from
  `.env`. See [the gateway's signing key](docs/operators/signing-key.md).
- Modules built with the SDK answer every error as Problem Details
  (`application/problem+json`) with a `traceId`, so a caller handles one
  error format. An unhandled exception becomes a `500` that reveals nothing
  of its cause, and an invalid request a `400` that lists each failure by its
  camelCase field path. A module declares its own problem types and maps
  business errors onto them. Echo answers a request without a body with a
  problem type of its own. See [answering errors](docs/module-developers/answering-errors.md)
  and [problem types](docs/reference/problems/README.md).
- The gateway holds every error response to Problem Details, whichever
  module answered. A module's Problem Details 4xx passes through with the
  gateway's `traceId`; any other error body, and the body of every 5xx, is
  replaced, so no module internals reach the caller. The gateway's own errors
  carry types of their own — `unknown-module`, `module-unreachable`,
  `module-unavailable`, `module-timeout` — so a caller can tell them from a
  module's. See [errors](docs/api-consumers/errors.md).
- A module's OpenAPI document describes its endpoints from the XML docs of
  its controllers, names every operation, and lists the errors every module
  endpoint can answer with — `401`, `403`, `429` and `500` — as Problem
  Details. `ModuleOpenApi.Validate` lets a module's test fail on an operation
  without a summary, an operation id or a success response. A module now
  calls `AddOpenApi` itself. See [documenting endpoints](docs/module-developers/documenting-endpoints.md).
- `Homeport.Shared.Client` holds the wire types Homeport ships: the module
  manifest, `ProblemResponse` to read any Homeport error, and the request and
  response types of the catalog modules, starting with Echo. It references
  nothing, so a .NET caller takes the types without taking ASP.NET or the
  SDK, and it is published with the SDK under the same version.
  `ModuleManifest` and `ManifestRoute` moved there from `Homeport.Sdk`. See
  [calling a module](docs/api-consumers/calling-a-module.md) and
  [errors](docs/api-consumers/errors.md).
- The gateway, the SDK and the token validation of the platform services
  log what an operator needs to diagnose them: each service logs its start
  with its effective configuration, never a key; a rejected gateway token and
  a failed retrieval of the gateway's JWKS are warnings. The level is set per
  category through environment variables. See [logging](docs/operators/logging.md).
- Modules register themselves, and the gateway routes to them live. The new
  registry keeps every running module instance for 30 s after its last
  heartbeat, in Valkey; a module built with the SDK registers once it has
  started and sends a heartbeat every 10 s. The gateway reads the routes from
  the registry every 5 s, so starting a module makes it reachable and stopping
  it removes its routes, with no configuration change at the gateway. Each
  route goes only to the instances that declare it, which allows rolling
  updates and two major versions side by side; while the registry is down,
  the gateway keeps the last known routes. A module authenticates with a
  registration key per module id, of which two can be valid at once for a
  rotation without downtime; the registry checks the manifest and refuses a
  broken one with `422` `invalid-manifest`, and the SDK logs why and stops.
  The gateway now forwards only the routes a module declares. See
  [the registry](docs/operators/registry.md) and
  [registering a module](docs/module-developers/registering-a-module.md).
- The control plane knows the installation's tenants, their plans and their
  clients, and tells the gateway which tenant and plan a client belongs to,
  at `GET /clients/{clientId}` behind a gateway service token. In this
  release it holds one tenant, plan and client, seeded from configuration.
  Homeport's database is prepared by an init script that gives each service
  its own schema, an owner role for its migrations and an app role without
  DDL rights to run as; a one-shot `migrate` step applies the migrations and
  the seed, and the control plane starts only after it succeeded. See
  [the control plane](docs/operators/control-plane.md) and
  [the data model](docs/architecture/data-model.md).
- The gateway authenticates the clients that call modules. A client gets a
  token from the operator's identity provider with the client-credentials
  grant; the gateway validates it against the configured authority and
  audience, reads the client id from a configurable claim (`azp` by default)
  and asks the control plane which tenant the client belongs to, keeping the
  answer for a configurable time. No or an invalid token is answered with
  `401` `unauthorized`, a client the control plane does not know with `403`
  `unknown-client`, and an unreachable control plane with `503`
  `control-plane-unavailable`. Module routes are no longer anonymous. For
  development, Keycloak imports the realm `homeport` with the client the
  control plane seeds, its secret generated rather than committed, and is
  reachable from the host at `localhost:8080`. See
  [obtaining a token](docs/api-consumers/obtaining-a-token.md) and
  [connecting an identity provider](docs/operators/identity-provider.md).
- Modules trust the gateway's token and nothing else. For every request it
  forwards, the gateway replaces the client's token with a short-lived token
  of its own — audience the module id, claims tenant, client, subject and
  scopes, 60 s by default — so the client's credential never reaches a module
  and reaching the internal network no longer lets anyone act as a tenant.
  The gateway also checks the scope each route declares in the manifest and
  answers a token without it with `403` `insufficient-scope`. Modules built
  with the SDK reject every request without a valid module token with `401`,
  get a policy per scope of the manifest and read the caller with
  `HomeportCaller`. Echo now requires `echo.invoke` and gains `GET /v1/caller`;
  the local Keycloak client gets `echo.invoke` only when the token request
  asks for it. See [the module token](docs/module-developers/module-token.md)
  and [connecting an identity provider](docs/operators/identity-provider.md).
- Homeport meters usage. Every request the gateway forwards to a module
  becomes a usage event — tenant, client, module, the route template, its
  meter and quantity, status, duration and trace id — which the new metering
  service adds to an hourly and a daily aggregate per tenant, module and
  meter, in UTC. An event delivered twice counts once. Events and aggregates
  are the first tenant-owned data, kept apart per tenant by query filters
  and PostgreSQL row-level security. Without an outbox, an event can be lost
  between the gateway and RabbitMQ: one the broker does not take within the
  gateway's retries is counted in `homeport.gateway.usage.dropped`. Raw
  events have no retention yet. The gateway and metering connect to
  RabbitMQ with a user of their own each, which the broker's new init script
  creates; the database init script and the compose `.env` gain metering's
  passwords. See [metering](docs/operators/metering.md) and
  [how metering works](docs/architecture/metering.md).
- A module can report how much a request counts with the response header
  `X-Homeport-Usage: <meter>=<quantity>`, so a batch or a large document is
  not metered like a single call. The gateway removes the header before the
  caller sees it. A report in a meter the route does not declare, or with a
  quantity that is not a positive integer, falls back to the route's
  quantity, is logged at Warning and counted in
  `homeport.gateway.usage.rejected`. `Homeport.Sdk` reports with
  `Response.ReportUsage(meter, quantity)`. Echo gains
  `POST /v1/echo/batch`, which counts one in the meter `items` per item. See
  [reporting usage](docs/module-developers/reporting-usage.md).
- Every API of an installation in one place: the gateway serves Scalar at
  `/platform/docs`, with one document per module and one for the platform.
  A module's document shows the paths a caller actually uses and only the
  routes the gateway forwards, each as the newest instance describes it.
  Requests are tried with a pasted bearer token. Everything is served by the
  gateway; nothing is loaded from or sent to a CDN. On by default only in
  Development, where the Aspire dashboard links it as `API docs` on the
  gateway; `Gateway:Docs:Enabled` switches it on elsewhere, anonymously.
  See [API docs](docs/api-consumers/api-docs.md) and
  [switching them on](docs/operators/api-docs.md).
- The gateway holds every tenant to the rate limit of its plan, a token
  bucket of the plan's burst refilled at its requests per second. A request
  over it is answered with `429`, `Retry-After` and the problem type
  `rate-limited`, never reaches the module and is not metered; every module
  response carries the `RateLimit-Policy` and `RateLimit` headers. The
  buckets live in Valkey, so the limit holds across gateway replicas, and the
  AppHost and the compose file now run two. While Valkey is down the gateway
  fails open and counts the requests it let through. See
  [rate limits](docs/api-consumers/rate-limits.md) and
  [throttling](docs/operators/throttling.md).
- Releases with signed images. A tag publishes Homeport's own images to
  GHCR as `ghcr.io/data-worx/homeport-<component>`, each signed with cosign
  and carrying its SBOM as a signed attestation. An operator verifies an
  image with `cosign.pub` from the repository alone, without network access
  to Sigstore. Each GitHub release carries a compose file pinned to the
  released images by digest, the `deploy/` directory it needs, and the SBOM
  of every image. A release is refused without its section in this
  changelog. See [verifying an image](docs/operators/verifying-images.md).

### Changed

- The module contract is normative at `contractVersion` 1. `Homeport.Sdk`
  and Echo declare 1, and the registry accepts only 1, so a module built on
  a draft of the contract is rejected with `422` until it is rebuilt.
- Homeport's own images are named after their component — `homeport-gateway`,
  `homeport-registry`, `homeport-control-plane`, `homeport-metering`,
  `homeport-module-echo` — locally as in GHCR. A `.env` for the compose file
  that names the old local images needs the new names.
- The registry rejects a manifest whose `openApiUrl` is not an absolute path
  on the instance, such as `/openapi/v1.json`, with `422`. A module built
  with `Homeport.Sdk` already sends one.

### Fixed

- Keycloak in the AppHost serves plain HTTP on `localhost:8080` even where
  the ASP.NET Core development certificate is trusted. Before, a trusted
  certificate moved Keycloak to HTTPS, so no client got a token and every
  call through the gateway was answered with `401`.
- The control plane and metering answer on `localhost:5300` and
  `localhost:5400` in a local AppHost run. Before, their `migrate` resources
  claimed the same ports and left a dead proxy on them once they finished,
  so the dashboard showed the services unhealthy and the gateway answered
  `503 control-plane-unavailable`.
- The gateway answers its errors with Problem Details whatever the caller's
  `Accept` header asks for. Before, a caller accepting only `text/csv`, such
  as a usage export over its rate limit, got `500` with an empty body
  instead of `429`.
- The seeded tenant, plan and client of a local AppHost run take their
  values from `Parameters:seed-…` in the user secrets, with the documented
  values as defaults. Before, the defaults were fixed and a configured value
  was ignored, so a smaller plan for trying out the rate limit never applied.

### Security

- The gateway answers every request under `/modules/` without a valid token
  with `401`, before it routes. Before, an anonymous caller got `405` with an
  `Allow` header for a wrong method and `404` for an unknown path or module,
  and so learned which paths a module serves and which methods each accepts.
  A caller with a valid token still gets `404` and `405`.
- The gateway ignores `X-Forwarded-For` and `X-Forwarded-Proto` unless they
  come from a reverse proxy the operator trusts, configured under
  `TrustedProxies`. Before, the compose file let the gateway take them from
  any caller, who could so pass as another address or as having called over
  HTTPS. See [running behind a reverse proxy](docs/operators/reverse-proxy.md).
