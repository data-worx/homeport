# ADR-0019: Tokens for forwarded platform requests

- Status: Accepted
- Date: 2026-11-07

## Context

The gateway calls a platform service in two different roles.

- **As itself.** It reads the route table from the registry and a
  client's tenant and plan from the control plane. ADR-0014 covers this
  with a service token: audience the called component, subject
  `homeport-gateway`, no tenant.
- **On behalf of a client.** `GET /platform/v1/usage` (ADR-0013) is a
  client's request that the gateway forwards to metering, which must serve
  that client's tenant only. A service token names no tenant, and the
  client's own token must not leave the gateway (ADR-0011).

A module already receives a forwarded request with a token that names the
caller (ADR-0011). Nothing says what a platform service receives.

## Decision

- For every client request it forwards to a platform service, the gateway
  issues a **caller token** with the claims of a module token — tenant,
  client, subject and scopes — and replaces the client's token with it.
- Its audience is the platform service, such as `homeport-metering`. It is
  issued per request, with the lifetime of a module token.
- The service validates it against the gateway's JWKS like a service token
  (ADR-0014) and authorises the endpoint from its claims. An endpoint that
  serves a tenant's data requires the tenant claim and the scope the
  gateway checked, and serves that tenant only.
- A service token stays what ADR-0014 says: the gateway calling as itself.
  It carries no tenant, so it never reaches a tenant's data through an
  endpoint that requires one.
- Which platform routes the gateway forwards, and to which service, is
  configured in the gateway, not registered: platform services do not use
  the registry.

This supplements ADR-0014; it supersedes nothing.

## Consequences

- One token shape for every forwarded request, module or platform; the
  gateway issues both from the same code and key.
- A platform service tells the gateway's own calls from forwarded ones by
  their claims, and needs nothing but the JWKS to do so.
- Reaching metering on the internal network is still not enough to read a
  tenant's usage: a valid token for its audience with that tenant is.
- A platform service with an endpoint for forwarded requests checks the
  scope again, after the gateway did. The check is duplicated by design.
