# Homeport

> **Homeport by data-worx** — *Your data never leaves port.*

Homeport is a self-hosted platform for deep business modules. An operator runs
it in their own environment, and it provides what a SaaS product would:
gateway, multi-tenancy, authentication, throttling, usage metering,
observability and an admin portal. Each module covers one thick business
domain in depth — EDIFACT, e-invoicing, bank statement formats, healthcare
billing files.

- **Not a hosted SaaS.** Business data never leaves the operator's
  infrastructure. Nothing phones home.
- **Not an invoicing system.** Homeport meters and exports usage for showback
  and chargeback; it never creates invoices.

More in [docs/vision.md](docs/vision.md).

## Status

Pre-alpha. Nothing runs yet. The first milestone, a walking skeleton from
gateway to metering, is described in [ROADMAP.md](ROADMAP.md).

## Documentation

- [docs/](docs/README.md) — vision, architecture, decision records and the
  module contract
- [AGENTS.md](AGENTS.md) — the binding rules for working on the code
- [CONTRIBUTING.md](CONTRIBUTING.md) — how to contribute
- [SECURITY.md](SECURITY.md) — how to report a vulnerability

Questions and ideas go to
[GitHub Discussions](https://github.com/data-worx/homeport/discussions).

## Licence

Homeport is licensed under the
[GNU Affero General Public License v3.0 or later](LICENSE). Contributions
require a signed [Contributor License Agreement](CLA.md).

The names "Homeport" and "data-worx" and their logos are not licensed with the
code — see [TRADEMARKS.md](TRADEMARKS.md).
