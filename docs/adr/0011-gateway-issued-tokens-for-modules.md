# ADR-0011: Gateway-issued tokens for modules

- Status: Accepted
- Date: 2026-10-22

## Context

ADR-0004 lets the gateway pass the caller's identity to a module as the
plain headers `X-Homeport-Tenant-Id` and `X-Homeport-Client-Id`. A module
trusts them because it is reachable only on the internal network. Anything
else that reaches that network can set any tenant, which defeats the tenant
isolation the platform promises. A module also receives neither subject nor
scopes, so it cannot authorise individual endpoints.

## Decision

- The gateway authenticates the caller against the operator's identity
  provider as before (ADR-0006).
- For every request it forwards, the gateway issues its own **short-lived,
  signed token** and sends it to the module as a bearer token.
- The token's audience is the module id. Its claims carry **tenant, client,
  subject and scopes**.
- The gateway publishes its signing keys as a JWKS. Modules validate the
  token against those keys and reject a request without a valid token.
- The token is the **only identity source** a module may trust. The gateway
  no longer sets identity headers, and still strips every incoming
  `X-Homeport-*` header.
- Modules authorise per endpoint from the token's claims. They never talk to
  the identity provider.

This supersedes the identity-header bullet of ADR-0004.

## Consequences

- Reaching the internal network is no longer enough to act as a tenant.
- Modules stay independent of the identity provider and of the credential
  type; API keys work without a special path.
- The gateway becomes a token issuer: it holds a signing key, rotates it and
  serves the JWKS. Key rotation needs its own design before v0.1.
- Every module validates a token per request; the SDK provides this for .NET.
