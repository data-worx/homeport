# Metering

How a request through the gateway becomes usage in PostgreSQL, what is
guaranteed on the way, and why it is built this way. The decision is
[ADR-0007](adr/0007-throttling-and-metering.md); the binding rules are in
the *Metering* section of [`AGENTS.md`](../../AGENTS.md#metering). How to
run it is in [the operators' page](../operators/metering.md).

## The path of one request

1. The gateway forwards a request to a module and answers the client. Then
   it publishes one `UsageRecorded`: a new `eventId`, the tenant and client,
   the module, the **route template** from the manifest (`/v1/orders/{id}`,
   never the concrete path), the route's meter and quantity — or the
   quantity the module reported for that meter — the status the
   client got, the duration and the trace id.
2. Wolverine sends it to the fanout exchange `homeport.usage` in RabbitMQ.
3. RabbitMQ routes it to the durable queue `homeport.metering.usage`.
4. Metering takes it from the queue and, in one transaction for the event's
   tenant, records the event and adds its quantity to the aggregate of its
   UTC hour and of its UTC day. Only then does it acknowledge the message.

A request the gateway refuses before it reaches a module — `401`, `403`, an
unknown module — publishes nothing: there is no module request to count and,
for an unauthenticated one, no tenant to count it for. Every request that
reaches a module counts with its route's quantity, or the one the module
reported, whatever the status. The gateway takes the report out of the
module's response while it forwards the headers, before the caller sees
them, and checks it against the route in its Domain; a report it rejects
falls back to the route's quantity, so a module's mistake changes how much is
counted, never whether.

## What is guaranteed

**From the queue on, every event counts exactly once.** Metering
acknowledges a message only after its transaction committed, so a crash
redelivers the message rather than losing it. Delivering it again changes
nothing: the transaction first looks for the `eventId`, and an event it
already recorded is discarded. Two deliveries of the same event racing each
other end at the unique key on `eventId`; the loser's transaction rolls
back, Wolverine retries the message, and the retry finds the event. The
aggregates are increased by one `UPDATE` that adds in the database, so two
events of one bucket never overwrite each other; two first events of a
bucket race at the bucket's key and end the same way.

A message that still fails after three retries goes to Wolverine's
dead-letter queue, `wolverine-dead-letter-queue`, where it stays until an
operator looks at it.

**Between the gateway and the queue, there is no outbox.** The gateway
holds an event in memory until RabbitMQ confirms it (publisher confirms).
When a send fails, the gateway waits and tries again, after 1, 5 and 30
seconds by default; an event that fails after its last retry is dropped,
logged and counted in `homeport.gateway.usage.dropped`. An event the gateway
still holds when it stops or crashes is lost without being counted. So an
event reaches metering at most once from a gateway that stays up through a
broker outage shorter than its retries, and may be missing otherwise.

The request itself never waits for the broker: the gateway has answered
before it publishes.

## Why it is built this way

- **No outbox at the gateway.** An outbox needs a database the gateway
  writes to on every request. The gateway has none, and adding one would put
  a database write in the path of every call to every module. Usage is
  showback and chargeback data, not money: a small, counted loss in a
  broker outage is accepted in M1. The loss is visible in a metric rather
  than silent.
- **Wolverine on both sides.** Wolverine is the abstraction over the broker
  (ADR-0007). The gateway and metering speak to it, not to RabbitMQ, so a
  different broker changes their configuration rather than their code.
- **Idempotency in the database, not in the broker.** RabbitMQ delivers at
  least once. A unique key on `eventId` and one transaction per event make a
  duplicate harmless, wherever it came from.
- **Buckets in UTC.** An hour and a day mean the same for every tenant and
  every gateway replica, whatever their time zone. An operator's own
  calendar is a matter of the export.
- **The domain owns the buckets.** Which hour and day an event falls in is a
  rule, tested without a database: an event at `23:59:59.999Z` and one at
  `00:00:00Z` fall on different days.
- **Tenant-owned tables.** Events and aggregates are a tenant's data, with
  query filters and row-level security; see
  [the data model](data-model.md#row-level-security).
