#!/usr/bin/env bash
# SPDX-License-Identifier: AGPL-3.0-or-later
#
# Releases every own image of deploy/compose/docker-compose.yaml (ADR-0009):
# builds it from its project, scans it with Trivy - High or Critical fails
# before anything is pushed - pushes it to GHCR, signs it with cosign and
# attaches its CycloneDX SBOM as a signed attestation.
#
# Signing uses the key pair whose public half is cosign.pub, and no
# transparency log, so an operator verifies with cosign.pub alone, without
# network access to Sigstore.
#
# Writes into the output directory:
#   images.env          <NAME>_IMAGE=<image>@<digest>, one line per own image
#   sbom/<image>.cdx.json
#
# Needs the .NET SDK, Docker logged in to the registry, Trivy and cosign on
# the PATH, and COSIGN_PRIVATE_KEY and COSIGN_PASSWORD in the environment.
#
# Usage:  bash .github/scripts/release-images.sh <image tag> <output directory> [registry]
#         (registry default ghcr.io/data-worx)
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

. .github/scripts/own-images.sh

tag="${1:?usage: release-images.sh <image tag> <output directory> [registry]}"
out="${2:?usage: release-images.sh <image tag> <output directory> [registry]}"
registry="${3:-ghcr.io/data-worx}"

: "${COSIGN_PRIVATE_KEY:?COSIGN_PRIVATE_KEY is not set}"
: "${COSIGN_PASSWORD:?COSIGN_PASSWORD is not set}"

read_compose_images

mkdir -p "${out}/sbom"
: > "${out}/images.env"

for name in $(printf '%s\n' "${!own[@]}" | sort); do
  project="${own[${name}]}"
  repository="$(image_repository "${project}")"
  image="${registry}/${repository}"

  echo "::group::Build ${image}:${tag} from ${project}"
  # PublishRepositoryUrl labels the image with this repository and commit, so
  # GHCR links the package to the repository.
  dotnet publish "${project}" -t:PublishContainer --nologo \
    -p:ContainerImageTag="${tag}" -p:PublishRepositoryUrl=true
  docker tag "${repository}:${tag}" "${image}:${tag}"
  echo "::endgroup::"

  echo "::group::Scan ${image}:${tag}"
  trivy image --quiet --scanners vuln --severity HIGH,CRITICAL \
    --ignorefile .trivyignore --exit-code 1 "${image}:${tag}"
  trivy image --quiet --format cyclonedx \
    --output "${out}/sbom/${repository}.cdx.json" "${image}:${tag}"
  echo "::endgroup::"

  echo "::group::Push and sign ${image}:${tag}"
  docker push --quiet "${image}:${tag}"
  digest="$(docker inspect --format '{{range .RepoDigests}}{{println .}}{{end}}' "${image}:${tag}" \
    | sed -n "s|^${image}@||p")"
  cosign sign --yes --key env://COSIGN_PRIVATE_KEY \
    --use-signing-config=false --tlog-upload=false "${image}@${digest}"
  cosign attest --yes --key env://COSIGN_PRIVATE_KEY \
    --use-signing-config=false --tlog-upload=false \
    --type cyclonedx --predicate "${out}/sbom/${repository}.cdx.json" "${image}@${digest}"
  echo "::endgroup::"

  echo "${name}_IMAGE=${image}:${tag}@${digest}" >> "${out}/images.env"
done

echo "Released:"
sed 's/^/  /' "${out}/images.env"
