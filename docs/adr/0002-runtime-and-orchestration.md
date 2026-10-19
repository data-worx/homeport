# ADR-0002: Runtime and orchestration

- Status: Accepted
- Date: 2026-10-19

## Context

.NET is the maintainer's core stack. Local development needs service
discovery and OpenTelemetry without hand-written infrastructure, and the
deployment artefacts must not drift from the development topology.

## Decision

- **.NET 10 (LTS)** and ASP.NET Core for every own service.
- The **.NET Aspire AppHost** is the source of truth for the topology.
- Docker Compose files are **generated** from the AppHost, later Kubernetes
  manifests as well. They are never edited by hand.

## Consequences

- One place describes the topology; local runs, tests and deployments agree.
- Everything must stay runnable through the AppHost.
- Generated Compose output may need profiles or overrides; those are added
  through the AppHost, not by editing the output.
