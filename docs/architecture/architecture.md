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
| `Architecture.Tests` | Asserts the reference graph and the layout (devkit template, not yet in a solution) | — |

No production project exists yet. The target picture is in
[vision.md](vision.md); the binding layout is in
[`AGENTS.md`](../../AGENTS.md#architecture).

## Directory layout

```text
src/
  Directory.Build.props
  Tests/
    Architecture.Tests/
```

## Why it is cut this way

- Services and modules are separate deployables, so the layer rule of
  `dotnet.layout` is applied per service instead of to one application
  ([ADR-0003](adr/0003-service-granularity-and-module-contract.md)). A
  service without business rules stays a single project, which keeps the
  infrastructure-only services (gateway, registry) small.
