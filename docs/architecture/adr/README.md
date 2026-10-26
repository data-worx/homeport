# Architecture decision records

Decisions that are hard to reverse or cross a boundary. When an ADR is
required, and how one is changed, is a rule in
[`AGENTS.md`](../../../AGENTS.md#decisions).

An ADR is never edited once accepted, apart from its status line. A changed
decision is a new ADR that supersedes the old one, and the old one is marked
`Superseded by ADR-NNNN`.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-distribution-and-licensing.md) | Distribution and licensing | Accepted |
| [0002](0002-runtime-and-orchestration.md) | Runtime and orchestration | Accepted |
| [0003](0003-service-granularity-and-module-contract.md) | Service granularity and module contract | Accepted |
| [0004](0004-gateway-and-service-discovery.md) | Gateway and service discovery | Accepted; identity headers superseded by ADR-0011 |
| [0005](0005-persistence-and-multi-tenancy.md) | Persistence and multi-tenancy | Accepted |
| [0006](0006-authentication.md) | Authentication | Accepted |
| [0007](0007-throttling-and-metering.md) | Throttling and metering | Accepted |
| [0008](0008-observability.md) | Observability | Accepted |
| [0009](0009-supply-chain-security.md) | Supply-chain security | Accepted |
| [0010](0010-admin-portal.md) | Admin portal | Accepted |
| [0011](0011-gateway-issued-tokens-for-modules.md) | Gateway-issued tokens for modules | Accepted |

## Template

```markdown
# ADR-NNNN: <decision in a few words>

- Status: Proposed | Accepted | Superseded by ADR-NNNN
- Date: YYYY-MM-DD

## Context

What forces the decision. Facts, not opinions.

## Decision

What we do. One paragraph or a short list.

## Consequences

What becomes easier, what becomes harder, what we give up.
```
