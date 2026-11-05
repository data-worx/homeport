# unknown-module

`https://homeport.data-worx.de/problems/unknown-module` — status `404`.

The gateway answered: no route matches the path, so the request never
reached a module. The module id in `/modules/{moduleId}/…` is misspelled, the
module is not installed in this Homeport installation or has no running
instance, or the module does not declare the path below its id. The gateway
routes only the paths a module declares in its manifest. A caller without a
valid token gets [`unauthorized`](unauthorized.md) instead.

```json
{
  "type": "https://homeport.data-worx.de/problems/unknown-module",
  "title": "No module is mounted at this path.",
  "status": 404,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

A `404` from a module itself, such as for a record that does not exist, never
carries this type. Check the module id against what the operator gave you,
and the path against the module's OpenAPI document; the same request fails
the same way until the module is installed and running.
