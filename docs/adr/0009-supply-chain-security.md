# ADR-0009: Supply-chain security

- Status: Accepted
- Date: 2026-10-19

## Context

Operators run Homeport images inside their own perimeter and need reasons to
trust them. The EU Cyber Resilience Act will require vulnerability handling
and a software bill of materials.

## Decision

- `SECURITY.md` with private vulnerability reporting on GitHub and a mail
  fallback.
- Dependabot for packages, actions and image digests.
- An **SBOM** for every release.
- **Trivy** scans every image in the Compose files — own and third-party.
  High or Critical fails CI. Exceptions only in `.trivyignore`, each with a
  reason and an expiry date. A nightly re-scan covers all images.
- Third-party images are **pinned by digest**.
- Own images use **chiseled** .NET base images, run as **non-root**, and are
  **signed with cosign** on release. Images are published to GHCR.

## Consequences

- A newly published vulnerability in a third-party image can turn CI red
  without a code change. That is intended.
- More CI time, and more maintenance of pins and exceptions.
