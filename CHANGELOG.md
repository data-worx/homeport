# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Homeport starts locally with its infrastructure — PostgreSQL, RabbitMQ,
  Valkey and Keycloak — either through the .NET Aspire AppHost or through a
  Docker Compose file generated from it. Every third-party image is pinned by
  digest. See [running Homeport locally](docs/operators/running-locally.md).
- The module SDK, `Homeport.Sdk`, and the Echo reference module built on it.
  A module declares its manifest in code; the SDK checks it against the module
  contract at start-up and serves it with the health endpoints and an OpenAPI
  document. Echo returns whatever it is sent and runs in the AppHost and the
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
