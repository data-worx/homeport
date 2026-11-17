# module-timeout

`https://homeport.data-worx.de/problems/module-timeout` — status `504`.

The gateway answered: the module did not answer in time, and the gateway
stopped waiting. The module may still have done the work. A platform or
admin request is answered with
[`platform-service-timeout`](platform-service-timeout.md) instead.

In time means without a pause longer than the operator's forwarding
timeout, 30 seconds unless configured otherwise; see
[running behind a reverse proxy](../../operators/reverse-proxy.md#timeouts).

```json
{
  "type": "https://homeport.data-worx.de/problems/module-timeout",
  "title": "The module did not answer in time.",
  "status": 504,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Check whether the work was done before repeating a request that changes data,
unless the endpoint says it is idempotent. If it persists, quote the
`traceId` to the operator.
