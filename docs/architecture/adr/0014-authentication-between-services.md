# ADR-0014: Authentication between services

- Status: Accepted
- Date: 2026-10-26

## Context

`core.security` requires every endpoint to be authorised explicitly. The
platform services call each other: the gateway asks the registry for its
routes and the control plane for a client's tenant and plan. Reaching the
internal network must not be enough to read the route table or a tenant's
plan.

More callers will follow. The admin portal talks to the control plane
(ADR-0010), and modules may one day call platform services or each other.
Their mechanism depends on questions that are open today, but each of them
has to meet the same bar.

[ADR-0011](0011-gateway-issued-tokens-for-modules.md) already makes the
gateway a token issuer with a signing key and a published JWKS, so modules
can validate the requests it forwards. This ADR supplements it for traffic
between Homeport's own components.

## Decision

**Principle.** Every call between components is authenticated as its
concrete caller — HTTP, registration and messaging alike:

- Every call carries the identity of the component that makes it.
  Reaching the internal network grants nothing.
- No secret is shared between callers.
- Tokens are bound to the audience they are issued for and short-lived.
- A long-lived secret is allowed only when it belongs to exactly one
  caller, serves one purpose and rotates without downtime. Module
  registration keys (ADR-0012) and the broker credentials per service meet
  this.

**Mechanism in v0.1: gateway service tokens.**

- The gateway issues itself short-lived service tokens, signed with the
  same key and published in the same JWKS as the module tokens. Their
  lifetime is at most 5 minutes; the gateway caches and renews them.
- The audience is the called component (`homeport-registry`,
  `homeport-control-plane`, …), the subject `homeport-gateway`.
- A platform service validates the token against the gateway's JWKS and
  rejects a request without a valid token for its own audience with `401`.
- Not covered: a module calling the registry, which uses its registration
  key (ADR-0012), and messages through RabbitMQ, which use broker
  credentials per service.

**Further callers.** Only the gateway issues service tokens in v0.1. The
mechanism for any other caller — the portal, a module — is decided when the
first one appears, and must meet the principle.

**mTLS** between services was considered and deferred: certificate
management outweighs its benefit for v0.1. The question stays open in
[discussion #4](https://github.com/data-worx/homeport/discussions/4).

## Consequences

- Every endpoint between platform services is authorised, with one key and
  one JWKS for all tokens the gateway issues.
- A platform service needs nothing but the JWKS to validate a call; no
  secret per pair of services.
- A future caller cannot be let in on network trust or a shared secret; the
  principle rules out the shortcuts.
- **Known gap:** traffic between components is authenticated but not
  encrypted by Homeport. Until discussion #4 is decided, encryption on the
  wire is left to the operator's network, such as a service mesh.
