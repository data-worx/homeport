# ADR-0010: Admin portal

- Status: Accepted
- Date: 2026-10-19

## Context

Operators need a UI for tenants, plans, clients, modules and usage. Frontend
technology is not a learning goal of this project.

## Decision

- **Blazor** with **MudBlazor**, themed through the Homeport design tokens.
- The portal talks to the ControlPlane API only; it holds no business logic.

## Consequences

- One language and one toolchain across the whole platform.
- The look is bound to what MudBlazor can theme; custom components are the
  exception.
