# ADR-0016: Error responses

- Status: Accepted
- Date: 2026-10-28

## Context

Homeport has no common error format. No service or module maps its errors
to a defined body, and the gateway passes whatever a module returns straight
to the caller. A caller that uses several modules would have to handle as
many error formats as there are modules, and an unhandled exception in a
module can reach the caller with its internals.

Problem Details for HTTP APIs (RFC 9457) is the standard error body for HTTP
APIs, and ASP.NET Core produces it natively.

## Decision

- **Format.** Every 4xx and 5xx response that leaves Homeport is Problem
  Details (RFC 9457) with the media type `application/problem+json`.
- **Module contract.** The module contract requires a module to answer every
  4xx and 5xx as Problem Details. The SDK provides this by default.
- **Problem types.** `type` is an absolute URI. Homeport's own types live
  under `https://homeport.data-worx.de/problems/<name>`, with one reference
  page per type. A module defines its own types under a prefix of its own.
- **Trace id.** Every error response carries the extension `traceId`, set by
  the gateway.
- **Validation errors.** They use the `type`
  `https://homeport.data-worx.de/problems/validation`, status 400, and the
  extension `errors` as `{ "<field path>": ["<message>", …] }`. A field path
  uses the JSON notation of the request: camelCase, such as
  `items[0].name`.
- **Language.** `title` and `detail` are English. There is no localisation.
- **Gateway, 4xx.** The gateway passes a conforming 4xx Problem Details
  response through and only adds `traceId`. It replaces every non-conforming
  error body with a Problem Details body of its own.
- **Gateway, own errors.** The gateway answers its own errors — 401, 429,
  502, 503, 504 and an unknown module — with a Problem Details body of its
  own.
- **Gateway, 5xx.** The gateway replaces the body of every 5xx from a module,
  even a conforming one, so no internals leak to the caller.
- **Business errors.** Inside a service a business error is a result value,
  not an exception (`core.antipatterns`). The API layer maps it onto a
  problem type. The domain knows no HTTP status.

## Consequences

- A caller handles one error format, whichever module answered, and can
  correlate any error with its trace through `traceId`.
- The gateway has to inspect error responses: it reads the media type and
  the body of every 4xx and rewrites every 5xx. A module cannot hand a
  caller a 5xx body of its own design, however careful.
- A module that does not use the SDK has to produce Problem Details itself
  to meet the contract; otherwise the gateway replaces its 4xx bodies and
  the caller loses the module's detail.
- Problem type URIs are identifiers. Homeport never fetches them, so they do
  not contradict the rule that nothing phones home. The reference pages
  behind Homeport's own types have to be published, and a type, once
  published, is part of the public API.
- Clients that want localised messages have to map `type` to their own
  texts.
