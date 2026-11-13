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

The admin endpoints themselves are served by the platform services and
arrive with the admin portal. Until then, every admin path answers `404` on
the admin port, too.

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

At Warning, without the token: a rejected operator token with the reason
(the type of the validation failure), and a token without the admin claim,
with the claim it lacks, or without a subject. At Debug: a request answered
`404` because it came to the wrong port, with its path and port, and a
token without the name claim.
