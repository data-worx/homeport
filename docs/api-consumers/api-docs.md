# API docs

Where the operator has switched them on, the gateway shows every API of the
installation in one place, at `/platform/docs`:

```text
https://<gateway>/platform/docs
```

The page needs no token. It lists one document per module and one for the
platform; pick one in the selector at the top left. See
[ADR-0017](../architecture/adr/0017-consolidated-api-docs.md).

When the page answers `404`, the docs are off in this installation; ask the
operator. They are on by default only in a local run.

## What a module's document shows

Exactly what you can call through the gateway:

- **Public paths.** Every path starts with `/modules/<module id>/`, as you
  call it, and the server is the gateway itself; see
  [calling a module](calling-a-module.md).
- **Only routed operations.** An operation the module describes but does not
  declare as a route is left out: the gateway would answer it with `404`.
- **The newest description of each route.** While two versions of a module
  run side by side, each route is shown as the instance with the highest
  version describes it, the instance whose attributes the gateway applies.
- **Renamed schemas, rarely.** Where two versions describe a schema of the
  same name differently, the newer one keeps the name and the other is
  prefixed with its version, such as `v1.2.0_Order`.

The documents themselves are at
`https://<gateway>/platform/docs/openapi/<module id>.json` and
`…/openapi/platform.json`, for a code generator or any other OpenAPI tool.

## Trying a request

Every operation requires a bearer token. Get one from the identity provider
as [obtaining a token](obtaining-a-token.md) describes, with `curl`, and print
it:

```bash
curl -s \
  -d grant_type=client_credentials \
  -d client_id=<client id> \
  -d client_secret=<client secret> \
  -d scope=<scope> \
  <token endpoint> \
  | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p'
```

Paste the printed token into **Bearer Token** under **Authentication** and
send the request with **Test Request**. The request goes from your browser
straight to the gateway, under the path shown.

The page never asks for a client secret, and it keeps the token only while
the page is open. A token expires, typically after a few minutes; get a new
one when the gateway answers
[`unauthorized`](../reference/problems/unauthorized.md).

Locally, with Keycloak and the client `local-client`, the token request of
[obtaining a token](obtaining-a-token.md#try-it-locally) works unchanged; the
dashboard links the page as `API docs` on the gateway's row.
