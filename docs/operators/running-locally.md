# Running Homeport locally

Two ways to start Homeport on one machine: through the .NET Aspire AppHost,
which is how Homeport is developed, and through the generated Docker Compose
file, which is how it is deployed without .NET.

Today both start the infrastructure — PostgreSQL, RabbitMQ, Valkey and
Keycloak — and the Echo module. Homeport's own services follow with the next
milestones.

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
`keycloak` and `echo`; each reaches the state *Healthy* within a minute, longer
on the first run while the images are pulled.

`Ctrl+C` stops everything and removes the containers.

### Credentials

The AppHost generates the passwords of all four on its first start and keeps
them in the .NET user secrets of the AppHost project, on your machine only.
List them from the repository root:

```bash
dotnet user-secrets list --project src/Aspire/Homeport.Aspire.AppHost
```

```text
Parameters:valkey-password = …
Parameters:rabbitmq-password = …
Parameters:postgres-password = …
Parameters:keycloak-password = …
```

The dashboard shows the same values under the parameter resources, masked
until revealed.

Keycloak's admin console is at <http://localhost:8080/admin>. Sign in as
`admin` with `keycloak-password`. Keycloak takes the longest of the four to
become healthy, around half a minute.

## With Docker Compose

The compose file in [`deploy/compose/`](../../deploy/compose/) is generated
from the AppHost. Do not edit it; a change belongs in the AppHost, and the
`compose` gate fails until the file is regenerated.

No image of Homeport's own is published yet, so build the Echo module's image
locally first, from the repository root:

```bash
dotnet publish src/Modules/Echo/Homeport.Modules.Echo.Api -t:PublishContainer
```

Its last line names the image:

```text
… "homeport-modules-echo-api:latest" …
```

The compose file reads its settings from `deploy/compose/.env`, which is never
committed. Create it once with the Echo image and port, and a random password
for every `_PASSWORD` variable listed in `.env.example`:

```bash
cd deploy/compose
{
  printf 'ECHO_IMAGE=homeport-modules-echo-api:latest\nECHO_PORT=8080\n'
  sed -n 's/^\([A-Z_]*_PASSWORD\)=$/\1/p' .env.example | while read -r name; do
    printf '%s=%s\n' "$name" "$(openssl rand -hex 24)"
  done
} > .env
```

Check it: every line has a value.

```bash
grep -c '=.' .env      # prints 6
```

Start, then check:

```bash
docker compose up -d
docker compose ps --format '{{.Service}} {{.State}}'
```

```text
echo running
keycloak running
postgres running
rabbitmq running
valkey running
```

The services are reachable only on the compose network; no port is published
to the host. Stop and remove everything with:

```bash
docker compose down
```

After pulling changes to Echo, rebuild its image before starting. When
`.env.example` gains a variable after an update, add it to `.env` the same way
before starting.
