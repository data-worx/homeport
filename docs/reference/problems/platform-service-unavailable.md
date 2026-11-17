# platform-service-unavailable

`https://homeport.data-worx.de/problems/platform-service-unavailable` — status `503`.

The gateway answered: no instance of the platform service behind a platform
or admin endpoint is available to take the request. The request did not
reach the service. A module's failure is answered with
[`module-unavailable`](module-unavailable.md) instead.

```json
{
  "type": "https://homeport.data-worx.de/problems/platform-service-unavailable",
  "title": "The platform service is not available.",
  "status": 503,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying later may succeed. A `503` from the service itself never carries
this type. If it persists, quote the `traceId` to the operator.
