# Vision

> **Homeport by data-worx** — *Your data never leaves port.*

## What Homeport is

Homeport is a **self-hosted platform for deep business modules**.

- A company pulls a Homeport release — container images and Docker Compose
  files — and runs it in **its own environment**.
- The platform provides what a SaaS product would provide: gateway,
  multi-tenancy, authentication, throttling, usage metering, observability and
  an admin portal.
- **Modules** plug into the platform. Each module covers one *thick* business
  domain in depth, for example:
  - Commerce: EDIFACT (ORDERS, DESADV, INVOIC …), e-invoicing (XRechnung,
    ZUGFeRD)
  - Banking: CAMT, MT940, SEPA files
  - Healthcare billing: validation of § 302 SGB V data files
  - Product compliance: Digital Product Passport
- Besides the catalog modules, an operator can run its **own internal
  modules** on the same contract and get the same SSO, plans, throttling,
  metering and dashboards for them.
- Tenants are typically **departments or customers of the operating company**.
  Homeport counts their usage per module so the company can do **showback or
  chargeback** — internal cost allocation.

## What Homeport is not

- **Not a hosted SaaS.** data-worx does not operate customer instances.
- **Not a collection of thin one-function APIs** ("validate IBAN"). Every
  module solves a hard problem in depth.
- **Not an invoicing system.** Homeport meters and exports usage; the
  operator's own ERP or accounting creates invoices.

## Why

1. **Learning project first.** Homeport is where modern SaaS technology is
   applied in practice: containerisation, observability, service discovery,
   usage metering, throttling, multi-tenancy, supply-chain security. A
   solution that teaches something is preferred over a black box — never at
   the cost of correctness or security.
2. **Monetisation is an option, not the goal.** Possible later: a commercial
   licence (dual licensing), support, customising, white-label branding.
3. **Data sovereignty.** Business data — invoices, bank statements, billing
   files — never leaves the operator's infrastructure. This is the core promise
   and the claim.

## How it fits together

```text
                 ┌──────────────── operator's environment ────────────────┐
  client  ──►    │  Gateway (YARP)                                        │
 (token /        │   ├─ authN: OIDC JWT / client credentials (API keys)   │
  API key)       │   ├─ tenant resolution → TenantId, ClientId            │
                 │   ├─ throttling + quotas  ──► Valkey                   │
                 │   ├─ dynamic routes       ◄── Registry                 │
                 │   └─ UsageRecorded event  ──► RabbitMQ                 │
                 │                                                        │
                 │  Registry        modules register + heartbeat          │
                 │  ControlPlane    tenants, plans, clients, keys (API)   │
                 │  Metering        consumes usage, aggregates, exports   │
                 │  Portal          Blazor admin UI → ControlPlane        │
                 │                                                        │
                 │  Modules         <module>-api  (+ <module>-worker)     │
                 │                                                        │
                 │  PostgreSQL · RabbitMQ · Valkey · Keycloak (dev)       │
                 │  OTLP ──► operator's monitoring or Grafana profile     │
                 └────────────────────────────────────────────────────────┘
```

This is the target picture. What exists today is in
[architecture.md](architecture.md); the decisions behind it are in
[adr/](adr/README.md).
