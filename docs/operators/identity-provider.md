# Connecting an identity provider

Homeport keeps no credentials of its own. The clients of your tenants
authenticate at your identity provider — Entra ID, Keycloak or any other
OpenID Connect provider — with the OAuth 2.0 client-credentials grant, and
call the gateway with the token they get
([ADR-0006](../architecture/adr/0006-authentication.md)).

For every call to a module the gateway

1. validates the token: signature against the provider's keys, issuer,
   audience and expiry. Anything else is answered with `401`;
2. reads the client id from the token and asks the control plane which
   tenant and plan the client belongs to. A client the control plane does
   not know is answered with `403`.

Keycloak in the AppHost and the compose file is for development and tests
only. Connect your own provider in production.

## Configuration

| Setting | Environment variable | Default | Meaning |
|---|---|---|---|
| `ClientAuthentication:Authority` | `ClientAuthentication__Authority` | — | The issuer of the tokens. The gateway reads the provider's metadata at `<authority>/.well-known/openid-configuration` and its signing keys from there |
| `ClientAuthentication:Audience` | `ClientAuthentication__Audience` | — | The audience a token must carry. Configure the provider to issue it to Homeport's clients |
| `ClientAuthentication:ClientIdClaim` | `ClientAuthentication__ClientIdClaim` | `azp` | The claim that carries the client id. Keycloak uses `azp` (and `client_id`); Entra ID uses `azp` in v2 tokens and `appid` in v1 tokens |
| `ClientAuthentication:RequireHttpsMetadata` | `ClientAuthentication__RequireHttpsMetadata` | `true` | Whether the metadata must be fetched over HTTPS. Turn it off only for a development provider without TLS |
| `ControlPlane:ClientCacheLifetime` | `ControlPlane__ClientCacheLifetime` | `00:01:00` | How long the gateway remembers which tenant a client belongs to, and that a client is unknown |

The gateway does not start without an authority and an audience. Its start
is logged at Information with the authority, the audience, the client id
claim and the cache lifetime.

## Mapping clients to tenants

Register a client at your provider for every machine identity of a tenant,
then map its client id — the value of the client id claim — to the tenant in
the control plane. Until the admin portal ships, the control plane knows
only the client it seeds; see [the control plane](control-plane.md).

The gateway keeps the answer for a client for the cache lifetime. A client
you map takes effect at most that long after it was first refused; a client
you remove can still call for at most that long.

## When the control plane is down

A client the gateway looked up within the cache lifetime keeps working. Any
other is answered with `503` and the problem type
[`control-plane-unavailable`](../reference/problems/control-plane-unavailable.md),
and the gateway logs a warning.

## What is logged

At Warning, without the token: a rejected token with the reason (the type of
the validation failure), a token without the client id claim, and a client
the control plane does not know, with its client id.

## Keycloak in development

The AppHost imports the realm in
[`deploy/keycloak/realms/`](../../deploy/keycloak/realms/homeport-realm.json):

- the realm `homeport`, with the client scopes `echo.invoke` and
  `usage.read`;
- the client the control plane seeds (`local-client` by default, the
  parameter `seed-client-id`), confidential, with only the client-credentials
  grant, both scopes and the audience `homeport`;
- the client `unmapped-client`, which the control plane does not know, for
  the tests that expect `403`.

The client secrets are not in the realm file. The AppHost generates them as
the parameters `local-client-secret` and `unmapped-client-secret`; the
compose file takes them from `LOCAL_CLIENT_SECRET` and
`UNMAPPED_CLIENT_SECRET`.

Keycloak's issuer is `http://localhost:8080/realms/homeport` in both setups,
so a token a client gets on the host is valid at the gateway. On the compose
network the gateway fetches the metadata and keys from `keycloak:8080`.
