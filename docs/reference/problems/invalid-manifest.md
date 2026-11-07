# invalid-manifest

`https://homeport.data-worx.de/problems/invalid-manifest` — status `422`.

The registry refused a registration: the manifest breaks the
[module contract](../module-contract/README.md). The extension `errors` lists
every failure, keyed by the field path in the manifest, in the notation of
[`validation`](validation.md):

```json
{
  "type": "https://homeport.data-worx.de/problems/invalid-manifest",
  "title": "The manifest breaks the module contract.",
  "status": 422,
  "errors": {
    "contractVersion": ["Contract version 99 is not supported; supported versions: 1."],
    "routes[0].path": ["Path '/echo' must start with /v{major}/."]
  },
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

- An unsupported `contractVersion` names the versions the registry supports:
  the current one and, once there is one, the previous.
- `id` is reported when it is not lower-case letters, digits and `-`, when it
  is the reserved `platform`, or when it differs from the module id the
  registration is made for.
- Every route path starts with `/v{major}/`; its scope and meter are among
  the manifest's `scopes` and `meters`.
- `openApiUrl` is an absolute path below the instance's address, such as
  `/openapi/v1.json`: no scheme, no host, no query.

The instance is not registered. Fix the manifest and start the module again;
the same manifest fails the same way. A module built with `Homeport.Sdk` logs
the errors and stops.
