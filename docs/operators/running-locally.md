# Running Homeport locally

Two ways to start Homeport on one machine: through the .NET Aspire AppHost,
which is how Homeport is developed, and through the generated Docker Compose
file, which is how it is deployed without .NET.

Today both start the infrastructure — PostgreSQL, RabbitMQ, Valkey and
Keycloak — the gateway, the registry, the control plane, metering and the
Echo module.
Homeport's other services follow
with the next milestones.

Keycloak is part of this setup for development and tests only. In a real
installation the operator brings their own identity provider.

## Prerequisites

- Docker, running
- For the AppHost: the .NET 10 SDK, and a trusted development certificate
  (`dotnet dev-certs https --trust`, once per machine)
- For Compose: a shell with `openssl` — on Windows, Git Bash
- On Windows: Smart App Control switched off; see
  [Smart App Control](#smart-app-control)

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
`keycloak`, `registry`, `echo`, `gateway`, `control-plane-migrate`,
`control-plane`, `metering-migrate` and `metering`; each reaches the state
*Healthy* within a minute, longer on the first run while the images are
pulled. `control-plane-migrate` instead reaches *Finished* once it has
migrated and seeded the control plane's database, and `control-plane` starts
only then; see [the control plane](control-plane.md). `metering-migrate` and
`metering` do the same for metering; see [metering](metering.md). Echo
registers at the registry once it has started, and the gateway routes to it
a few seconds later; see [the registry](registry.md). The gateway runs as two
replicas, which share every tenant's rate limit; see
[throttling](throttling.md).

The gateway is the only Homeport service with an external endpoint; its
address is in the dashboard. Keycloak is reachable from the host too, at
<http://localhost:8080>, so a client can get a token there. Call Echo through
the gateway with a token, as
[obtaining a token](../api-consumers/obtaining-a-token.md) describes.

`Ctrl+C` stops everything and removes the containers.

### Credentials

The AppHost generates the passwords of all four, the passwords of the
database roles of the control plane and metering, the passwords of the
broker users of the gateway and metering, the gateway's signing key and
Echo's registration key on its first start and keeps them in the .NET user secrets of the AppHost
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
Parameters:local-client-secret = …
Parameters:unmapped-client-secret = …
Parameters:gateway-signing-key = -----BEGIN PRIVATE KEY-----…
Parameters:echo-registration-key = …
Parameters:control-plane-owner-password = …
Parameters:control-plane-app-password = …
Parameters:metering-owner-password = …
Parameters:metering-app-password = …
Parameters:gateway-rabbitmq-password = …
Parameters:metering-rabbitmq-password = …
```

The dashboard shows the same values under the parameter resources, masked
until revealed. `gateway-signing-key` is the key the gateway signs its tokens
with, under the key id `local`; see [the gateway's signing key](signing-key.md).
`echo-registration-key` is the key Echo registers with; see
[the registry](registry.md). The two `control-plane-` and the two
`metering-` `-owner-` and `-app-` passwords belong to the database roles the
services migrate and run as; the two `-rabbitmq-` passwords to the broker
users of the gateway and metering, see [metering](metering.md). The two
`-client-secret` values are the secrets of the Keycloak clients; see
[connecting an identity provider](identity-provider.md).

The seeded tenant, plan and client are parameters too, with fixed defaults:
tenant `3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61`, plan
`8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50` with 10 requests per second and a
burst of 20, and the client `local-client`.

Keycloak's admin console is at <http://localhost:8080/admin>. Sign in as
`admin` with `keycloak-password`. Keycloak takes the longest of the four to
become healthy, around half a minute. It imports the realm `homeport` from
[`deploy/keycloak/realms/`](../../deploy/keycloak/realms/homeport-realm.json)
on its first start.

## With Docker Compose

The compose file in [`deploy/compose/`](../../deploy/compose/) is generated
from the AppHost. Do not edit it; a change belongs in the AppHost, and the
`compose` gate fails until the file is regenerated.

No image of Homeport's own is published yet, so build the images of the
gateway, the registry, the control plane, metering and the Echo module
locally first, from the repository root:

```bash
dotnet publish src/Platform/Gateway/Homeport.Platform.Gateway.Api -t:PublishContainer
dotnet publish src/Platform/Registry/Homeport.Platform.Registry.Api -t:PublishContainer
dotnet publish src/Platform/ControlPlane/Homeport.Platform.ControlPlane -t:PublishContainer
dotnet publish src/Platform/Metering/Homeport.Platform.Metering.Api -t:PublishContainer
dotnet publish src/Modules/Echo/Homeport.Modules.Echo.Api -t:PublishContainer
```

The last line of each names its image:

```text
… "homeport-gateway:latest" …
… "homeport-registry:latest" …
… "homeport-control-plane:latest" …
… "homeport-metering:latest" …
… "homeport-module-echo:latest" …
```

The compose file reads its settings from `deploy/compose/.env`, which is never
committed. Create it once with the images and ports, a random password for
every `_PASSWORD` variable listed in `.env.example`, a signing key for the
gateway, as [the gateway's signing key](signing-key.md) describes, a
registration key for Echo, as [the registry](registry.md) describes, the
directories of the database and broker init scripts and the Keycloak realm,
a random secret for every `_SECRET` variable, and the seed of
[the control plane](control-plane.md). `control-plane-migrate` runs from the
control plane's image, `metering-migrate` from metering's:

```bash
cd deploy/compose
{
  printf 'ECHO_IMAGE=homeport-module-echo:latest\nECHO_PORT=8080\n'
  printf 'GATEWAY_IMAGE=homeport-gateway:latest\nGATEWAY_PORT=8080\n'
  printf 'REGISTRY_IMAGE=homeport-registry:latest\nREGISTRY_PORT=8080\n'
  printf 'CONTROL_PLANE_IMAGE=homeport-control-plane:latest\nCONTROL_PLANE_PORT=8080\n'
  printf 'CONTROL_PLANE_MIGRATE_IMAGE=homeport-control-plane:latest\n'
  printf 'METERING_IMAGE=homeport-metering:latest\nMETERING_PORT=8080\n'
  printf 'METERING_MIGRATE_IMAGE=homeport-metering:latest\n'
  printf 'POSTGRES_BINDMOUNT_0=../postgres/init\nKEYCLOAK_BINDMOUNT_0=../keycloak/realms\n'
  printf 'RABBITMQ_BINDMOUNT_0=../rabbitmq/init\n'
  sed -n 's/^\([A-Z_]*_\(PASSWORD\|SECRET\)\)=$/\1/p' .env.example | while read -r name; do
    printf '%s=%s\n' "$name" "$(openssl rand -hex 24)"
  done
  printf 'GATEWAY_SIGNING_KEY=%s\n' \
    "$(openssl genpkey -algorithm EC -pkeyopt ec_paramgen_curve:P-256 | tr -d '\r\n')"
  printf 'GATEWAY_SIGNING_KEY_ID=local\n'
  printf 'GATEWAY_ADDITIONAL_PUBLIC_KEY=\nGATEWAY_ADDITIONAL_KEY_ID=\n'
  printf 'ECHO_REGISTRATION_KEY=%s\n' "$(openssl rand -hex 32)"
  printf 'SEED_TENANT_ID=3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61\nSEED_TENANT_NAME=Local tenant\n'
  printf 'SEED_PLAN_ID=8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50\nSEED_PLAN_NAME=Local plan\n'
  printf 'SEED_PLAN_REQUESTS_PER_SECOND=10\nSEED_PLAN_BURST=20\nSEED_CLIENT_ID=local-client\n'
} > .env
```

Check it: every line but the two `GATEWAY_ADDITIONAL_` ones has a value.

```bash
grep -c '=.' .env      # prints 39
```

Start, then check:

```bash
docker compose up -d
docker compose ps --format '{{.Service}} {{.State}}'
```

```text
control-plane running
echo running
gateway running
gateway running
keycloak running
metering running
postgres running
rabbitmq running
registry running
valkey running
```

The gateway runs as two replicas, each publishing a port to the host, chosen
by Docker on every start; `docker compose port` names the first one's.
Keycloak publishes `8080`, so a client on the host gets its token there, and
its management port, chosen by Docker. Get a token for `local-client`, ask
for the gateway's port, then call Echo through it. Until Echo has registered
and the gateway has read its routes, a few seconds after the start, the call
is answered with `404`:

```bash
secret="$(sed -n 's/^LOCAL_CLIENT_SECRET=//p' .env)"
token="$(curl -s \
  -d grant_type=client_credentials \
  -d client_id=local-client \
  -d "client_secret=${secret}" \
  -d scope=echo.invoke \
  http://localhost:8080/realms/homeport/protocol/openid-connect/token \
  | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"
docker compose port gateway 8080      # prints 0.0.0.0:<port>
curl -s -X POST -H "Authorization: Bearer ${token}" \
  -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' http://localhost:<port>/modules/echo/v1/echo
```

```json
{"hello":"Homeport"}
```

Every other service is reachable only on the compose network. The token is
valid for five minutes; get a new one after that. Stop and remove
everything with:

```bash
docker compose down
```

After pulling changes to a service or Echo, rebuild its image
before starting. When `.env.example` gains a variable after an update, add it to
`.env` the same way before starting.

## Smart App Control

On Windows 11, Smart App Control blocks every DLL that is neither signed nor
known to Microsoft, and a build of Homeport loads several, its own included.
A service then fails at start, or a test project finds no tests, with:

```text
System.IO.FileLoadException: Could not load file or assembly '…\Npgsql.OpenTelemetry.dll'.
Eine Anwendungssteuerungsrichtlinie hat diese Datei blockiert. (0x800711C7)
```

In English the message reads "An Application Control policy has blocked this
file". Check whether Smart App Control is on, in PowerShell:

```powershell
Get-ItemPropertyValue 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' VerifiedAndReputablePolicyState
```

`1` means it is on, `2` that it only evaluates, `0` that it is off. While it
evaluates, Windows may switch it on by itself. The block is logged in the
event log `Microsoft-Windows-CodeIntegrity/Operational` with the ids `3033`
and `3077`, naming the DLL.

Smart App Control knows no exception for a file or a folder: on a machine
that builds Homeport it has to be off. Switch it off under Windows Security →
App & browser control → Smart App Control settings. Since the cumulative
update KB5083769 of April 2026 it can be switched on again in the same place,
although the dialog may still warn that only a reinstall can; on older builds
the warning holds. Another application control policy, such as one your
organisation sets through WDAC, needs an exception from whoever manages it.
