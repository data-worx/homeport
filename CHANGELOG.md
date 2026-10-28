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
