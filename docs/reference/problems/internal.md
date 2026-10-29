# internal

`https://homeport.data-worx.de/problems/internal` — status `500`.

The request failed for a reason the caller cannot influence: an unhandled
exception, a broken dependency. The body says nothing about the cause, so no
internals reach the caller. A module answers with it, and the gateway puts it
in place of the body of every `500` a module answers with. The cause is in the logs and the trace of the
request; quote the `traceId` when you report it to the operator.

```json
{
  "type": "https://homeport.data-worx.de/problems/internal",
  "title": "An internal error occurred.",
  "status": 500,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying may succeed if the cause was temporary. Repeating a request that
changes data is only safe where the endpoint says it is idempotent.
