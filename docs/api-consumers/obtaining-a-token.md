# Obtaining a token

Every call to a module carries a bearer token. Homeport has no credentials
of its own: you get the token from the identity provider your operator runs,
with the OAuth 2.0 client-credentials grant, and send it to the gateway.

## What you need from the operator

- **The token endpoint** of their identity provider.
- **A client id and a client secret.** The client is a machine identity of
  your tenant; the operator has mapped its client id to that tenant in
  Homeport.
- **The audience or scope** that makes the identity provider issue a token
  for Homeport, if it needs one.
- **The scopes your client may get.** Every route of a module requires one
  scope, which the module's manifest names, such as `echo.invoke`. Some
  providers grant a client its scopes by default; others issue a scope only
  when the token request asks for it with `scope`.

## Getting the token

```bash
curl -s \
  -d grant_type=client_credentials \
  -d client_id=<client id> \
  -d client_secret=<client secret> \
  <token endpoint>
```

The answer carries the token in `access_token` and its lifetime in seconds
in `expires_in`. Reuse the token until shortly before it expires rather than
asking for one per call.

## Calling a module with it

```bash
curl -s -X POST \
  -H "Authorization: Bearer <access token>" \
  -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' \
  https://<gateway>/modules/echo/v1/echo
```

The gateway checks the token on every call:

| Status | Type | Means |
|---|---|---|
| `401` | [`unauthorized`](../reference/problems/unauthorized.md) | No token, or one that is expired, signed by another issuer or meant for another audience |
| `403` | [`unknown-client`](../reference/problems/unknown-client.md) | The token is valid, but its client is not mapped to a tenant |
| `403` | [`insufficient-scope`](../reference/problems/insufficient-scope.md) | The client is known, but the token lacks the scope the route requires |
| `503` | [`control-plane-unavailable`](../reference/problems/control-plane-unavailable.md) | The gateway could not look the client up; retry later |

A `401` or `403` from the gateway means the request never reached the module.
What the gateway does with the token on the way to the module is drawn in
[the request flow](../architecture/request-flow.md#a-call-to-a-module).

## Try it locally

With Homeport running locally ([running Homeport locally](../operators/running-locally.md)),
Keycloak plays the identity provider. Its realm `homeport` has the client
`local-client`, which the control plane maps to the local tenant. It gets
`echo.invoke` only when the token request asks for it. Its secret
is the AppHost parameter `local-client-secret`; with Docker Compose it is
`LOCAL_CLIENT_SECRET` in `deploy/compose/.env`.

From the repository root, with the AppHost running, and `<gateway>` the
gateway's address from the dashboard:

```bash
secret="$(dotnet user-secrets list --project src/Aspire/Homeport.Aspire.AppHost \
  | sed -n 's/^Parameters:local-client-secret = //p')"
token="$(curl -s \
  -d grant_type=client_credentials \
  -d client_id=local-client \
  -d "client_secret=${secret}" \
  -d scope=echo.invoke \
  http://localhost:8080/realms/homeport/protocol/openid-connect/token \
  | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"
curl -s -X POST \
  -H "Authorization: Bearer ${token}" \
  -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' \
  http://<gateway>/modules/echo/v1/echo
```

```json
{"hello":"Homeport"}
```

The same call without the `Authorization` header is answered with `401`,
and with a token requested without `scope=echo.invoke` with `403`.
