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

## The portal

The admin portal applies forwarded headers the same way, with the same
settings in its own environment. It needs them more than the gateway: it
signs in only over HTTPS, and behind a proxy that terminates TLS it knows
the request was HTTPS only from a trusted proxy's `X-Forwarded-Proto`. See
[the admin portal](admin-portal.md#tls-and-the-reverse-proxy).
