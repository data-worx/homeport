# Running Homeport locally

Two ways to start Homeport on one machine: through the .NET Aspire AppHost,
which is how Homeport is developed, and through the generated Docker Compose
file, which is how it is deployed without .NET.

Today both start the infrastructure — PostgreSQL, RabbitMQ, Valkey and
Keycloak — the gateway, the registry and the Echo module. Homeport's other services follow
with the next milestones.

Keycloak is part of this setup for development and tests only. In a real
installation the operator brings their own identity provider.

## Prerequisites

- Docker, running
- For the AppHost: the .NET 10 SDK, and a trusted development certificate
  (`dotnet dev-certs https --trust`, once per machine)
- For Compose: a shell with `openssl` — on Windows, Git Bash

## With the AppHost

From the repository root:

```bash
dotnet run --project src/Aspire/Homeport.Aspire.AppHost
```

In Visual Studio or Rider, make `Homeport.Aspire.AppHost` the startup project and
start it; the IDE opens the dashboard itself. Both use the launch profile
`https`; the profile `http` runs without TLS.

The output ends with the dashboard address and a login link:

```text
Login to the dashboard at https://localhost:17000/login?t=<token>
Distributed application started. Press Ctrl+C to shut down.
```

Open the login link. The dashboard lists `postgres`, `rabbitmq`, `valkey`,
`keycloak`, `registry`, `echo` and `gateway`; each reaches the state *Healthy*
within a minute, longer on the first run while the images are pulled. Echo
registers at the registry once it has started, and the gateway routes to it
a few seconds later; see [the registry](registry.md).

The gateway is the only resource with an external endpoint; its address is in
the dashboard. Call Echo through it, as
[calling a module](../api-consumers/calling-a-module.md) describes.

`Ctrl+C` stops everything and removes the containers.

### Credentials

The AppHost generates the passwords of all four, the gateway's signing key
and Echo's registration key on its first start and keeps them in the .NET user secrets of the AppHost
project, on your machine only.
List them from the repository root:

```bash
dotnet user-secrets list --project src/Aspire/Homeport.Aspire.AppHost
```

```text
Parameters:valkey-password = …
Parameters:rabbitmq-password = …
Parameters:postgres-password = …
Parameters:keycloak-password = …
Parameters:gateway-signing-key = -----BEGIN PRIVATE KEY-----…
Parameters:echo-registration-key = …
```

The dashboard shows the same values under the parameter resources, masked
until revealed. `gateway-signing-key` is the key the gateway signs its tokens
with, under the key id `local`; see [the gateway's signing key](signing-key.md).
`echo-registration-key` is the key Echo registers with; see
[the registry](registry.md).

Keycloak's admin console is at <http://localhost:8080/admin>. Sign in as
`admin` with `keycloak-password`. Keycloak takes the longest of the four to
become healthy, around half a minute.

## With Docker Compose

The compose file in [`deploy/compose/`](../../deploy/compose/) is generated
from the AppHost. Do not edit it; a change belongs in the AppHost, and the
`compose` gate fails until the file is regenerated.

No image of Homeport's own is published yet, so build the images of the
gateway, the registry and the Echo module locally first, from the repository
root:

```bash
dotnet publish src/Platform/Gateway/Homeport.Platform.Gateway.Api -t:PublishContainer
dotnet publish src/Platform/Registry/Homeport.Platform.Registry.Api -t:PublishContainer
dotnet publish src/Modules/Echo/Homeport.Modules.Echo.Api -t:PublishContainer
```

The last line of each names its image:

```text
… "homeport-platform-gateway-api:latest" …
… "homeport-platform-registry-api:latest" …
… "homeport-modules-echo-api:latest" …
```

The compose file reads its settings from `deploy/compose/.env`, which is never
committed. Create it once with the images and ports, a random password for
every `_PASSWORD` variable listed in `.env.example`, a signing key for the
gateway, as [the gateway's signing key](signing-key.md) describes, and a
registration key for Echo, as [the registry](registry.md) describes:

```bash
cd deploy/compose
{
  printf 'ECHO_IMAGE=homeport-modules-echo-api:latest\nECHO_PORT=8080\n'
  printf 'GATEWAY_IMAGE=homeport-platform-gateway-api:latest\nGATEWAY_PORT=8080\n'
  printf 'REGISTRY_IMAGE=homeport-platform-registry-api:latest\nREGISTRY_PORT=8080\n'
  sed -n 's/^\([A-Z_]*_PASSWORD\)=$/\1/p' .env.example | while read -r name; do
    printf '%s=%s\n' "$name" "$(openssl rand -hex 24)"
  done
  printf 'GATEWAY_SIGNING_KEY=%s\n' \
    "$(openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 | tr -d '\r\n')"
  printf 'GATEWAY_SIGNING_KEY_ID=local\n'
  printf 'GATEWAY_ADDITIONAL_PUBLIC_KEY=\nGATEWAY_ADDITIONAL_KEY_ID=\n'
  printf 'ECHO_REGISTRATION_KEY=%s\n' "$(openssl rand -hex 32)"
} > .env
```

Check it: every line but the two `GATEWAY_ADDITIONAL_` ones has a value.

```bash
grep -c '=.' .env      # prints 13
```

Start, then check:

```bash
docker compose up -d
docker compose ps --format '{{.Service}} {{.State}}'
```

```text
echo running
gateway running
keycloak running
postgres running
rabbitmq running
registry running
valkey running
```

Only the gateway publishes a port to the host, chosen by Docker on every
start. Ask for it, then call Echo through it. Until Echo has registered and
the gateway has read its routes, a few seconds after the start, the call is
answered with `404`:

```bash
docker compose port gateway 8080      # prints 0.0.0.0:<port>
curl -s -X POST -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' http://localhost:<port>/modules/echo/v1/echo
```

```json
{"hello":"Homeport"}
```

Every other service is reachable only on the compose network. Stop and remove
everything with:

```bash
docker compose down
```

After pulling changes to the gateway, the registry or Echo, rebuild its image
before starting. When `.env.example` gains a variable after an update, add it to
`.env` the same way before starting.
