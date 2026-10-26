# ADR-0006: Authentication

- Status: Accepted
- Date: 2026-10-19

## Context

Companies expect single sign-on against their existing identity provider.
Machine clients need a secure default that is also easy to onboard later.

## Decision

- **Humans:** OIDC against the operator's identity provider (Entra ID,
  Keycloak, …). Keycloak ships in the dev/test compose only.
- **Machines:** OAuth 2.0 **client credentials** first, **API keys** later.
- The gateway normalises both to `TenantId` and `ClientId`; modules never see
  the original credential type.

## Consequences

- No own user store and no password handling.
- API keys need their own decision on format and rotation before they ship.
- Modules stay independent of the authentication mechanism.
