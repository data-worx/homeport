# Throttling

The gateway holds every tenant to the rate limit of its plan: a token bucket
that holds the plan's *burst* and refills at its requests per second. A
request over the limit is answered with `429` and `Retry-After`, never
reaches the module and is not metered; every module response carries the
RateLimit headers. What a caller sees is in
[rate limits](../api-consumers/rate-limits.md). Quotas per meter follow in a
later milestone.

## Plan limits

A plan's limits live in the control plane, next to its tenants and clients.
Today the control plane seeds one plan; set its limits with the seed, as
[the control plane](control-plane.md#migrating-and-seeding) describes:

| Setting | Means |
|---|---|
| `Seed__RequestsPerSecond` | How fast the bucket refills, in requests per second; positive |
| `Seed__Burst` | How many requests the bucket holds at once; at least the rate |

A tenant with a burst of 20 and 10 requests per second can send 20 requests
at once, then 10 per second. The gateway keeps the plan it looked up with
the client for `ControlPlane:ClientCacheLifetime`, so a changed limit takes
effect once that has passed.

## The buckets

The buckets live in Valkey, one key per tenant,
`homeport:gateway:ratelimit:<tenant id>`. The gateway takes a token with a
script that runs inside Valkey, so it is atomic: however many gateway
replicas a tenant's requests reach, together they accept no more than the
one bucket allows. The script uses Valkey's clock, not the gateway's, so
replicas whose clocks drift still agree. A key expires once its bucket would
be full again, so an idle tenant leaves nothing behind.

The gateway reads Valkey's address from `ConnectionStrings__valkey`. The
AppHost runs two gateway replicas, and so does the compose file, with
`deploy.replicas`.

## While Valkey is down

The gateway **fails open**: while it cannot reach Valkey, every request
passes without a rate limit, and its response carries no RateLimit headers.
Refusing every tenant because a counter store is gone would turn an outage of
Valkey into an outage of Homeport. The gateway stays ready meanwhile: Valkey
has no part in its health check, and a request does not wait for Valkey but
passes at once.

What tells you it happened:

| Signal | When |
|---|---|
| Metric `homeport.gateway.ratelimit.unavailable` | Counts every request that passed unthrottled |
| Warning `Valkey could not be reached; requests pass without a rate limit` | Logged at most once a minute per replica, with the exception, however many requests pass |

Alert on the metric: while it rises, no tenant is limited. The limits apply
again as soon as Valkey answers; a bucket Valkey lost starts full.

## Metrics

| Metric | Counts |
|---|---|
| `homeport.gateway.throttled` | Requests rejected with `429` for their tenant's rate limit |
| `homeport.gateway.ratelimit.unavailable` | Requests that passed unthrottled because Valkey could not be reached |

Both are on the gateway's meter, `Homeport.Platform.Gateway.Api`, and carry
no tags. A rejected request is logged at Debug with its tenant and plan.
