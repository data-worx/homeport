# Roadmap

Where homeport is going, in coarse phases. No dates, no estimates.
Each phase is broken into tickets when it is picked up — this file stays the
big picture, the tickets are the work.

Phases are ordered by dependency: a later phase assumes the earlier ones are
in place. Open questions live in GitHub Discussions.

## M0 — Groundwork

The repository and the organisation around it, before the first line of
product code. Nothing of the product runs at the end of M0.

Done when:

1. The maintainer's setup is in place: `homeport.data-worx.de` resolves to
   GitHub Pages, `security@data-worx.de` receives mail, and the CLA workflow
   has its signatures token.
2. The Homeport design kit — tokens, icons, logos, illustrations and fonts —
   lives under `design/`.
3. The design kit's generated files are committed under `design/` and are
   rebuilt with `node design/tools/build-kit.mjs` after every change to a
   source.
4. `TRADEMARKS.md` states how the names "Homeport" and "data-worx" and their
   logos may be used.
5. `github.com/data-worx` shows an organisation profile with the data-worx
   branding.

Out of scope: any product code.

## M1 — v0.1 Walking skeleton

One thin path that works end to end. No portal, no API keys, no real module.

Done when:

1. `docker compose up` (generated from the AppHost) starts gateway, registry,
   control plane, metering, Echo module, PostgreSQL, RabbitMQ, Valkey and
   Keycloak. `--profile observability` adds the Grafana stack.
2. Echo registers with its manifest and sends heartbeats. Its route appears in
   the gateway without a restart. Stopping Echo removes the route after the
   TTL.
3. A seeded tenant and client exist (seed data and Keycloak realm import).
   `curl` with a client-credentials token reaches Echo.
4. Echo accepts only requests that carry a valid gateway-issued token
   ([ADR-0011](docs/architecture/adr/0011-gateway-issued-tokens-for-modules.md));
   a request without one is rejected with `401`.
5. Exceeding the plan's rate limit returns `429` with `Retry-After`, also with
   two gateway replicas.
6. Every call produces a `UsageRecorded` event that ends up aggregated in
   PostgreSQL. Duplicate events are not counted twice. The usage export works
   as CSV and JSON, for the calling tenant only.
7. One trace shows gateway → Echo → RabbitMQ → metering.
8. CI: build, tests, SBOM, Trivy scan of all images, cosign signing, push to
   GHCR on tag, nightly scan.
9. `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md` (with the CLA) and a
   `README.md` with a quickstart.

Out of scope: portal, API keys, real modules, Helm chart, invoices, quotas,
the period-close webhook, a write API for the control plane, usage reporting
from asynchronous workers.

## Later — not yet phased

- Admin portal
- API keys
- First real module, chosen by the community poll in GitHub Discussions
- Helm chart
- Quotas
- Period-close webhook
- Usage reporting from asynchronous workers
- `Deprecation` and `Sunset` headers for module routes
- Operator-defined module groups under `/modules/`
- In-product developer portal: a catalogue of the modules registered in an
  installation, built from their manifests and OpenAPI documents
