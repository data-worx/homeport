# insufficient-scope

`https://homeport.data-worx.de/problems/insufficient-scope` — status `403`.

The gateway answered: the client is known, but its token does not grant the
scope the route requires — the scope a module's manifest declares, or the
one a platform API requires, such as `usage.read` for
[the usage export](../../api-consumers/reading-usage.md). The request did
not reach the module or the platform service.

Metering answers with the same type when a token the gateway forwarded
lacks `usage.read` or names no tenant; through the gateway, a caller sees
the gateway's answer first.

```json
{
  "type": "https://homeport.data-worx.de/problems/insufficient-scope",
  "title": "The client's token lacks the scope this route requires.",
  "status": 403,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying with the same token fails the same way. Request a token that grants
the scope, or ask the operator to allow your client the scope at the identity
provider. Which scope a route needs is listed in the module's manifest, or
on the page of the platform API.
