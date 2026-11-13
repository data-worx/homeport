# admin-claim-missing

`https://homeport.data-worx.de/problems/admin-claim-missing` — status `403`.

The gateway answered on its admin port: the operator's token is valid, but
it does not carry the claim that grants admin access, as the operator
configured it — `roles` with the value `homeport-admin` unless changed; see
[the admin API](../../operators/admin-api.md). The request did not reach the
platform service.

```json
{
  "type": "https://homeport.data-worx.de/problems/admin-claim-missing",
  "title": "The token lacks the claim that grants admin access.",
  "status": 403,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

Retrying with the same token fails the same way. Grant the user the claim
at the identity provider, sign in again for a fresh token, and check that
the provider puts the claim into the access token, not only into the ID
token.
