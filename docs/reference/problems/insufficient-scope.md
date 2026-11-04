# insufficient-scope

`https://homeport.data-worx.de/problems/insufficient-scope` — status `403`.

The gateway answered: the client is known, but its token does not grant the
scope the route declares in the module's manifest. The request did not reach
the module.

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
provider. Which scope a route needs is listed in the module's manifest.
