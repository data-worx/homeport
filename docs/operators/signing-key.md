# The gateway's signing key

The gateway signs every token it issues: the service tokens it calls
Homeport's platform services with
([ADR-0014](../architecture/adr/0014-authentication-between-services.md)),
and, from a later release, the tokens it forwards to modules
([ADR-0011](../architecture/adr/0011-gateway-issued-tokens-for-modules.md)).
Whoever holds the key can act as the gateway towards every component, so it
is the most sensitive secret of an installation.

The gateway publishes the matching public keys at `/.well-known/jwks.json`.
Platform services and modules validate tokens against them and never see the
private key.

## Generating a key

The key is an ECDSA key on the curve P-256; the gateway signs with ES256.
Generate one with `openssl`, on Windows from Git Bash:

```bash
openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 -out signing-key.pem
```

The file starts with `-----BEGIN PRIVATE KEY-----`. Keep it in your secret
store and delete the local copy once it is configured.

Each key gets a **key id** (`kid`) of your choice. Tokens name it in their
header, and validators pick the key by it. Use a value that is unique for the
lifetime of the installation, such as the month it was created: `2026-10`.

## Configuring the key

| Setting | Environment variable | Compose `.env` | Required |
|---|---|---|---|
| Private key, PEM | `Signing__Key` | `GATEWAY_SIGNING_KEY` | yes |
| Its key id | `Signing__KeyId` | `GATEWAY_SIGNING_KEY_ID` | yes |
| Additional public key, PEM | `Signing__AdditionalPublicKey` | `GATEWAY_ADDITIONAL_PUBLIC_KEY` | no |
| Its key id | `Signing__AdditionalKeyId` | `GATEWAY_ADDITIONAL_KEY_ID` | with the key |

The gateway does not start without a valid signing key and key id. It stops
with a message naming the setting that is missing or wrong, for example
`Signing:Key is not a P-256 private key in PEM.`

An environment file holds one value per line, so put the PEM on one line.
The gateway reads it without line breaks:

```bash
printf 'GATEWAY_SIGNING_KEY=%s\n' "$(tr -d '\r\n' < signing-key.pem)" >> .env
printf 'GATEWAY_SIGNING_KEY_ID=%s\n' '2026-10' >> .env
```

Check it: the gateway publishes the key id.

```bash
curl -s http://localhost:<port>/.well-known/jwks.json
```

```json
{"keys":[{"kty":"EC","use":"sig","alg":"ES256","kid":"2026-10","crv":"P-256","x":"…","y":"…"}]}
```

The additional public key is published, but never signed with. It is what
makes a rotation possible without downtime, and stays empty otherwise.

The AppHost generates a signing key with the key id `local` on its first run
and keeps it in its user secrets, as it does the passwords; see
[running Homeport locally](running-locally.md).

## Rotating the key

Validators fetch the JWKS at the latest every five minutes, and immediately
when a token names a key id they do not know. That immediate fetch runs in the
background, so the request that triggered it is rejected with `401`. A planned
rotation therefore publishes the new key before the gateway signs with it.

### Planned rotation

Generate the new key and derive its public key:

```bash
openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 -out next-key.pem
openssl pkey -in next-key.pem -pubout -out next-public.pem
```

1. **Publish the new key.** Keep signing with the current key and set the new
   public key as the additional key:

   ```text
   GATEWAY_SIGNING_KEY=<current private key>
   GATEWAY_SIGNING_KEY_ID=2026-10
   GATEWAY_ADDITIONAL_PUBLIC_KEY=<next-public.pem on one line>
   GATEWAY_ADDITIONAL_KEY_ID=2027-04
   ```

   Restart the gateway and check that the JWKS lists both key ids. Wait five
   minutes, so every validator has fetched it.

2. **Switch.** Sign with the new key and keep the old public key published,
   for the tokens still in flight:

   ```bash
   openssl pkey -in signing-key.pem -pubout -out previous-public.pem
   ```

   ```text
   GATEWAY_SIGNING_KEY=<next-key.pem on one line>
   GATEWAY_SIGNING_KEY_ID=2027-04
   GATEWAY_ADDITIONAL_PUBLIC_KEY=<previous-public.pem on one line>
   GATEWAY_ADDITIONAL_KEY_ID=2026-10
   ```

   Restart the gateway. Wait five minutes, the longest a token lives.

3. **Withdraw the old key.** Empty both additional settings and restart the
   gateway. The JWKS lists the new key id only.

No request is rejected at any step. With more than one gateway replica,
complete each step on every replica before waiting.

### When the key is compromised

Do not publish the old key any longer. Generate a new key, set it as the
signing key with a new key id, leave both additional settings empty and
restart the gateway at once.

- Each validator rejects the first token with the new key with `401`, fetches
  the JWKS and accepts the following ones.
- A validator keeps accepting tokens signed with the compromised key until it
  next fetches the JWKS, **at most five minutes**. Homeport has no way to
  revoke a key faster.
