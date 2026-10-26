# Module contract

**Status: draft, `contractVersion` 0.** Normative once v0.1 ships, which sets
`contractVersion` to 1; from then on every change bumps it. Until then
it changes freely, but every change still updates the SDK and the Echo module
in the same pull request.

The contract is language-neutral HTTP. A module may be written in any language;
the .NET SDK (`Homeport.Module.Sdk`) is a convenience, not a requirement.

## Manifest

`GET /.well-known/homeport/manifest` returns:

| Field | Meaning |
|---|---|
| `id` | Stable module id, lowercase, used in routes, metrics and `service.name` |
| `name` | Display name |
| `version` | The module's own version, SemVer |
| `contractVersion` | The version of this contract the module implements, an integer |
| `routes[]` | `method`, `path`, required `scope`, `meter` and `quantity` per route |
| `scopes[]` | The OAuth scopes the module defines |
| `meters[]` | The meters the module counts in, for example `requests`, `messages`, `documents` |
| `openApiUrl` | Where the module's OpenAPI document is served |

Every route `path` starts with `/v{major}/`, for example `/v1/echo`. Minor
changes stay backwards compatible; a breaking change is a new major version
next to the old one. The module id `platform` is reserved.

## Health

- `GET /health/live`
- `GET /health/ready`

## Registration and heartbeat

- On start, the module registers at the registry with its manifest and its
  instance address.
- It then sends a heartbeat, by default every 10 s. An instance without a
  heartbeat for 30 s (TTL) is removed from routing.
- Registration and heartbeat carry no version in the path; the
  `contractVersion` in the manifest decides. The registry accepts the
  current and the previous version and rejects any other with `422`, naming
  the supported versions.
- If the registry is unavailable, the gateway keeps serving the last known
  routes.

## Routing

- Callers reach a module under `/modules/{id}/{path}`. The gateway strips
  `/modules/{id}`; the module sees only `{path}`.
- A module's route table is the union of the manifests of its live
  instances. Each route goes only to the instances that declare it.
- Where instances declare the same route with different attributes, those
  of the instance with the highest `version` apply.
- This lets a module be updated instance by instance, and run two major
  versions of its API side by side under one id.

## Requests from the gateway

- Modules are reachable only through the gateway, on the internal network.
- The gateway strips every incoming `X-Homeport-*` header.
- Every request carries a short-lived token signed by the gateway, as a bearer
  token in `Authorization`:
  - audience: the module `id`
  - claims: tenant, client, subject and scopes
- The module validates the token against the gateway's JWKS, served at
  `/.well-known/jwks.json`, and rejects a request without a valid token.
  The token is the only identity source the module may trust; it authorises
  per endpoint from the claims.
- The module never talks to the identity provider.
- W3C `traceparent` is propagated.

## Metering

- The gateway records one usage event per request, using the meter and
  quantity of the matched route.
- A module may report a different quantity with the response header
  `X-Homeport-Usage: <meter>=<quantity>`.
