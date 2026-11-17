# platform-service-timeout

`https://homeport.data-worx.de/problems/platform-service-timeout` — status `504`.

The gateway answered: the platform service behind a platform or admin
endpoint did not answer in time, and the gateway stopped waiting. The
service may still have done the work. A module's failure is answered with
[`module-timeout`](module-timeout.md) instead.

In time means without a pause longer than the operator's forwarding
timeout, 30 seconds unless configured otherwise; see
[running behind a reverse proxy](../../operators/reverse-proxy.md#timeouts).

```json
{
  "type": "https://homeport.data-worx.de/problems/platform-service-timeout",
  "title": "The platform service did not answer in time.",
  "status": 504,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Check whether the work was done before repeating a request that changes data,
unless the endpoint says it is idempotent. If it persists, quote the
`traceId` to the operator.
