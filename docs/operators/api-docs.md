# API docs

The gateway can show the API documentation of every module and of the
platform in one place, at `/platform/docs`, rendered with Scalar. What a
caller sees there is described in [API docs](../api-consumers/api-docs.md);
the decision is [ADR-0017](../architecture/adr/0017-consolidated-api-docs.md).

## Switching them on

| Setting | Environment variable | Default |
|---|---|---|
| `Gateway:Docs:Enabled` | `Gateway__Docs__Enabled` | `true` in Development, `false` otherwise |

A local run with the AppHost runs the gateway in Development, so the docs
are on; the gateway's row in the dashboard links to them as `API docs`. The
generated compose file does not set the environment, so they are off there
and in every other installation until you set the variable to `true`. The gateway logs whether they are on in its start line
(`API docs on` or `API docs off`).

While they are off, `/platform/docs` and every document below it answer
`404`, and the gateway reads no module's document.

**The view is anonymous.** Anyone who reaches the gateway sees the shape of
every module's API: paths, parameters, schemas. That is why it is off
outside Development. Switch it on in production only where that is
acceptable, for example on a gateway reachable from the internal network
only. A view behind a login needs a new decision.

## Where the documents come from

Every instance of a module serves its own OpenAPI document under the path its
manifest names. Whenever the routes change, the gateway reads the document of
every instance that wins a route and rebuilds its view, in the background, so
routing never waits for a document. The gateway reads them without a token;
a module serves its document anonymously.

- While the registry is unavailable, the routes and the docs stay as they
  were last read.
- When one instance's document cannot be read, the gateway logs a warning
  and keeps the document it read last from that instance. If it never read
  one, the routes of that instance are missing from the view until the
  routes change again; the routes themselves are served regardless.

## Nothing leaves the installation

Scalar's script is part of the gateway and served by it. Scalar's CDN fonts,
telemetry, AI agent, MCP integration, hosted API client and developer
toolbar, whose *Share* uploads a document to scalar.com, are switched off.
The page loads nothing from any host but the gateway, and a request sent
from it goes from the browser straight to the gateway.
