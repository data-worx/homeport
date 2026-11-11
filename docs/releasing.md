# Releasing

How a merged `main` becomes a Homeport release: the changelog cut, the
candidates, the release, and removing a candidate nobody needs any more. It
is written for the maintainer. The rules are `core.releasing` and the
*Releasing* section of [`AGENTS.md`](../AGENTS.md); how the workflow builds,
scans and signs the images is in [the supply chain](architecture/supply-chain.md#releases).

Homeport releases **trunk with tags**: candidates and releases are tagged on
`main`. What a release produces is a set of container images in GHCR plus a
GitHub release carrying the compose file pinned to them and the deploy
bundle — see [verifying images](operators/verifying-images.md#what-a-release-carries).

## The version comes from the tag

`src/Directory.Build.props` carries the placeholder `0.0.0-dev`, and every
build from source reports it. Nothing in the repository is bumped for a
release. The *Release* workflow takes the version from the tag — `0.1.0`
from `v0.1.0`, `0.1.0-rc.1` from `v0.1.0-rc.1` — and passes it to the build
as `-p:Version`, so the binaries report it (`0.1.0-rc.1+<commit>`) and the
images are tagged with it.

The workflow refuses a tag unless:

- it is `vX.Y.Z` or `vX.Y.Z-rc.N`, with `N` from 1;
- the tagged commit is on `main`, or on `release/X.Y` for a fix to an older
  line (below);
- `CHANGELOG.md` has the section `## [X.Y.Z]`, for a candidate as well as
  for the release.

Then it runs the gates on the tagged commit, and only if they pass does it
build, scan, push and sign the images and create the GitHub release.
[`check-release.sh`](../.github/scripts/check-release.sh) holds the checks,
and the `scripts` gate tests them.

## 1. Cut the changelog

A release starts with a pull request that turns `## [Unreleased]` into
`## [X.Y.Z] - <date>`, with a fresh, empty `## [Unreleased]` above it, and
changes nothing else but what has to be true once the version is out — the
`README.md`, the docs. No change to code or behaviour goes into it.

## 2. Tag a candidate

After the cut is merged, tag its commit on `main`:

```bash
git checkout main && git pull
git tag v0.1.0-rc.1
git push origin v0.1.0-rc.1
```

The *Release* workflow publishes the images tagged `0.1.0-rc.1` and a GitHub
release marked as a prerelease, never as the latest. Its notes are the cut
section, headed *Candidate for 0.1.0, not for production.*

The candidate is checked as it is. A fix found on it lands on `main` through
its own pull request, adds its line to the `## [X.Y.Z]` section — not to
`[Unreleased]` — and is followed by the next candidate, `v0.1.0-rc.2`.

## 3. Tag the release

When a candidate is accepted, tag `vX.Y.Z` **on that candidate's commit**,
not on the tip of `main`:

```bash
git tag v0.1.0 v0.1.0-rc.2^{commit}
git push origin v0.1.0
```

The release is built from the same commit as the accepted candidate and
differs from it in its version only. Its notes are the `## [X.Y.Z]` section,
and it is marked as the latest.

After the first release of a new own image, make its GHCR package public by
hand, in the package settings, as [the supply chain](architecture/supply-chain.md#releases)
describes.

## 4. Remove a discarded candidate

A candidate that is not needed any more — every candidate before the
accepted one, and one that turned out broken — leaves nothing behind that
can be fetched. A registry has no drafts: its images could be pulled from the
moment they were pushed, so deleting the GitHub release and the tag alone
leaves them downloadable.

Deleting package versions needs the `delete:packages` scope, once:

```bash
gh auth refresh -s read:packages,delete:packages
```

Then remove the GitHub release, the tag, and in every own image's package
the version tagged with the candidate's version together with its signature
and attestation, which cosign stores as tags starting with `sha256-<digest>`:

```bash
version=0.1.0-rc.1
gh release delete "v${version}" --yes --cleanup-tag

for package in homeport-gateway homeport-registry homeport-control-plane \
               homeport-metering homeport-module-echo; do
  versions="/orgs/data-worx/packages/container/${package}/versions"
  digest="$(gh api --paginate "${versions}" \
    --jq ".[] | select(.metadata.container.tags | index(\"${version}\")) | .name")"
  [ -n "${digest}" ] || { echo "${package}: no ${version}"; continue; }
  prefix="sha256-${digest#sha256:}"
  gh api --paginate "${versions}" \
    --jq ".[] | select(.name == \"${digest}\" or any(.metadata.container.tags[]; startswith(\"${prefix}\"))) | .id" \
  | while read -r id; do
      gh api -X DELETE "${versions}/${id}" && echo "${package}: deleted version ${id}"
    done
done
```

The package list is the table in
[`own-images.sh`](../.github/scripts/own-images.sh). A version of an image
that has several platforms or an attestation lists the manifests it points at
as untagged versions of the same package; check what is left in the package
settings and delete those as well:

```bash
gh api --paginate /orgs/data-worx/packages/container/homeport-gateway/versions \
  --jq '.[] | select(.metadata.container.tags == []) | "\(.id) \(.name) \(.created_at)"'
```

## Fixing an older line

A fix for a release that is no longer the latest is made on a `release/X.Y`
branch, cut from the last tag of that line, and the fix release is tagged
there:

```bash
git checkout -b release/0.1 v0.1.0
git push origin release/0.1
```

The fix itself reaches `release/0.1` through a pull request from a `fix/`
branch, and `main` through its own pull request. The workflow accepts a tag
`v0.1.Z` or `v0.1.Z-rc.N` on `release/0.1` and never marks its GitHub
release as the latest. The changelog section `## [0.1.Z]` is cut on
`release/0.1` as in step 1, and reaches `main` with the fix.
