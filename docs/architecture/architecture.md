# Architecture

The projects that exist in homeport today, how they are laid out, and
why they are cut this way. This document describes the current state. The
binding rules — the dependency direction, who may reference what — live in the
*Architecture* section of [`AGENTS.md`](../../AGENTS.md#architecture).

## Projects

<!-- One row per project. "May depend on" names the projects it references,
     or "nothing" for the centre. A project that is not in this table does not
     exist yet. -->

| Project | Role | May depend on |
|---|---|---|
| `Homeport.AppHost` | The topology and its single source of truth: declares PostgreSQL, RabbitMQ, Valkey and Keycloak, and generates `deploy/compose/` | nothing |
| `Homeport.ServiceDefaults` | What every service sets up the same way: OpenTelemetry over OTLP, health endpoints, service discovery, resilient HTTP clients | nothing |
| `Architecture.Tests` | Asserts the per-service layout, naming and reference rules of `AGENTS.md`, each also against a fixture tree that breaks it (solution folder `/Tests/`) | — |
| `Homeport.AppHost.Tests.Integration` | Starts the AppHost and waits for every resource to become healthy; needs Docker | `Homeport.AppHost` |
| `Homeport.ServiceDefaults.Tests.Integration` | Asserts the health endpoints outside Development | `Homeport.ServiceDefaults` |

No service or module exists yet. The target picture is in
[vision.md](vision.md); the binding layout is in
[`AGENTS.md`](../../AGENTS.md#architecture).

## Directory layout

```text
Homeport.slnx
Directory.Packages.props
deploy/
  compose/                generated from the AppHost, never edited
src/
  Directory.Build.props
  Aspire/
    Homeport.AppHost/
  Shared/
    Homeport.ServiceDefaults/
  Tests/
    Architecture.Tests/
    Homeport.AppHost.Tests.Integration/
    Homeport.ServiceDefaults.Tests.Integration/
```

## Why it is cut this way

- Services and modules are separate deployables, so the layer rule of
  `dotnet.layout` is applied per service instead of to one application
  ([ADR-0003](adr/0003-service-granularity-and-module-contract.md)). A
  service without business rules stays a single project, which keeps the
  infrastructure-only services (gateway, registry) small.
