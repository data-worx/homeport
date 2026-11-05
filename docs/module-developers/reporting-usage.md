# Reporting usage

The gateway counts every request a module answers in the meter and quantity
the matched route declares in the manifest
([module contract](../reference/module-contract/README.md#metering)). A
route whose requests differ in size — a batch, a document with pages —
reports the quantity of each request itself. This page shows how a module
built with `Homeport.Sdk` does that. The batch route of the Echo module in
`src/Modules/Echo/` is the reference.

## Declaring the meter

The route declares the meter it counts in and the quantity that applies
when the module reports none, and the meter is listed under `meters`:

```csharp
new ManifestRoute(HttpMethods.Post, "/v1/echo/batch", "echo.invoke", "items", Quantity: 1),
```

A response can report only in the meter of its own route.

## Reporting the quantity

`ReportUsage` sets the header `X-Homeport-Usage: <meter>=<quantity>` on the
response, before the response has started:

```csharp
Response.ReportUsage("items", batch.Value.Quantity);

return new BatchResponse(batch.Value.Items);
```

A response reports one meter; a second call replaces the first. A meter that
is empty or contains `=`, `,` or white space, and a quantity that is not
positive, throw an `ArgumentException`: a module that reports them has a bug.

How much a request counts is a business rule. It belongs in the module's
Domain, as Echo's `BatchRules`, and the host only reports what the use case
returns.

## What the gateway does with it

The gateway removes the header before the response reaches the caller and
counts the request with the reported quantity, whatever the status. It
rejects a report when

- it is not one `<meter>=<quantity>` pair,
- the meter is not the one the route declares — compared exactly,
  case included,
- the quantity is not a positive integer, such as `0`, `-5`, `+5` or `2.5`.

A rejected report counts the request with the route's quantity. The gateway
logs it at Warning and counts it in `homeport.gateway.usage.rejected`, so
the operator sees a module that reports wrongly (see
[metering](../operators/metering.md#metrics-and-logs)).

A module that does not use the SDK sets the header itself, with the same
rules.
