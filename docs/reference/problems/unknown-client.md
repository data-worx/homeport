# unknown-client

`https://homeport.data-worx.de/problems/unknown-client` — status `403`.

The gateway answered: the token is valid, but the client it was issued to is
not mapped to a tenant in this Homeport installation, or the token does not
carry a client id. The request did not reach the module.

```json
{
  "type": "https://homeport.data-worx.de/problems/unknown-client",
  "title": "The client is not known to Homeport.",
  "status": 403,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying with the same token fails the same way. Ask the operator to map your
client id to a tenant. The gateway remembers the answer for a client for a
short while, so a newly mapped client may be refused for up to that long; see
[connecting an identity provider](../../operators/identity-provider.md).
