# ADR-0004: Gateway and service discovery

- Status: Accepted
- Date: 2026-10-19

## Context

Every request needs authentication, tenant resolution, throttling and
metering. Starting a module container should make it available without a
configuration change. Throttling and metering are explicit learning goals.

## Decision

- An own gateway built on **YARP** in ASP.NET Core.
- Modules **self-register** at the **Registry** with their manifest and send
  heartbeats (default every 10 s, TTL 30 s). The gateway updates its routes
  live from the registry.
- If the registry is unavailable, the gateway keeps serving the **last known
  routes**.
- The gateway strips incoming `X-Homeport-*` headers and sets
  `X-Homeport-Tenant-Id` and `X-Homeport-Client-Id`.

## Consequences

- Starting a module container is enough to make it reachable; stopping it
  removes the route after the TTL.
- The gateway is on the path of every request and must run as several
  replicas; its shared state lives outside the process.
- No off-the-shelf API gateway product to configure — and none to lean on.
