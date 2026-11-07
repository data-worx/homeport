# Verifying an image

Every release of Homeport publishes its own images to GHCR, each signed with
cosign and carrying its SBOM as a signed attestation
([ADR-0009](../architecture/adr/0009-supply-chain-security.md)). Verify an
image before you run it: a valid signature shows that the image is the one
the Homeport release workflow built, unchanged since.

## The images

| Image | Component |
|---|---|
| `ghcr.io/data-worx/homeport-gateway` | The gateway |
| `ghcr.io/data-worx/homeport-registry` | The registry |
| `ghcr.io/data-worx/homeport-control-plane` | The control plane; its `migrate` step runs the same image |
| `ghcr.io/data-worx/homeport-metering` | Metering; its `migrate` step runs the same image |
| `ghcr.io/data-worx/homeport-module-echo` | The Echo reference module |

Each is tagged with the version it is released as, `0.1.0` for v0.1.0. A
pre-release such as `v0.1.0-rc.1` is tagged `0.1.0-rc.1` and is not meant
for production.

## What a release carries

The [GitHub release](https://github.com/data-worx/homeport/releases) of a
version carries:

- `docker-compose.yaml` — the compose file with every Homeport image pinned
  to the released one by digest, such as
  `ghcr.io/data-worx/homeport-gateway:0.1.0@sha256:…`.
- `homeport-deploy-<version>.tar.gz` — the `deploy/` directory the compose
  file needs: that compose file, `.env.example`, the init scripts of the
  database and the broker, and the Keycloak realm. How to fill `.env` is
  described in [running Homeport locally](running-locally.md#with-docker-compose);
  the `_IMAGE` variables are not needed, since the images are pinned.
- `homeport-<component>.cdx.json` — the CycloneDX SBOM of each image.

## The public key

Images are signed with a key pair. Its public half is
[`cosign.pub`](../../cosign.pub) in the repository root, and is all a
verification needs. Take it from the tag of the release you run, not from
the release page, and keep the copy you checked:

```bash
curl -fsSLo cosign.pub https://raw.githubusercontent.com/data-worx/homeport/v0.1.0/cosign.pub
```

## Verifying

Install [cosign](https://docs.sigstore.dev/cosign/system_config/installation/)
3; the commands here are tested with 3.1.4. Then verify an image:

```bash
cosign verify --key cosign.pub --insecure-ignore-tlog=true \
  ghcr.io/data-worx/homeport-gateway:0.1.0
```

It prints the verified signature as JSON and exits with `0`; a missing
signature or one made with another key fails with a non-zero exit code.

The images are signed without an entry in Sigstore's public transparency
log, so a verification needs nothing but `cosign.pub` and access to the
registry — no network access to Sigstore, which suits an installation
without internet access. `--insecure-ignore-tlog=true` tells cosign not to
look for that entry; the warning it prints about skipping the transparency
log is expected. The trust rests on the key alone.

To verify exactly what the compose file of a release runs, verify every
image it pins:

```bash
sed -n -E 's|^[[:space:]]+image:[[:space:]]*"(ghcr.io/data-worx/[^"]+)"$|\1|p' docker-compose.yaml \
  | sort -u \
  | while read -r image; do
      cosign verify --key cosign.pub --insecure-ignore-tlog=true "${image}" > /dev/null \
        && echo "verified ${image}"
    done
```

The expected output is one `verified` line per Homeport image, five in all.

## The SBOM

The SBOM of each image is attached to it as a CycloneDX attestation, signed
with the same key. Verify it and read it from the registry:

```bash
cosign verify-attestation --key cosign.pub --insecure-ignore-tlog=true \
  --type cyclonedx ghcr.io/data-worx/homeport-gateway:0.1.0
```

The same SBOM is attached to the GitHub release as
`homeport-gateway.cdx.json`.

## Mirroring into your own registry

`cosign copy` copies an image together with its signature and attestation,
so a mirrored image verifies the same way:

```bash
cosign copy ghcr.io/data-worx/homeport-gateway:0.1.0 registry.example.internal/homeport-gateway:0.1.0
cosign verify --key cosign.pub --insecure-ignore-tlog=true registry.example.internal/homeport-gateway:0.1.0
```
