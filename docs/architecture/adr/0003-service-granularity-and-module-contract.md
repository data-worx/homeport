# ADR-0003: Service granularity and module contract

- Status: Accepted
- Date: 2026-10-19

## Context

Modules must be deployable independently, and the best library for a domain
is not always a .NET one (for example Java validators). At the same time a
small team cannot operate a sprawl of tiny services.

## Decision

- **Coarse-grained services.** One module is one API service plus an optional
  worker. Features inside a module are components, not services.
- The module contract is **language-neutral HTTP**: manifest, health,
  registration and heartbeat, gateway headers, usage reporting. It is
  normative in
  [docs/reference/module-contract/](../../reference/module-contract/README.md).
- A **.NET SDK** (NuGet) implements the contract for .NET modules. It is a
  convenience, not a requirement.
- The **Echo** module is the reference implementation.
- A contract change bumps `contractVersion` and updates SDK, documentation and
  Echo in the same pull request.

## Consequences

- A module can wrap a library in any language.
- The contract must be versioned and kept backward compatible within a
  `contractVersion`.
- Code shared between modules travels through the SDK or
  `Homeport.Contracts`, never through project references between services.
