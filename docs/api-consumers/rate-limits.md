# Rate limits

Every request to a module counts against the rate limit of your tenant's
plan. The limit is a token bucket: it holds a number of requests at once,
the *burst*, and refills at a steady rate in requests per second. All
clients of a tenant draw from the same bucket, whichever gateway replica
answers them. The operator sets both numbers per plan.

A request the gateway refuses before it reaches a module — `401`, `403`, an
unknown module — does not count.

## The headers

Every module response carries two headers, defined by the IETF draft
[RateLimit header fields for HTTP](https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/):

```text
RateLimit-Policy: "tenant";q=20;w=2
RateLimit: "tenant";r=19;t=1
```

| Header | Parameter | Means |
|---|---|---|
| `RateLimit-Policy` | `q` | How many requests the bucket holds: the plan's burst |
| | `w` | How many seconds an empty bucket takes to fill, rounded up |
| `RateLimit` | `r` | How many requests you may still send right now |
| | `t` | How many seconds until the bucket is full again, rounded up |

`"tenant"` names the policy; it is the only one today. Read `r` to slow down
before you are refused, rather than waiting for a `429`.

## Too many requests

A request over the limit is answered with `429` and never reaches the module:

```text
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
Retry-After: 1
RateLimit-Policy: "tenant";q=20;w=2
RateLimit: "tenant";r=0;t=2
```

```json
{
  "type": "https://homeport.data-worx.de/problems/rate-limited",
  "title": "The rate limit is exceeded.",
  "status": 429,
  "detail": "The tenant exceeded the rate limit of its plan. Retry after 1 seconds.",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

`Retry-After` is in whole seconds, rounded up: a request sent after that
long finds a token, unless another client of your tenant took it first. Wait
at least that long before you retry, and spread retries out rather than
sending them all at once. See
[rate-limited](../reference/problems/rate-limited.md).

## When the headers are missing

The gateway keeps the buckets in Valkey. While it cannot reach Valkey, it
lets every request through unthrottled rather than refusing them all, and
the responses carry no RateLimit headers. Do not read their absence as
"no limit": the limit applies again once Valkey is back.
