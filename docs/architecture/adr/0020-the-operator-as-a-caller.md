# ADR-0020: The operator as a caller

- Status: Accepted
- Date: 2026-11-12

## Context

The admin portal (ADR-0010) is the first caller of the platform services
besides the gateway, and the first human one. ADR-0014 lets a new caller in
only through an ADR that authenticates it as itself, with no network trust
and no shared secret.

The portal manages tenants, plans and clients in the control plane, shows
the registered modules, and reads usage across tenants from metering. So it
reaches more than one service, and some of what it reads is tenant data,
which row-level security (ADR-0005) confines to one tenant per session.

The operator's identity provider already issues tokens for clients
(ADR-0006). Homeport runs inside the operator's network, behind a reverse
proxy the operator controls.

## Decision

**Sign-in.**

- The operator signs in at the portal with OIDC, authorization code with
  PKCE, at the operator's identity provider.
- A configurable claim, type and value, such as `roles` = `homeport-admin`,
  grants access. There is one role and no finer permissions.

**Path.**

- The portal is Blazor Server. It calls the admin API only from the server,
  never from the browser, with the operator's access token.
- The admin API lives on the gateway under `/platform/v1/admin/...`. The
  portal talks to the gateway only, not to the control plane directly; this
  supersedes the API path of ADR-0010.

**Admin port.**

- Admin routes answer only on a second endpoint of the gateway. On the
  public endpoint they do not exist and answer like any unknown path.
- The compose file does not publish the admin port. An operator who wants
  to automate against the admin API publishes it on purpose.

**Caller token.**

- The gateway validates the operator's token against the identity provider
  and checks the admin claim.
- It forwards with a caller token as in ADR-0019: audience the platform
  service, subject the operator's subject, scope `admin`, no tenant claim,
  lifetime at most 5 minutes. It carries the operator's display name for
  the audit entry.
- A service serves its admin endpoints on that scope only. A token with a
  tenant claim, or a service token, does not reach them.

**Tenant data.**

- An admin read across tenants, such as usage, is the only code path
  allowed to bypass query filters and row-level security.
- It runs as a platform-admin database role, one per service that owns
  tenant data: `<schema>_admin`, the first being `metering_admin`. The
  scripts in `deploy/postgres/init/` create it next to the owner and app
  roles.
- The role reads every tenant's rows through a row-level security policy
  for that role. It does not get `BYPASSRLS`: granting that needs a
  superuser, and it lifts row-level security on every table, not only on
  the ones the role is meant to read.
- It gets `SELECT` on what it reads and `INSERT` and `SELECT` on the
  service's audit table, nothing else.
- The init scripts run only on an empty volume. An existing installation
  creates the role by hand when it upgrades, as the upgrade documentation
  describes.

**Audit.**

- Every admin change and every admin read of tenant data writes an audit
  entry in the same transaction, into a platform table
  `<schema>.audit_entries` of the service that owns the data.
- An entry records the operator's subject and display name as they were at
  the time of the action, so it stays readable after the user is gone from
  the identity provider. The claim the name is taken from is configurable.
- The service's app role has `INSERT` and `SELECT` on the table, nothing
  else. An integration test asserts that.
- Homeport never deletes audit entries. They hold personal data; the
  operator deletes them after a period of their own choosing, with the
  schema's owner role, as the operator documentation describes.

**Edge.**

- TLS ends at the operator's reverse proxy. The portal trusts forwarded
  headers only from configured proxies.
- The portal sets its cookies `Secure`, `HttpOnly` and `SameSite=Lax`, and
  refuses to sign in over plain HTTP.
- TLS and mTLS between components stay open in
  [discussion #4](https://github.com/data-worx/homeport/discussions/4).

**Error responses.**

- As a client of the admin API, the portal reads Problem Details
  (ADR-0016) with the `Problems` read model of `Homeport.Shared.Client`. It
  shows `title` and `detail`, puts the entries of `errors` at their form
  fields, and shows the `traceId`.
- As a server for a browser, the portal answers its own HTTP errors —
  access denied, not found, an unexpected error — with an HTML page that
  shows the trace id. This is an exception to ADR-0016, limited to HTML
  responses of the portal to a browser. The admin API behind the gateway
  stays Problem Details.

This supplements ADR-0014 and ADR-0019 and supersedes the API path of
ADR-0010.

## Consequences

- A second caller exists without a second mechanism: the operator gets the
  caller token every forwarded request gets, distinguished by its scope.
- The admin API cannot be reached from the public endpoint, whatever a
  misconfigured route or a missing check would allow.
- Every service that owns tenant data gets one more role, a row-level
  security policy for it and an audit table, and upgrades of an existing
  installation take a manual step for the role.
- Who changed or read what is answered from the database, per service.
  There is no single audit log across services; the portal combines them.
- Audit entries grow without bound until the operator deletes them, and
  they are personal data the operator is responsible for.
- Bypassing tenant isolation is confined to one role per service and one
  scope, both of which a review can find.
