# Running behind a reverse proxy

The gateway is Homeport's one external endpoint. An operator often puts a
reverse proxy or a load balancer in front of it, which terminates TLS and
passes the caller on in `X-Forwarded-For` and `X-Forwarded-Proto`.

Anybody can send those headers. The gateway therefore applies them only when
the request comes from a proxy you configured as trusted, and ignores them
from every other caller. Without configuration it trusts no proxy, loopback
included: the address it sees is the address that connected, and the scheme
is the one the connection used.

## What the forwarded headers change

The gateway passes the caller's address and scheme on to the module in
`X-Forwarded-For` and `X-Forwarded-Proto`, and its traces record them.
Behind a trusted proxy these are the caller's; behind an untrusted one, or
with no trusted proxy configured, they are the proxy's.

`X-Forwarded-Host` is never applied. The gateway does not route by host.

## Configuring trusted proxies

| Setting | Environment variable | Example |
|---|---|---|
| Address of a trusted proxy | `TrustedProxies__Proxies__0`, `__1`, … | `10.0.0.5` |
| Network of trusted proxies, CIDR | `TrustedProxies__Networks__0`, `__1`, … | `10.0.0.0/24` |

Name the proxy's address as the gateway sees it, which is usually its address
on the internal network rather than its public one. A network suits a proxy
whose address changes, such as a load balancer with several instances; keep it
as narrow as the proxies' addresses allow, because every caller inside it is
trusted.

The gateway does not start when an entry is not an IP address or not a
network in CIDR notation. It logs the proxies and networks it trusts in its
start message.

## One proxy in front

The gateway takes one entry from the end of each header: the one the proxy
directly in front of it added. With a chain of proxies, the address it applies
is the one the last proxy saw connecting, not the original caller's.

## The compose file

`deploy/compose/docker-compose.yaml` sets no trusted proxy. Add the variables
to the gateway's `environment` in your own override file, for example:

```yaml
services:
  gateway:
    environment:
      TrustedProxies__Proxies__0: "10.0.0.5"
```

`ASPNETCORE_FORWARDEDHEADERS_ENABLED`, which makes an ASP.NET Core service
accept forwarded headers from any caller, has no effect on the gateway: only
the proxies configured here are trusted.

## Timeouts

The gateway gives up on a forwarded request that stays idle - neither a
byte sent nor received - for `Gateway:Forwarding:Timeout`, and answers `504`
with the problem type
[`module-timeout`](../reference/problems/module-timeout.md), or
[`platform-service-timeout`](../reference/problems/platform-service-timeout.md)
for a platform endpoint or the admin API. It applies to every route:
modules, platform endpoints and the admin API.

| Setting | Environment variable | Default |
|---|---|---|
| Forwarding timeout, `hh:mm:ss` | `Gateway__Forwarding__Timeout` | `00:00:30` |

The timeout counts idle time, not the whole request: a response that keeps
streaming, such as a large usage export, is not cut off. The gateway does
not start with a timeout of zero or less, and logs the timeout in its start
message.

Give the proxy in front a longer timeout than the gateway's, so the caller
receives the gateway's `504` with its trace id rather than the proxy's own
error.

## The gateway's port

The compose file runs one gateway and publishes its port on the host port
`GATEWAY_HOST_PORT` in `.env`, which `create-env.sh` sets to `5100`. The
port is the same after every restart: point the proxy at it. Inside the
container the gateway listens on `GATEWAY_PORT`; its admin port, `8000`, is
never published ([admin API](admin-api.md#keep-it-unpublished)).

## Scaling out the gateway

The gateway keeps no state of its own: every replica shares the tenants'
rate limits in Valkey ([throttling](throttling.md#the-buckets)), so more
replicas behind one load balancer act as one. Two replicas cannot publish
the same host port, so an override file that scales out publishes none, and
your load balancer joins the compose network and reaches the replicas at
`gateway:${GATEWAY_PORT}`, which Docker resolves to each of them in turn.
Write the override, for example as `docker-compose.scale.yaml`:

```yaml
services:
  gateway:
    ports: !reset []
    deploy:
      replicas: 2
```

Start with it, then check:

```bash
docker compose -f docker-compose.yaml -f docker-compose.scale.yaml up -d
docker compose -f docker-compose.yaml -f docker-compose.scale.yaml ps --format '{{.Service}} {{.Ports}}'
```

It lists `gateway` once per replica, each with `8000/tcp, 8080/tcp` and no
host port. Name the load balancer as a trusted proxy, as above, and roll a
new signing key out to every replica, as
[the gateway's signing key](signing-key.md) describes.

## The portal

The admin portal applies forwarded headers the same way, with the same
settings in its own environment. It needs them more than the gateway: it
signs in only over HTTPS, and behind a proxy that terminates TLS it knows
the request was HTTPS only from a trusted proxy's `X-Forwarded-Proto`. See
[the admin portal](admin-portal.md#tls-and-the-reverse-proxy).
