# unknown-module

`https://homeport.data-worx.de/problems/unknown-module` — status `404`.

The gateway answered: no module is mounted at the path, so the request never
reached one. The module id in `/modules/{moduleId}/…` is misspelled, or the
module is not installed in this Homeport installation.

```json
{
  "type": "https://homeport.data-worx.de/problems/unknown-module",
  "title": "No module is mounted at this path.",
  "status": 404,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

A `404` from a module itself, such as for a record that does not exist, never
carries this type. Check the module id against what the operator gave you;
the same request fails the same way until the module is installed.
