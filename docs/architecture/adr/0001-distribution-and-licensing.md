# ADR-0001: Distribution and licensing

- Status: Accepted
- Date: 2026-10-19

## Context

Homeport processes business data — invoices, bank statements, billing files —
that operators must keep in their own infrastructure. data-worx does not want
the hosting and GDPR burden of running customer instances. Monetisation may
come later through a commercial licence.

## Decision

- Homeport is **self-hosted only**. Docker Compose first, a Helm chart for
  Kubernetes later.
- The code is licensed under **AGPL-3.0-or-later**.
- Contributions require a **CLA** that grants the copyright holder the right to
  relicense. The holder is René Kuss and his successors and assigns.
- The names "Homeport" and "data-worx" and their logos are not licensed with
  the code.

## Consequences

- No hosted offering, no telemetry to data-worx, no dependency on a service
  data-worx operates.
- The CLA keeps dual licensing possible; without a signed CLA a contribution
  cannot be merged.
- AGPL deters operators who would modify Homeport and offer it as a service
  without sharing their changes.
