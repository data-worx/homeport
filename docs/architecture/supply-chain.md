# The supply chain

How Homeport keeps track of what its images contain, and why it is set up
this way. The decision is
[ADR-0009](adr/0009-supply-chain-security.md); the binding rules are in
[`AGENTS.md`](../../AGENTS.md#supply-chain).

## What is scanned

Everything an operator runs from [`deploy/compose/`](../../deploy/compose/):
the third-party images exactly as they are pinned there, and Homeport's own
images, built from their projects the way a release builds them. The
[*Supply chain* workflow](../../.github/workflows/supply-chain.yml) runs
[`.github/scripts/scan-images.sh`](../../.github/scripts/scan-images.sh),
which reads the image list from the compose file rather than keeping one of
its own. An own image appears there as a variable such as `${GATEWAY_IMAGE}`;
the script maps it to its project through the table in
[`own-images.sh`](../../.github/scripts/own-images.sh), which the release
shares, and fails on a variable it cannot map. A new service therefore cannot
slip past the scan. The image is named by `ContainerRepository` in its
project.

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

## Releases

A tag releases. The [*Release* workflow](../../.github/workflows/release.yml)
runs on a tag `vX.Y.Z`, or `vX.Y.Z-rc.N` for a candidate such as
`v0.1.0-rc.1`, and [`check-release.sh`](../../.github/scripts/check-release.sh)
refuses it unless the tagged commit is on `main` — or on `release/X.Y` for a
fix to an older line — and `CHANGELOG.md` has its section `## [X.Y.Z]`. That section
becomes the release notes, so no release goes out without them. The version
comes from the tag alone: the repository carries the placeholder
`0.0.0-dev`, and the release build takes the version as its one input.
Before anything is built for the release, the gates run on the tagged
commit, since a tag may point at a commit no pull request was built from.
The steps a maintainer takes are in [releasing](../releasing.md).

[`release-images.sh`](../../.github/scripts/release-images.sh) then builds
every own image from the table in `own-images.sh` with that version — the
same images the scan covers. Each is scanned before anything is pushed, so a
High or Critical finding stops the release, and only then pushed to
`ghcr.io/data-worx/<image>`, signed with cosign and given its SBOM as a
signed CycloneDX attestation. The GitHub release carries the compose file
with every own image pinned by digest, the `deploy/` directory it needs, and
one SBOM per image.

GHCR creates the package of an image private when the image is pushed for
the first time, and GitHub offers no API to change that. After the first
release of a new own image the maintainer makes its package public by hand,
in the package settings, so an operator can pull it without an account. The
organisation already allows public container packages.

Signing uses a key pair rather than Sigstore's keyless signing, and writes
no entry into Sigstore's public transparency log. An installation without
internet access verifies an image with `cosign.pub` from the repository and
access to its registry alone, as [verifying an image](../operators/verifying-images.md)
shows. The price is that trust rests on the key: there is no public record
of what was signed. The private key and its password are the repository
secrets `COSIGN_PRIVATE_KEY` and `COSIGN_PASSWORD`, and no workflow but
the release reads them.

Nothing is signed or pushed outside a release: the scan builds the same
images on every pull request without pushing them. The scripts the release
runs are tested by the `scripts` gate.

## Licence notices

The MIT, Apache-2.0, BSD and PostgreSQL licences of the packages Homeport
ships require their copyright notices and licence texts to travel with the
software. One file does that for every own image:
[`THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md) in the repository
root, generated by `.devkit/generate-third-party-notices.cs` from what each
project in `NOTICES_PROJECTS` in `.devkit/gates.sh` actually publishes. It is
the union of those projects, every package and version listed once. The
`notices` gate regenerates it and fails while the committed file is stale.

Every project that becomes an image sets `ShipsThirdPartyNotices` in its
project file, and `Directory.Build.targets` puts the file into its publish
output, so the image carries it at `/app/THIRD-PARTY-NOTICES.md`. The SDK
builds an image from the publish output alone, so a path outside `/app/` is
not available. A chiseled image has no shell; `docker cp` streams the file
as a tar archive, which `tar` unpacks:

```bash
docker cp "$(docker create <image>)":/app/THIRD-PARTY-NOTICES.md - | tar -xO
```

Two things are deliberately not in the file. The third-party images —
PostgreSQL, RabbitMQ, Valkey, Keycloak — are pulled by digest from their
publishers, and Homeport does not redistribute them. The content of the
chiseled base images is listed in the SBOM of each own image.

A package offered under a choice of licences, such as RabbitMQ.Client under
`Apache-2.0 OR MPL-2.0`, is shipped under one of them; the choice is recorded
in `.devkit/third-party-notices.overrides.json`.

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

The tests start the same images: the new `tag@digest` is copied into
[`ContainerImages`](../../src/Tests/Homeport.Testing/Containers/ContainerImages.cs)
in the same pull request, so no test runs against an image an operator never
runs.

Actions are pinned by commit SHA, with the version in a comment, because a
tag can be moved to other code after it was reviewed.

Packages released together arrive in one pull request: Aspire, OpenTelemetry
and WolverineFx each form a group, so `main` never carries mixed versions of
one family. `Microsoft.EntityFrameworkCore.Design` is ignored. It is pinned to
the EF Core version Aspire brings, and on its own an update breaks the build,
so it is moved by hand together with an Aspire update. Every pull request is
titled with the prefix `chore`, because the squash merge makes the title the
commit on `main`.

An update of a package that a shipped project publishes fails the `notices`
gate until [`THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md) is
regenerated on the same branch and committed:

```bash
dotnet run --file .devkit/generate-third-party-notices.cs -- \
  $(sed -n '/^NOTICES_PROJECTS=(/,/^)/s/^  "\(.*\)"$/--project \1/p' .devkit/gates.sh)
```
