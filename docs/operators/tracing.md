# Tracing

Every Homeport service and every module built with the SDK exports its
traces over OTLP, together with its metrics and logs, to the endpoint in
`OTEL_EXPORTER_OTLP_ENDPOINT`. A local run through the AppHost sets it to the
Aspire dashboard, which shows the traces under *Traces*. Without the variable
nothing is exported.

## One trace per request

A request to a module is one trace, from the gateway to metering:

1. The gateway's span of the client request, and its call to the module.
2. The module's span of the forwarded request. The gateway passes the W3C
   `traceparent` on, so a client that sends one finds its trace id in every
   span.
3. The gateway's publish of the `UsageRecorded` event to RabbitMQ. Wolverine
   carries the trace context in the message.
4. Metering's span of consuming the event.

The trace id is also stored with the event in metering's `UsageEvents` and
returned as `traceId` in every error response, so a usage row or an error a
client reports leads to its trace.

## Span attributes

| Attribute | Set by | Value |
|---|---|---|
| `homeport.tenant.id` | gateway, module, metering | The tenant of the client |
| `homeport.client.id` | gateway, module, metering | The client id as the identity provider issued it |
| `homeport.module.id` | gateway, module, metering | The module the request was routed to |
| `homeport.meter` | gateway, metering | The meter the request is counted in |

The gateway sets them on requests on a module's route only, once it knows
the client. A module sets the tenant and client for a request with a module
token; on an anonymous endpoint, such as the manifest, only the module id.

## Metrics

| Metric | Service | Counts |
|---|---|---|
| `homeport.gateway.requests` | gateway | Requests on a module's route, throttled ones included; tagged with `homeport.module.id` and `http.response.status_code` |
| `homeport.gateway.throttled` | gateway | See [throttling](throttling.md#metrics) |
| `homeport.metering.events` | metering | See [metering](metering.md#metrics-and-logs) |

No metric carries a label taken from a request, such as a request id, a trace
id, a tenant or a path a caller sent. The module id of
`homeport.gateway.requests` comes from the route table, not from the URL: a
request for an unknown module is not counted. Per-tenant usage is metering's
job, not the metrics'.
