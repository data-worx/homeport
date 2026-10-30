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
| Error | An unhandled exception in a module, answered as `internal` Problem Details |
| Warning | A rejected gateway token, with the reason; a failed retrieval of the gateway's JWKS; a failed forward to a module; a module error body the gateway replaced |
| Information | The start of a service, with its effective configuration: the gateway's key ids and modules, a module's id, version and contract version |
| Debug | Per-request decisions, such as the `X-Homeport-*` headers the gateway stripped from a request |

The default level is `Information`, so Debug entries are not written.

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
