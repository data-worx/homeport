# The supply chain

How Homeport keeps track of what its images contain, and why it is set up
this way. The decision is
[ADR-0009](adr/0009-supply-chain-security.md); the binding rules are in
[`AGENTS.md`](../../AGENTS.md#supply-chain). Signing and publishing images
come with the release work.

## What is scanned

Everything an operator runs from [`deploy/compose/`](../../deploy/compose/):
the third-party images exactly as they are pinned there, and Homeport's own
images, built from their projects the way a release builds them. The
[*Supply chain* workflow](../../.github/workflows/supply-chain.yml) runs
[`.github/scripts/scan-images.sh`](../../.github/scripts/scan-images.sh),
which reads the image list from the compose file rather than keeping one of
its own. An own image appears there as a variable such as `${GATEWAY_IMAGE}`;
the script maps it to its project through a table, and fails on a variable it
cannot map. A new service therefore cannot slip past the scan.

Trivy fails the run on any High or Critical vulnerability. The workflow runs
on every pull request, on every push to `main`, and nightly: a vulnerability
published after the last change turns the nightly run red without a code
change, which is intended.

Each own image gets a CycloneDX SBOM, uploaded as the build artifact `sbom`.

The script runs locally as well, with the .NET SDK, Docker and Trivy on the
path:

```bash
bash .github/scripts/scan-images.sh
```

## Exceptions

A finding is removed at the source first: a patched version of the image, a
newer digest of the same tag, or a leaner variant. That is why PostgreSQL,
RabbitMQ and Valkey run on their Alpine variants — the Debian ones carry
packages with vulnerabilities that have no fix yet. PostgreSQL is
initialised with ICU as its collation provider, because the Alpine C library
sorts text by byte.

What remains — a finding only the image's maintainers can fix — goes into
`.trivyignore`, in a block that opens with the reason, each entry with an
expiry date:

```text
# keycloak: why these findings are acceptable until the date below.
CVE-2026-12345 exp:2026-12-31
CVE-2026-12346 exp:2026-12-31
```

An entry applies to every scanned image, not only the one it was written for.

Trivy alone would stop ignoring an expired entry and stay silent while the
finding is gone. [`check-trivyignore.sh`](../../.github/scripts/check-trivyignore.sh)
runs first and fails on an entry outside a block with a reason, without an
expiry date, or past it, so every exception is looked at again when it
expires.

## Own images

Every project that becomes an image is built on the chiseled ASP.NET base
image, set once as `ContainerFamily` in `src/Directory.Build.props`. A
chiseled image has no shell and no package manager, which removes most of
what a scanner finds in a full distribution image and most of what an
attacker could use. The SDK picks the `-extra` variant, which keeps ICU and
the time zone data, because no project switches to invariant globalisation.
The image runs as the non-root user `app` (UID 1654).

## Updates

Dependabot proposes updates for NuGet packages, GitHub Actions and the image
digests in the compose file ([`.github/dependabot.yml`](../../.github/dependabot.yml)).

The compose file is generated from the AppHost, so a digest update by
Dependabot is half done: its pull request fails the `compose` gate until the
new digest is copied into
[`Program.cs`](../../src/Aspire/Homeport.Aspire.AppHost/Program.cs) on the
same branch. The AppHost pins each image as `tag@digest`, which the compose
file carries over. Without the tag, Dependabot would propose the digest of
`latest`, a major upgrade nobody asked for; with it, it stays within the tag.

Actions are pinned by commit SHA, with the version in a comment, because a
tag can be moved to other code after it was reviewed.
