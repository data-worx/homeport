# module-unreachable

`https://homeport.data-worx.de/problems/module-unreachable` — status `502`.

The gateway answered: it could not reach the module, or the module broke off
its answer. The module may be restarting or failing. The request may or may
not have reached the module before the connection failed.

```json
{
  "type": "https://homeport.data-worx.de/problems/module-unreachable",
  "title": "The module could not be reached.",
  "status": 502,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying after a moment may succeed. Repeating a request that changes data is
only safe where the endpoint says it is idempotent. If it persists, quote the
`traceId` to the operator.
