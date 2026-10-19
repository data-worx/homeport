# Roadmap

Where homeport is going, in coarse phases. No dates, no estimates.
Each phase is broken into tickets when it is picked up — this file stays the
big picture, the tickets are the work.

Phases are ordered by dependency: a later phase assumes the earlier ones are
in place. Open questions live in GitHub Discussions.

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
4. Exceeding the plan's rate limit returns `429` with `Retry-After`, also with
   two gateway replicas.
5. Every call produces a `UsageRecorded` event that ends up aggregated in
   PostgreSQL. Duplicate events are not counted twice. A CSV export endpoint
   works.
6. One trace shows gateway → Echo → RabbitMQ → metering.
7. CI: build, tests, SBOM, Trivy scan of all images, cosign signing, push to
   GHCR on tag, nightly scan.
8. `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md` (with the CLA) and a
   `README.md` with a quickstart.

Out of scope: portal, API keys, real modules, Helm chart, invoices.

## Later — not yet phased

- Admin portal
- API keys
- First real module, chosen by the community poll in GitHub Discussions
- Helm chart
