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

Done: released as v0.1.0.

One thin path that works end to end. No portal, no API keys, no real module.

Done when:

1. `docker compose up` (generated from the AppHost) starts gateway, registry,
   control plane, metering, Echo module, PostgreSQL, RabbitMQ, Valkey and
   Keycloak.
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
7. One trace shows gateway → Echo → RabbitMQ → metering in the Aspire
   dashboard.
8. CI: build, tests, SBOM, Trivy scan of all images, cosign signing, push to
   GHCR on tag, nightly scan.
9. `LICENSE`, `SECURITY.md`, `CONTRIBUTING.md` (with the CLA) and a
   `README.md` with a quickstart.

Out of scope: portal, API keys, real modules, Helm chart, invoices, quotas,
the period-close webhook, an observability profile for the compose
deployment, a write API for the control plane, usage reporting from
asynchronous workers.

## M2 — v0.2 Admin portal

The operator manages tenants, plans and clients in a web portal and reads
modules, usage and audit across tenants, without touching the database or
the seed data.

Done when:

1. The portal is for the operator only. The operator signs in with OIDC —
   authorization code with PKCE — at their own identity provider, and a
   configurable claim grants access. There is one role.
2. The portal is Blazor Server with MudBlazor, its theme generated from the
   design tokens. Its user interface is in English only.
3. The portal reaches the platform services only through an admin API on the
   gateway, on a separate port that the compose file does not publish.
4. Tenants can be listed, created and renamed.
5. Plans can be listed, created and changed, and deleted while no tenant
   uses them.
6. Clients are registered by a client id the identity provider already
   issued, with a tenant and a plan. A client's plan can be changed, and a
   client can be removed.
7. The registered modules can be read.
8. Usage can be read across tenants and exported.
9. Every admin change and every admin read of tenant data is audited in the
   same transaction, append-only, in the service that owns the data. The
   audit can be read in the portal.
10. TLS ends at the operator's reverse proxy. The portal trusts forwarded
    headers only from configured proxies and refuses to sign in over plain
    HTTP.
11. Upgrading from v0.1.0 is documented, and tested per schema: migrate to
    v0.1.0, seed, migrate to the latest version.
12. The portal image is scanned, listed in the third-party notices and signed
    like every own image.

Out of scope: tenant self-service, suspending or deleting a tenant, creating
clients at the identity provider, API keys, more than one role, active
invalidation of the gateway's cached client lookups, dashboards, localisation,
TLS between components
([discussion #4](https://github.com/data-worx/homeport/discussions/4)).

## Later — not yet phased

- API keys
- Tenant self-service
- Suspending or deleting a tenant
- Active invalidation of the gateway's cached client lookups
- First real module, chosen by the community poll in
  [discussion #2](https://github.com/data-worx/homeport/discussions/2);
  decided when M2 is released
- Helm chart
- Quotas
- Period-close webhook
- Usage reporting from asynchronous workers
- Observability profile for the compose deployment: Grafana with a Homeport
  dashboard
- `Deprecation` and `Sunset` headers for module routes
- Operator-defined module groups under `/modules/`
- In-product developer portal: a catalogue of the modules registered in an
  installation, built from their manifests and OpenAPI documents
- Typed .NET client in `Homeport.Shared.Client`: calls the platform and
  catalog module APIs and obtains tokens with client credentials
  ([ADR-0018](docs/architecture/adr/0018-where-wire-types-live.md))
