# Module contract

**Status: draft, `contractVersion` 0.** Normative once v0.1 ships. Until then
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
| `version` | The module's own version |
| `contractVersion` | The version of this contract the module implements |
| `routes[]` | `method`, `path`, required `scope`, `meter` and `quantity` per route |
| `scopes[]` | The OAuth scopes the module defines |
| `meters[]` | The meters the module counts in, for example `requests`, `messages`, `documents` |
| `openApiUrl` | Where the module's OpenAPI document is served |

## Health

- `GET /health/live`
- `GET /health/ready`

## Registration and heartbeat

- On start, the module registers at the registry with its manifest and its
  instance address.
- It then sends a heartbeat, by default every 10 s. An instance without a
  heartbeat for 30 s (TTL) is removed from routing.
- If the registry is unavailable, the gateway keeps serving the last known
  routes.

## Requests from the gateway

- Modules are reachable only through the gateway, on the internal network.
- The gateway strips every incoming `X-Homeport-*` header and sets:
  - `X-Homeport-Tenant-Id`
  - `X-Homeport-Client-Id`
- W3C `traceparent` is propagated.

## Metering

- The gateway records one usage event per request, using the meter and
  quantity of the matched route.
- A module may report a different quantity with the response header
  `X-Homeport-Usage: <meter>=<quantity>`.
- Asynchronous workers publish usage through the SDK.
