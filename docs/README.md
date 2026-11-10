# Documentation

Explanation and reference: how it works and why it was built that way. The
binding rules live in [`AGENTS.md`](../AGENTS.md), not here.

Arranged by reader. A directory exists once it has its first document.

## Operators — installing, configuring and running Homeport

| Document | About |
|---|---|
| [`operators/running-locally.md`](operators/running-locally.md) | Starting Homeport on one machine, with the AppHost or with Docker Compose |
| [`operators/verifying-images.md`](operators/verifying-images.md) | The released images in GHCR, what a release carries, and verifying an image and its SBOM with `cosign.pub`, offline |
| [`operators/signing-key.md`](operators/signing-key.md) | Generating, configuring and rotating the gateway's signing key |
| [`operators/registry.md`](operators/registry.md) | The registry, the modules' registration keys and what happens while it is down |
| [`operators/control-plane.md`](operators/control-plane.md) | The control plane, preparing its database, the `migrate` step and the seed configuration |
| [`operators/metering.md`](operators/metering.md) | Metering, preparing its database and the broker users, what may be lost, retention, the usage export, its configuration and metrics |
| [`operators/throttling.md`](operators/throttling.md) | The rate limit per tenant: plan limits, the buckets in Valkey, failing open while Valkey is down, and the metrics |
| [`operators/reverse-proxy.md`](operators/reverse-proxy.md) | Running the gateway behind a reverse proxy, and which proxies' forwarded headers it trusts |
| [`operators/identity-provider.md`](operators/identity-provider.md) | Connecting the identity provider whose client tokens the gateway accepts, and mapping its clients to tenants |
| [`operators/logging.md`](operators/logging.md) | What Homeport logs at which level, and how to change the level |
| [`operators/tracing.md`](operators/tracing.md) | The trace of a request from the gateway to metering, the span attributes and the request metrics |
| [`operators/api-docs.md`](operators/api-docs.md) | Switching the consolidated API docs on with `Gateway:Docs:Enabled`, what they expose and where they come from |

## Module developers — building a module on the contract and the SDK

| Document | About |
|---|---|
| [`module-developers/declaring-a-manifest.md`](module-developers/declaring-a-manifest.md) | Declaring and serving a module's manifest with `Homeport.Sdk` |
| [`module-developers/registering-a-module.md`](module-developers/registering-a-module.md) | Registering a module, heartbeats, rolling updates and two major versions side by side |
| [`module-developers/module-token.md`](module-developers/module-token.md) | Validating the gateway's module token, requiring a scope per endpoint and reading the caller with `Homeport.Sdk` |
| [`module-developers/answering-errors.md`](module-developers/answering-errors.md) | Answering errors as Problem Details with `Homeport.Sdk` |
| [`module-developers/reporting-usage.md`](module-developers/reporting-usage.md) | Reporting how much a request counts with `Homeport.Sdk`, and what the gateway does with the report |
| [`module-developers/documenting-endpoints.md`](module-developers/documenting-endpoints.md) | Documenting a module's endpoints in its OpenAPI document, and checking it in a test |

## API consumers — obtaining a token, calling modules, reading usage

| Document | About |
|---|---|
| [`api-consumers/obtaining-a-token.md`](api-consumers/obtaining-a-token.md) | Getting a client-credentials token and calling a module with it |
| [`api-consumers/calling-a-module.md`](api-consumers/calling-a-module.md) | The URL scheme for calling a module through the gateway |
| [`api-consumers/errors.md`](api-consumers/errors.md) | The error format, and what the gateway does to a module's errors |
| [`api-consumers/rate-limits.md`](api-consumers/rate-limits.md) | The rate limit of your tenant's plan, the RateLimit headers, and `429` with `Retry-After` |
| [`api-consumers/reading-usage.md`](api-consumers/reading-usage.md) | Reading your tenant's usage per hour or day from `/platform/v1/usage`, as JSON or CSV |
| [`api-consumers/api-docs.md`](api-consumers/api-docs.md) | The consolidated API docs at `/platform/docs`, and trying a request with a token from `curl` |

## Architecture — why Homeport is built the way it is

| Document | About |
|---|---|
| [`architecture/vision.md`](architecture/vision.md) | What Homeport is and is not, why it exists, and the target picture |
| [`architecture/architecture.md`](architecture/architecture.md) | The projects, the directory layout and why they are cut this way |
| [`architecture/data-model.md`](architecture/data-model.md) | One database with a schema and two roles per service, tenant-owned versus platform tables, row-level security, and the Valkey key layout |
| [`architecture/metering.md`](architecture/metering.md) | How a request becomes usage, what is delivered and counted exactly once, and why there is no outbox |
| [`architecture/testing.md`](architecture/testing.md) | Why tests control their clocks, wait for signals and keep the end-to-end tests small |
| [`architecture/supply-chain.md`](architecture/supply-chain.md) | Which images are scanned and how, exceptions in `.trivyignore`, how a tag releases signed images, chiseled own images, and Dependabot updates of the digests |
| [`architecture/adr/`](architecture/adr/README.md) | Architecture decision records |

## Reference — normative specifications

| Document | About |
|---|---|
| [`reference/module-contract/`](reference/module-contract/README.md) | The language-neutral contract every module implements |
| [`reference/problems/`](reference/problems/README.md) | Homeport's problem types, one page each |

## Design

| Document | About |
|---|---|
| [`../design/`](../design/README.md) | The design system: tokens, icons, fonts, logos and illustrations |
