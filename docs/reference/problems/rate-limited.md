# rate-limited

`https://homeport.data-worx.de/problems/rate-limited` — status `429`.

The gateway answered: the tenant's requests exceeded the rate limit of its
plan. The request did not reach the module and is not counted as usage.

```json
{
  "type": "https://homeport.data-worx.de/problems/rate-limited",
  "title": "The rate limit is exceeded.",
  "status": 429,
  "detail": "The tenant exceeded the rate limit of its plan. Retry after 1 seconds.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The response carries `Retry-After` in whole seconds and the RateLimit
headers. Retry once `Retry-After` has passed. If it keeps happening, the
plan is too small for the tenant's load; ask the operator. See
[rate limits](../../api-consumers/rate-limits.md).
