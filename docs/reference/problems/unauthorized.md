# unauthorized

`https://homeport.data-worx.de/problems/unauthorized` — status `401`.

The request carries no valid credential for the endpoint. Today the registry
answers with it: a registration or heartbeat with an unknown module id or a
wrong registration key, and a read of the route table without a valid
service token from the gateway.

```json
{
  "type": "https://homeport.data-worx.de/problems/unauthorized",
  "title": "The request is not authenticated.",
  "status": 401,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

The body never says which part of the credential was wrong, so it cannot be
used to guess a key. Check the module id and the key against what the
operator configured at the registry; the same request fails the same way
until they match.
