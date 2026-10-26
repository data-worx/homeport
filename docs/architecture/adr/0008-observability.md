# ADR-0008: Observability

- Status: Accepted
- Date: 2026-10-19

## Context

Operators already run monitoring of their own. Homeport must fit into it
rather than bring a mandatory stack, but a ready-made view helps anyone who
has none.

## Decision

- **OpenTelemetry** for traces, metrics and logs in every service, through
  `Homeport.ServiceDefaults`, exported over **OTLP**.
- An optional Compose profile `observability` adds a Grafana stack with
  Homeport dashboards.
- One trace spans gateway → module → RabbitMQ → metering.

## Consequences

- Operators point OTLP at their own collector and are done.
- The naming conventions for services, attributes and metrics are binding;
  they are in `AGENTS.md`.
- The Grafana profile adds images to scan and keep current.
