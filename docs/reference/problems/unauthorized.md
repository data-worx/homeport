# unauthorized

`https://homeport.data-worx.de/problems/unauthorized` — status `401`.

The request carries no valid credential for the endpoint. The gateway
answers with it for a call to a module without a token, or with one that is
expired, signed by another issuer or meant for another audience; see
[obtaining a token](../../api-consumers/obtaining-a-token.md). It does so for
every path under `/modules/`, whatever the method, before it routes: without
a valid token a caller never learns whether a module, a path or a method
exists. The registry
answers with it for a registration or heartbeat with an unknown module id or
a wrong registration key, and for a read of the route table without a valid
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
used to guess a key. At the gateway, get a fresh token and check its issuer
and audience with the operator. At the registry, check the module id and the
key against what the operator configured; the same request fails the same
way until they match.
