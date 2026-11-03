# control-plane-unavailable

`https://homeport.data-worx.de/problems/control-plane-unavailable` — status `503`.

The gateway answered: it could not ask the control plane which tenant the
client belongs to. The request did not reach the module.

```json
{
  "type": "https://homeport.data-worx.de/problems/control-plane-unavailable",
  "title": "The control plane is not available.",
  "status": 503,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying later may succeed. A client the gateway looked up a moment before
is still served from its cache, so the error hits clients it has not seen
recently first. If it persists, quote the `traceId` to the operator.
