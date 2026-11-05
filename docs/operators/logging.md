# Logging

Every Homeport service and every module writes its log through .NET's
`ILogger` and exports it with OpenTelemetry, together with its traces and
metrics, to the endpoint in `OTEL_EXPORTER_OTLP_ENDPOINT`. Without that
variable nothing is exported.

No log carries a secret, a token, personal data, or a request or response
body.

## What is logged

| Level | Example |
|---|---|
| Error | An unhandled exception in a module, answered as `internal` Problem Details; a module the registry refused, with the reasons, before it stops; a usage event the broker did not take, logged by Wolverine at every attempt |
| Warning | A rejected gateway token, with the reason; a failed retrieval of the gateway's JWKS; a failed forward to a module; a module error body the gateway replaced; a rejected registration key; a manifest the registry refused; a registry the gateway or a module cannot reach; a usage event the gateway dropped, with its event id |
| Information | The start of a service, with its effective configuration: the gateway's key ids and registry, the registry's module ids, a module's id, version and contract version; a registered instance; changed routes at the gateway |
| Debug | Per-request decisions, such as the `X-Homeport-*` headers the gateway stripped from a request, an unchanged route table, or a duplicate usage event metering discarded |

The default level is `Information`, so Debug entries are not written. The
framework categories that log every request — `Microsoft.AspNetCore`,
`System.Net.Http.HttpClient` and, at the gateway, `Yarp` — start at
`Warning`, so a heartbeat or a forwarded request leaves no entry. The
gateway also sets `Wolverine.Configuration.HandlerDiscovery` to `Error`: it
only publishes, so Wolverine's warning that it found no message handlers is
expected there and would only mislead. Each
service sets them in its `appsettings.json`; the variables below override
them.

## Changing the level

The level is set per category, the namespace of the code that logs. Every
Homeport category starts with `Homeport`. Set it through the environment
variable `Logging__LogLevel__<category>`, with `.` in the category written as
`__` too:

| Variable | Effect |
|---|---|
| `Logging__LogLevel__Default=Warning` | Only warnings and errors, from every category |
| `Logging__LogLevel__Homeport=Debug` | Homeport's own Debug entries, framework categories unchanged |
| `Logging__LogLevel__Homeport__Platform__Gateway=Debug` | Debug for the gateway's code only |

The service reads the variable at start-up, so a change takes effect after
the container is restarted.

With Docker Compose, put the variable into an override file next to
`docker-compose.yaml`, rather than into the generated file, which is replaced
on every update. `docker compose` merges a file named
`docker-compose.override.yaml` on its own:

```bash
cd deploy/compose
cat > docker-compose.override.yaml <<'EOF'
services:
  gateway:
    environment:
      Logging__LogLevel__Homeport: "Debug"
EOF
docker compose up -d gateway
```

`docker compose up -d` recreates only the containers whose configuration
changed. Remove the file and run the same command again to go back.
