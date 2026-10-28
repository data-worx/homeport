# Calling a module

Every module is reached through the gateway, the one entry point of a Homeport
installation. A module has no address of its own that a caller could use.

## The URL

```text
https://<gateway>/modules/{moduleId}/{path}
```

- **`<gateway>`** is the address the operator gives you.
- **`{moduleId}`** names the module, such as `echo`. It is the `id` the module
  declares in its manifest.
- **`{path}`** is the route inside the module, as its OpenAPI document lists
  it. It always starts with the major version of the module's API, such as
  `/v1/echo`.

The gateway removes `/modules/{moduleId}` and forwards the rest. A request to
`/modules/echo/v1/echo` reaches Echo as `/v1/echo`. A path that names no
module is answered with `404`.

The module id `platform` is reserved: `/platform/{path}` leads to Homeport's
own endpoints, never to a module. See
[ADR-0013](../architecture/adr/0013-url-scheme-and-api-versioning.md).

## Versions

A module keeps a major version of its API backwards compatible. A breaking
change appears as a new major version next to the old one — `/v2/…` beside
`/v1/…` — under the same module id, so you move when you are ready.

## What the gateway changes on the way

- **`X-Homeport-*` headers are removed.** Only the gateway sets them; one you
  send never reaches the module.
- **`traceparent` is kept.** Send a W3C `traceparent` and the module's work
  joins your trace.
- Everything else — method, query, body, content type and your other headers —
  is forwarded unchanged.

## Try it with Echo

Echo returns whatever it is sent. With Homeport running locally
([running Homeport locally](../operators/running-locally.md)), and
`<gateway>` the gateway's address from there:

```bash
curl -s -X POST -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' http://<gateway>/modules/echo/v1/echo
```

```json
{"hello":"Homeport"}
```

`GET /modules/echo/v1/request` answers with the headers Echo received, which
shows what the gateway forwards.

The gateway does not authenticate callers yet and accepts these calls without
a token. Client credentials follow before the v0.1 release.
