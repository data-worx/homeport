# ADR-0007: Throttling and metering

- Status: Accepted
- Date: 2026-10-19

## Context

Limits must hold with two or more gateway replicas. Operators allocate costs
to tenants with their own ERP or accounting system; they need usage data, not
invoices. Throttling and metering are explicit learning goals.

## Decision

- **Throttling** in the gateway, per tenant and plan, in two layers: a rate
  limit (token bucket) and quotas per meter per day or month. Counters live in
  **Valkey** (Redis-compatible, BSD licence). Exceeding a limit returns `429`
  with `Retry-After`.
- **Messaging** through **RabbitMQ** with **Wolverine** (MIT): one broker for
  usage events and worker jobs, with outbox and retries built in.
- **Metering** is an own implementation: the gateway publishes one
  `UsageRecorded` per request, an idempotent consumer (unique `eventId`)
  aggregates per tenant, module, meter, hour and day into PostgreSQL, and
  usage is exported as CSV and JSON, with a webhook when a period closes.
- **No invoices.**

## Consequences

- Two more infrastructure components (Valkey, RabbitMQ) for operators to run.
- Duplicate delivery is safe; usage is never counted twice.
- Billing logic stays with the operator.
