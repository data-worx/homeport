# module-unavailable

`https://homeport.data-worx.de/problems/module-unavailable` — status `503`.

The gateway answered: the module is installed, but no instance of it is
available to take the request. The request did not reach the module.

```json
{
  "type": "https://homeport.data-worx.de/problems/module-unavailable",
  "title": "The module is not available.",
  "status": 503,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying later may succeed. A `503` from the module itself never carries this
type. If it persists, quote the `traceId` to the operator.
