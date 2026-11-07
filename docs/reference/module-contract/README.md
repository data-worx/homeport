# Module contract

**Status: normative, `contractVersion` 1**, the version released with v0.1.
Every change bumps it and updates the SDK, this documentation and the Echo
module in the same pull request. The drafts before it carried version 0 and
were never released.

The contract is language-neutral HTTP. A module may be written in any language;
the .NET SDK (`Homeport.Sdk`) is a convenience, not a requirement.

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
| `openApiUrl` | Where the module's OpenAPI document is served: an absolute path below the instance's address, such as `/openapi/v1.json` |

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
- When the registry answers a heartbeat with `404`, it no longer knows the
  instance, and the instance registers again.
- Registration and every heartbeat carry the module's secret registration
  key as `Authorization: Bearer <key>`. The operator configures the key for
  the module id at the registry. An unknown module id or a wrong key is
  rejected with `401`. Up to two keys per module id are valid at once, so a
  key rotates without downtime.
- Registration and heartbeat carry no version in the path; the
  `contractVersion` in the manifest decides. The registry accepts the
  current and the previous version and rejects any other with `422`, naming
  the supported versions.
- If the registry is unavailable, the gateway keeps serving the last known
  routes.

The two calls, made to the registry directly on the internal network, never
through the gateway. `{instanceId}` is chosen by the instance, unique per
running instance, so instances of one module never replace each other.

| Call | Body | Answers |
|---|---|---|
| `PUT /instances/{moduleId}/{instanceId}` | `{ "address": "<url>", "manifest": { … } }` | `204` registered; `400` [`validation`](../problems/validation.md); `401` [`unauthorized`](../problems/unauthorized.md); `422` [`invalid-manifest`](../problems/invalid-manifest.md) |
| `POST /instances/{moduleId}/{instanceId}/heartbeat` | none | `204` renewed; `401` [`unauthorized`](../problems/unauthorized.md); `404` unknown instance, register again |

- `address` is where the gateway reaches the instance, an absolute URL such
  as `http://echo-1:8080`.
- `manifest` is the manifest as the module serves it, `contractVersion`
  included. Its `id` must equal `{moduleId}`.
- `PUT` is idempotent: registering again under the same `{instanceId}`
  replaces the registration and renews it.

## Routing

- Callers reach a module under `/modules/{id}/{path}`. The gateway strips
  `/modules/{id}`; the module sees only `{path}`.
- The gateway forwards only the routes the manifests declare, by method and
  path. Any other path below `/modules/{id}` is answered by the gateway.
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
  token in `Authorization`. The caller's own token never reaches the module.
  The module token is a JWT signed with ES256, with these claims:

  | Claim | Value |
  |---|---|
  | `iss` | `homeport-gateway` |
  | `aud` | The module `id` |
  | `sub` | The subject of the caller's token; the client id where it has none |
  | `tenant_id` | The tenant the client acts for, a UUID |
  | `client_id` | The client id as the identity provider issued it |
  | `scope` | The scopes the caller's token grants, space-delimited |
  | `iat`, `nbf`, `exp` | Issued, valid from, expires; 60 s by default, at most 5 minutes |

- The gateway forwards a request only when the caller's token grants the
  `scope` its route declares; it answers every other with `403`.
- The module validates the token against the gateway's JWKS, served at
  `/.well-known/jwks.json`, and rejects a request without a valid token
  with `401`, except on the manifest, OpenAPI and health endpoints.
  The token is the only identity source the module may trust; it authorises
  per endpoint from the claims.
- The module never talks to the identity provider.
- W3C `traceparent` is propagated.

## Errors

- A module answers every 4xx and 5xx response as Problem Details
  (RFC 9457), media type `application/problem+json`. That includes
  unhandled exceptions and status codes without a body of their own, such
  as `404` for an unknown path.
- `type` is an absolute URI. Homeport's own types live under
  `https://homeport.data-worx.de/problems/<name>` and are listed in
  [problem types](../problems/README.md). A module defines its own types
  under a prefix of its own.
- A request that fails validation is answered with
  [`validation`](../problems/validation.md): status `400` and the extension
  `errors`, an object of field path to messages. A field path uses the
  JSON notation of the request: camelCase, such as `items[0].name`.
- An unexpected failure is answered with
  [`internal`](../problems/internal.md), status `500`, and nothing about
  its cause.
- `title` and `detail` are English.
- The extension `traceId` carries the W3C trace id of the request.
- The gateway passes a conforming 4xx through and sets `traceId`. It
  replaces the body of every 5xx, conforming or not, and of every 4xx that
  is not Problem Details, so the caller loses the module's `detail`.

## Metering

- The gateway records one usage event per request, using the meter and
  quantity of the matched route.
- A module may report a different quantity with the response header
  `X-Homeport-Usage: <meter>=<quantity>`, at most one per response. The
  meter is the one the matched route declares, compared exactly; the
  quantity is a positive integer in decimal digits, without sign.
- The gateway removes `X-Homeport-Usage` before the response reaches the
  caller. A report that breaks a rule above is ignored, and the route's
  quantity applies.
