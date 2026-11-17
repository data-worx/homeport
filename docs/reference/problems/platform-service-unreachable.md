# platform-service-unreachable

`https://homeport.data-worx.de/problems/platform-service-unreachable` — status `502`.

The gateway answered: it could not reach the platform service behind a
platform endpoint, such as metering behind `/platform/v1/usage`, or an
admin endpoint, or the service broke off its answer. The service may be
restarting or failing. The request may or may not have reached the service
before the connection failed. A module's failure is answered with
[`module-unreachable`](module-unreachable.md) instead.

```json
{
  "type": "https://homeport.data-worx.de/problems/platform-service-unreachable",
  "title": "The platform service could not be reached.",
  "status": 502,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying after a moment may succeed. Repeating a request that changes data is
only safe where the endpoint says it is idempotent. If it persists, quote the
`traceId` to the operator.
