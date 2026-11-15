# The admin API

The admin API is how the operator manages Homeport: tenants, plans,
clients, and reading usage across tenants. The admin portal calls it from
its server, and you may call it yourself to automate
([ADR-0020](../architecture/adr/0020-the-operator-as-a-caller.md)).

It lives on the gateway under `/platform/v1/admin/...`, on a **port of its
own**, the admin port. The gateway decides by the port a request arrives
on, never by its `Host` header:

- On the admin port, only the admin API answers. Every other path — a
  module, the usage export, the API docs, the health endpoints — answers
  `404`.
- On every other port, the admin API does not exist. A path under
  `/platform/v1/admin/` answers `404`, exactly like a path nothing is
  mounted at, whatever token comes with it.

The admin endpoints themselves are served by the platform services; the
ones there are so far are listed under [Endpoints](#endpoints). Any other
admin path answers `404` on the admin port, too.

## Who gets in

Every request on the admin port carries the operator's access token from
your identity provider — the one the portal signs in at with authorization
code and PKCE. For each request the gateway

1. validates the token: signature against the provider's keys, issuer,
   audience and expiry. A request without a valid token is answered with
   `401` and the problem type
   [`unauthorized`](../reference/problems/unauthorized.md). A client token
   for the public endpoint is not valid here;
2. checks the admin claim, type and value, such as `roles` with
   `homeport-admin`. The claim may hold a list; one value must match. A
   token without it, or without a subject, is answered with `403` and the
   problem type
   [`admin-claim-missing`](../reference/problems/admin-claim-missing.md).
   There is one role and no finer permissions;
3. forwards the request to the platform service with a caller token of its
   own in place of the operator's: audience the service, subject the
   operator's subject, the operator's display name for the audit, scope
   `admin`, no tenant and no client, lifetime `ModuleTokens:Lifetime`, at
   most five minutes. The operator's token never reaches the service.

An admin request is neither throttled nor metered: the operator acts for
no tenant.

The platform service checks the caller token once more: it serves its admin
endpoints only on the scope `admin`, with a subject and without a tenant.
Any other token of the gateway, such as the caller token of a client, is
answered with `403` and the problem type
[`insufficient-scope`](../reference/problems/insufficient-scope.md).

## Endpoints

The request and response types are in `Homeport.Shared.Client`, namespace
`Homeport.Shared.Client.Platform.ControlPlane.V1` for the control plane and
`Homeport.Shared.Client.Platform.Registry.V1` for the registry.

| Method and path | Served by | Body | Answer |
|---|---|---|---|
| `GET /platform/v1/admin/tenants` | control plane | — | `200` with every tenant, ordered by name: id, name and number of clients |
| `POST /platform/v1/admin/tenants` | control plane | `{ "name": "Sales" }` | `201` with the new tenant and its address in `Location` |
| `GET /platform/v1/admin/tenants/{tenantId}` | control plane | — | `200` with the tenant, `404` for an unknown id |
| `PUT /platform/v1/admin/tenants/{tenantId}` | control plane | `{ "name": "Sales EMEA" }` | `200` with the renamed tenant, `404` for an unknown id |
| `GET /platform/v1/admin/plans` | control plane | — | `200` with every plan, ordered by name: id, name, requests per second, burst and number of clients |
| `POST /platform/v1/admin/plans` | control plane | `{ "name": "Gold", "requestsPerSecond": 50, "burst": 100 }` | `201` with the new plan and its address in `Location` |
| `GET /platform/v1/admin/plans/{planId}` | control plane | — | `200` with the plan, `404` for an unknown id |
| `PUT /platform/v1/admin/plans/{planId}` | control plane | `{ "name": "Gold", "requestsPerSecond": 80, "burst": 160 }` | `200` with the changed plan, `404` for an unknown id |
| `DELETE /platform/v1/admin/plans/{planId}` | control plane | — | `204`, `404` for an unknown id, `409` for a plan a client uses |
| `GET /platform/v1/admin/clients` | control plane | — | `200` with every registered client, ordered by client id: client id, tenant id and name, plan id and name. `?tenantId=<id>` lists one tenant's clients only |
| `POST /platform/v1/admin/clients` | control plane | `{ "clientId": "billing-service", "tenantId": "…", "planId": "…" }` | `201` with the new client and its address in `Location` |
| `GET /platform/v1/admin/clients/{clientId}` | control plane | — | `200` with the client, `404` for a client that is not registered |
| `PUT /platform/v1/admin/clients/{clientId}` | control plane | `{ "planId": "…" }` | `200` with the client on its new plan, `404` for a client that is not registered |
| `DELETE /platform/v1/admin/clients/{clientId}` | control plane | — | `204`, `404` for a client that is not registered |
| `GET /platform/v1/admin/modules` | registry | — | `200` with every module that has a live instance, ordered by module id: its instances with instance id, address, version, contract version, last heartbeat and when the registration ends, and the routes the gateway serves for it with method, path, scope, meter, quantity and the version of the instance whose declaration applies |

A tenant's name is required, at most 200 characters, and unique among
tenants. Surrounding whitespace is dropped. Names are compared
case-sensitively: `Sales` and `sales` are two tenants. A name that breaks a
rule is answered with `400` and the problem type
[`validation`](../reference/problems/validation.md), the message under
`errors.name`. A tenant is neither deleted nor suspended through the admin
API.

A plan's name follows the same rules among plans. Its requests per second
and its burst are both at least 1; a burst below the rate is allowed. A
value that breaks a rule is answered with `400` and `validation`, the
message under `errors.name`, `errors.requestsPerSecond` or `errors.burst`.
A plan is deleted only while no client uses it; otherwise the answer is
`409` with the problem type
[`plan-in-use`](../reference/problems/plan-in-use.md) and nothing changes.

A client is created at your identity provider; the admin API only maps its
client id to a tenant and a plan, and never creates, changes or deletes a
client at the provider. The client id is required, at most 255 characters
and not yet registered; it is taken exactly as given, without trimming,
since a token names it exactly. The tenant and the plan must exist. A value
that breaks a rule is answered with `400` and `validation`, the message
under `errors.clientId`, `errors.tenantId` or `errors.planId`. In a path,
the client id is escaped as a path segment. A client keeps its tenant: to
move it to another one, remove it and register it again, so the usage it
caused stays with the tenant it was recorded for. Removing a client keeps
its usage, too.

A changed plan, a client moved to another plan, and a client registered or
removed take effect at the gateway once it looks the client up
again: the gateway keeps the plan it looked up with a client for
`ControlPlane:ClientCacheLifetime`, one minute by default (see
[throttling](throttling.md#plan-limits)). Until then a client keeps the
limits it had.

Modules register themselves
([ADR-0004](../architecture/adr/0004-gateway-and-service-discovery.md)); the admin API
only reads what the [registry](registry.md) holds and neither removes an
instance nor manages registration keys. An instance drops out of the list
once its registration ends, 30 seconds after its last heartbeat.

## Audit

Every change through the admin API writes an entry into the audit table of
the service that owns the data, in the same transaction as the change: so
far `control_plane.audit_entries`. An entry records when, the operator's
subject and display name as the caller token named them, what was done
(`tenant.created`, `tenant.renamed`, `plan.created`, `plan.changed`,
`plan.deleted`, `client.registered`, `client.plan-changed`,
`client.removed`), what it was done to (`tenants/<id>`, `plans/<id>`,
`clients/<client id>`) and a sentence for the reader with the values before
and after. Renaming a tenant to the name it already has, saving a plan
unchanged, or moving a client to the plan it is on changes nothing and
writes no entry; neither does a change the admin API refuses. Reading the
modules writes none either: a registration is the platform's data, not a
tenant's.

The service's app role may only add entries and read them; it can neither
change nor delete one. Homeport never deletes an entry. The entries hold
personal data — the operator's subject and name — so delete them after a
period of your own choosing, as the schema's owner role:

```sql
-- as control_plane_owner
DELETE FROM control_plane.audit_entries WHERE "OccurredAt" < now() - interval '1 year';
```

## Configuration

| Setting | Environment variable | Default | Meaning |
|---|---|---|---|
| `AdminApi:Port` | `AdminApi__Port` | — | The admin port. It must be one of the ports the gateway listens on, set with `HTTP_PORTS` or `ASPNETCORE_URLS`. Unset, the admin API is off |
| `AdminApi:Authority` | `AdminApi__Authority` | — | The issuer of the operator's tokens. The gateway reads the provider's metadata at `<authority>/.well-known/openid-configuration` |
| `AdminApi:Audience` | `AdminApi__Audience` | — | The audience the operator's token must carry: the portal's client at your provider, or an audience you map into its tokens |
| `AdminApi:ClaimType` | `AdminApi__ClaimType` | `roles` | The type of the claim that grants admin access |
| `AdminApi:ClaimValue` | `AdminApi__ClaimValue` | `homeport-admin` | The value that claim must carry |
| `AdminApi:NameClaim` | `AdminApi__NameClaim` | `name` | The claim the operator's display name is taken from. Without it in a token, the subject stands in |
| `AdminApi:RequireHttpsMetadata` | `AdminApi__RequireHttpsMetadata` | `true` | Whether the metadata must be fetched over HTTPS. Turn it off only for a development provider without TLS |
| `PlatformRoutes:ControlPlaneUrl` | `PlatformRoutes__ControlPlaneUrl` | `http://control-plane` | Where the gateway forwards the tenants, plans and clients. The AppHost and the compose file set the control plane's endpoint |
| `PlatformRoutes:RegistryUrl` | `PlatformRoutes__RegistryUrl` | `http://registry` | Where the gateway forwards the modules. The AppHost and the compose file set the registry's endpoint |

With a port, the gateway does not start without an authority and an
audience. Its start is logged at Information with the admin port, the
authority, the audience and the claim, or with `admin API off`.

The compose file configures the admin port as `8000`:

```yaml
HTTP_PORTS: "${GATEWAY_PORT};8000"
AdminApi__Port: "8000"
```

In the AppHost the admin port is the gateway's endpoint `admin`, on a port
of its own for every replica.

## Keep it unpublished

The compose file **exposes** the admin port on the compose network and does
not **publish** it: nothing outside the network reaches it, and neither does
your reverse proxy unless you send it there. Keep it that way, and route
only the gateway's public port through the proxy that faces your clients
(see [the reverse proxy](reverse-proxy.md)).

The portal reaches the admin port on the network it shares with the
gateway. To automate against the admin API from outside it, publish the
port on purpose, to a network only operators reach — for example with an
override file next to the compose file:

```yaml
# docker-compose.override.yaml
services:
  gateway:
    ports:
      - "127.0.0.1:8000:8000"
```

## What is logged

At Warning, at the platform service: a caller token refused for an admin
endpoint, with the reason, never the token. At Information: a tenant created
or renamed, with its id.

At Warning, at the gateway, without the token: a rejected operator token with the reason
(the type of the validation failure), and a token without the admin claim,
with the claim it lacks, or without a subject. At Debug: a request answered
`404` because it came to the wrong port, with its path and port, and a
token without the name claim.
