# Metering

Metering counts the usage of every module per tenant: every request the
gateway forwards to a module becomes a usage event, which metering adds to
an hourly and a daily aggregate per tenant, module and meter. A tenant reads
its own aggregates through the gateway at `/platform/v1/usage`, as JSON or
CSV; see [reading usage](../api-consumers/reading-usage.md). How it works
and what it guarantees is in
[the architecture page](../architecture/metering.md).

It keeps its data in the schema `metering` of the Homeport database; see
[the data model](../architecture/data-model.md).

## What to expect

- **Usage arrives within seconds** of a request. The gateway answers the
  client first and publishes the event afterwards.
- **An event is never counted twice**, however often RabbitMQ delivers it.
- **An event can be lost** between the gateway and RabbitMQ: when the broker
  does not take it within the gateway's retries, and when the gateway stops
  or crashes while it still holds events. The first is counted in
  `homeport.gateway.usage.dropped`; the second is not. Keep the broker up
  and let the gateway stop gracefully.
- **Raw events are kept forever in this release.** There is no retention:
  `UsageEvents` grows by one row per module request, the aggregates by one
  row per tenant, module, meter and hour or day. Plan the disk for it. Do
  not delete events by hand: an event redelivered after its row is gone is
  counted again.

## Preparing the database

The database init script,
[`deploy/postgres/init/init-homeport.sh`](../../deploy/postgres/init/init-homeport.sh),
creates metering's schema and roles next to the control plane's; see
[the control plane](control-plane.md#preparing-the-database) for running it
against a server of your own.

| Role | Used by | Rights | Password from |
|---|---|---|---|
| `metering_owner` | `migrate` | owns `metering`, runs DDL | `METERING_OWNER_PASSWORD` |
| `metering_app` | metering | reads and writes the tables, no DDL, held to row-level security | `METERING_APP_PASSWORD` |

## Preparing the broker

Every service connects to RabbitMQ with a user of its own
([ADR-0014](../architecture/adr/0014-authentication-between-services.md)).
[`deploy/rabbitmq/init/init-homeport.sh`](../../deploy/rabbitmq/init/init-homeport.sh)
creates them: the AppHost and the compose file run it as the RabbitMQ
container's entrypoint, so it writes the users on every start, before the
broker accepts a connection, and a changed password takes effect with the
next start.

| User | Used by | Configure | Write | Read | Password from |
|---|---|---|---|---|---|
| `gateway` | the gateway | `homeport.usage` | `homeport.usage` | nothing | `GATEWAY_RABBITMQ_PASSWORD` |
| `metering` | metering | `homeport.usage`, `homeport.metering.usage`, `wolverine-dead-letter-queue` | `homeport.metering.usage`, `wolverine-dead-letter-queue` | `homeport.usage`, `homeport.metering.usage`, `wolverine-dead-letter-queue` | `METERING_RABBITMQ_PASSWORD` |
| `RABBITMQ_DEFAULT_USER` | the broker's health check | everything | everything | everything | `RABBITMQ_PASSWORD` |

The AppHost generates the passwords and keeps them in its user secrets; the
compose file takes them from `.env`.

Against a RabbitMQ of your own, create the two users once:

```bash
rabbitmqctl add_user gateway "${GATEWAY_RABBITMQ_PASSWORD}"
rabbitmqctl set_permissions -p / gateway '^homeport\.usage$' '^homeport\.usage$' '^$'
rabbitmqctl add_user metering "${METERING_RABBITMQ_PASSWORD}"
rabbitmqctl set_permissions -p / metering \
  '^(homeport\.usage|homeport\.metering\.usage|wolverine-dead-letter-queue)$' \
  '^(homeport\.metering\.usage|wolverine-dead-letter-queue)$' \
  '^(homeport\.usage|homeport\.metering\.usage|wolverine-dead-letter-queue)$'
```

Check it: each user authenticates and has its permissions.

```bash
rabbitmqctl authenticate_user gateway "${GATEWAY_RABBITMQ_PASSWORD}"
rabbitmqctl list_permissions -p /
```

```text
Success
user	configure	write	read
gateway	^homeport\.usage$	^homeport\.usage$	^$
metering	^(homeport\.usage|homeport\.metering\.usage|wolverine-dead-letter-queue)$	^(homeport\.metering\.usage|wolverine-dead-letter-queue)$	^(homeport\.usage|homeport\.metering\.usage|wolverine-dead-letter-queue)$
```

`list_permissions` also lists your administrator.

The services declare the exchange and the queues themselves when they start.

## Migrating

Metering's image has a second entry point, `migrate`. It connects as the
owner role through `ConnectionStrings__metering-owner`, applies the pending
migrations — the tables and their row-level security — and exits with `0`,
or with `1` and the cause in its log. The AppHost runs it as the resource
`metering-migrate`, the compose file as the service of the same name, and
both start metering only after it completed successfully. Like the control
plane's, it retries a connection while PostgreSQL still starts.

## The service

| Environment variable | Compose `.env` | Meaning |
|---|---|---|
| `ConnectionStrings__metering` | built from `METERING_APP_PASSWORD` | The app role's connection |
| `ConnectionStrings__rabbitmq` | built from `METERING_RABBITMQ_PASSWORD` | The broker, as the user `metering` |

The gateway publishes with `ConnectionStrings__rabbitmq` built from
`GATEWAY_RABBITMQ_PASSWORD`. How long it retries a failed send is
`Usage__RetryDelays__0`, `Usage__RetryDelays__1`, … — one `TimeSpan` per
retry, `00:00:01`, `00:00:05` and `00:00:30` when none is set. Setting one
replaces all three. The gateway logs the delays that apply when it starts.

## The usage export

Metering serves `GET /v1/usage`, which the gateway forwards from
`/platform/v1/usage` to clients with the scope `usage.read`. The route is
configured in the gateway, not registered
([ADR-0019](../architecture/adr/0019-tokens-for-forwarded-platform-requests.md)).
The gateway replaces the client's token with a caller token for the
audience `homeport-metering`, which names the client's tenant; metering
validates it against the gateway's JWKS and serves that tenant's rows only.
A service token, which names no tenant, is refused with `403`.

| Environment variable | Service | Meaning |
|---|---|---|
| `PlatformRoutes__MeteringUrl` | gateway | Where the gateway forwards the export; `http://metering` when none is set. The AppHost and the compose file set metering's endpoint |
| `GatewayTokens__JwksUri` | metering | The gateway's JWKS; `http://gateway/.well-known/jwks.json` when none is set, which the reference the AppHost and the compose file set resolves |

The gateway logs the metering address that applies when it starts. The
export counts against the tenant's rate limit and is not itself metered.
Metering logs a token it refuses at Warning, without the token.

## Metrics and logs

| Metric | Service | Counts |
|---|---|---|
| `homeport.metering.events` | metering | Consumed events; `homeport.metering.outcome` is `recorded` or `duplicate` |
| `homeport.gateway.usage.dropped` | gateway | Events that never reached the broker; `homeport.gateway.usage.reason` is `retries_exhausted` or `not_published` |
| `homeport.gateway.usage.rejected` | gateway | Quantities a module reported that were rejected for the route's quantity; `homeport.gateway.usage.reason` is `usage.malformed`, `usage.undeclared-meter` or `usage.invalid-quantity` |

The gateway and metering may start before RabbitMQ accepts connections.
Wolverine then retries, up to 20 times, and logs each failed attempt at
Error under `Wolverine.Runtime.WolverineRuntime` as
`Error trying to start message broker rabbitmq on Attempt 1 of 20`. That is
not a failure of the service; one that gives up stops.

The gateway logs a rejected quantity at Warning with the module, route and
reason: the module reports usage wrongly and needs fixing, while its requests
are still counted with the route's quantity. It logs a dropped event at
Warning with its event id, and
Wolverine logs every failed send at Error under
`Wolverine.Transports.Sending`. Metering logs a duplicate at Debug. A
message metering could not record after its retries stays in
`wolverine-dead-letter-queue`: a queue that is not empty needs a look.
