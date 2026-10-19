# ADR-0005: Persistence and multi-tenancy

- Status: Accepted
- Date: 2026-10-19

## Context

Operators need a database that is free in production and standard for open
source. Tenants of one installation must never see each other's data, and an
operator should not have to manage one database per tenant.

## Decision

- **PostgreSQL**, accessed through EF Core.
- **Single database**, shared schema. Every tenant-owned table has a non-null
  `TenantId` (uuid).
- Two independent layers of isolation:
  - EF Core **global query filters** on every tenant-owned entity
  - PostgreSQL **row-level security**, driven by the session setting
    `homeport.tenant_id` that an interceptor sets per connection or
    transaction
- Platform-admin access uses a dedicated database role and is audited.
- Every feature that touches tenant data has a cross-tenant leakage test.

## Consequences

- Simple to operate: one database, one backup, one migration path.
- A bug in the application filter is caught by RLS, and the other way round.
- The RLS setup is the deliberate exception to provider-neutral data access.
- A very large tenant cannot be moved to its own database without work.
