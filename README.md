# Homeport

> **Homeport by data-worx** — *Your data never leaves port.*

**Business services in your own infrastructure – multi-tenant, metered, open source.**
Gateway, tenants, authentication, rate limits and usage export included – add the business modules you need.

Homeport is a self-hosted platform for business modules. An operator runs
it in their own environment, and it provides what a SaaS product would:
gateway, multi-tenancy, authentication, throttling, usage metering,
observability and an admin portal. Each module covers one thick business
domain in depth — EDIFACT, e-invoicing, bank statement formats, healthcare
billing files.

- **Not a hosted SaaS.** Business data never leaves the operator's
  infrastructure. Nothing phones home.
- **Not an invoicing system.** Homeport meters and exports usage for showback
  and chargeback; it never creates invoices.

More in [docs/architecture/vision.md](docs/architecture/vision.md).

## Status

Pre-alpha. v0.1.0, the walking skeleton, is released: a call goes through the
gateway to a module and is metered. Images and the compose file are on the
[releases page](https://github.com/data-worx/homeport/releases). The
milestones ahead are described in [ROADMAP.md](ROADMAP.md): next is M2, the
admin portal. The first real module is chosen in
[discussion #2](https://github.com/data-worx/homeport/discussions/2) and
decided when M2 is released.

## Quickstart

From a clone to a metered call. You need Docker, running, the
[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build the
images, and a shell with `bash`, `curl` and `openssl` — on Windows, Git Bash.
On Windows, also switch Smart App Control off, as
[running locally](docs/operators/running-locally.md#smart-app-control)
explains.

Build the images of the gateway, the registry, the control plane, metering
and the Echo module from this checkout
([running locally](docs/operators/running-locally.md#with-docker-compose)):

```bash
git clone https://github.com/data-worx/homeport.git
cd homeport
for project in \
  Platform/Gateway/Homeport.Platform.Gateway.Api \
  Platform/Registry/Homeport.Platform.Registry.Api \
  Platform/ControlPlane/Homeport.Platform.ControlPlane \
  Platform/Metering/Homeport.Platform.Metering.Api \
  Modules/Echo/Homeport.Modules.Echo.Api; do
  dotnet publish "src/${project}" -t:PublishContainer
done
```

Create the settings with random passwords and keys, and start everything:

```bash
bash deploy/compose/create-env.sh
cd deploy/compose
docker compose up -d
```

Get a token for the seeded client `local-client` from the bundled Keycloak
([obtaining a token](docs/api-consumers/obtaining-a-token.md)). Keycloak
takes about half a minute to start, so this waits for it:

```bash
secret="$(sed -n 's/^LOCAL_CLIENT_SECRET=//p' .env)"
until token="$(curl -sf \
  -d grant_type=client_credentials \
  -d client_id=local-client \
  -d "client_secret=${secret}" \
  -d scope=echo.invoke \
  http://localhost:8080/realms/homeport/protocol/openid-connect/token \
  | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')" && [ -n "${token}" ]; do
  sleep 2
done
```

Call Echo through the gateway
([calling a module](docs/api-consumers/calling-a-module.md)). Docker picks
the gateway's port; until Echo has registered and the gateway routes to it
([the registry](docs/operators/registry.md)), the call is refused, so this
retries:

```bash
gateway="http://localhost:$(docker compose port gateway 8080 | sed 's/.*://')"
until curl -sf -w '\n' -X POST -H "Authorization: Bearer ${token}" \
  -H 'Content-Type: application/json' \
  -d '{"hello":"Homeport"}' "${gateway}/modules/echo/v1/echo"; do
  sleep 2
done
```

```json
{"hello":"Homeport"}
```

Read your tenant's usage of this year per day as CSV
([reading usage](docs/api-consumers/reading-usage.md)). The usage arrives
within seconds, so this waits for Echo to appear:

```bash
year="$(date -u +%Y)"
query="from=${year}-01-01T00:00:00Z&to=$((year + 1))-01-01T00:00:00Z&granularity=day&module=echo"
until csv="$(curl -sf -H "Authorization: Bearer ${token}" -H 'Accept: text/csv' \
  "${gateway}/platform/v1/usage?${query}")" && grep -q echo <<< "${csv}"; do
  sleep 2
done
echo "${csv}"
```

```text
period,module,meter,quantity
2026-11-09T00:00:00Z,echo,requests,1
```

The date is the day of your run. Stop and remove everything with
`docker compose down -v`.

## Documentation

- [docs/](docs/README.md) — vision, architecture, decision records and the
  module contract
- [AGENTS.md](AGENTS.md) — the binding rules for working on the code
- [CONTRIBUTING.md](CONTRIBUTING.md) — how to contribute
- [SECURITY.md](SECURITY.md) — how to report a vulnerability

Questions and ideas go to
[GitHub Discussions](https://github.com/data-worx/homeport/discussions).

## Licence

Homeport is licensed under the
[GNU Affero General Public License v3.0 or later](LICENSE). Contributions
require a signed [Contributor License Agreement](CLA.md).

The names "Homeport" and "data-worx" and their logos are not licensed with the
code — see [TRADEMARKS.md](TRADEMARKS.md).
