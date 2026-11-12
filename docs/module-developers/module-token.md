# The module token

A module trusts one identity source: the short-lived token the gateway signs
for every request it forwards
([ADR-0011](../architecture/adr/0011-gateway-issued-tokens-for-modules.md)).
The caller's own token never reaches the module, and the module never talks
to the identity provider. Where the token is issued on a request's way
through the gateway is drawn in
[the request flow](../architecture/request-flow.md#a-call-to-a-module).
The claims are normative in the
[module contract](../reference/module-contract/README.md#requests-from-the-gateway).
This page shows how a module built with `Homeport.Sdk` uses the token. The
Echo module in `src/Modules/Echo/` is the reference.

## What the SDK does on its own

`AddHomeportModule` (see [declaring a manifest](declaring-a-manifest.md))
validates the token on every request: signed by the gateway with ES256 under
a key of its JWKS, issued by `homeport-gateway`, for the manifest's `id` as
audience, not expired.

| Request | Answer |
|---|---|
| No token, or one that fails validation | `401` as Problem Details; the reason is logged at Warning, never the token |
| A valid token, on an endpoint whose policy it does not meet | `403` as Problem Details |
| A valid token | The endpoint runs |

Every endpoint needs the token, including one the module forgets to mark,
and a path the module does not serve: without a token it is `401`, not
`404`. Only the endpoints `MapHomeportModule` maps — manifest, OpenAPI
document, health — are anonymous.

The SDK reads the gateway's JWKS from `GatewayTokens:JwksUri`, by default
`http://gateway/.well-known/jwks.json`, which service discovery resolves
from the module's reference to the gateway. It reads the keys again when a
token names a key it does not know, so a key rotation needs no restart.

## Requiring a scope per endpoint

The gateway already refuses a caller without the scope a route declares in
the manifest. A module that checks again, or checks a scope of its own per
endpoint, uses the policy the SDK adds for every scope of the manifest,
named like the scope:

```csharp
[ApiController]
[Authorize(Policy = EchoManifest.InvokeScope)]
public sealed class EchoController : ControllerBase
```

A caller without the scope gets `403`.

## Reading the caller

`HomeportCaller.From(User)` returns who calls: the tenant, the client, the
subject of the client's token and the scopes it grants.

```csharp
[HttpGet(EchoManifest.CallerPath, Name = GetCallerOperation)]
public ActionResult<CallerResponse> Caller()
{
    var caller = HomeportCaller.From(User);

    return new CallerResponse(caller.TenantId, caller.ClientId, caller.Subject, caller.Scopes);
}
```

`TenantId` is the one to scope tenant data by. `From` throws on a request
without a module token, which only an anonymous endpoint lets through.

## Check it

Echo answers `GET /v1/caller` with the caller its token names. Through the
gateway, with a token that grants `echo.invoke`
([obtaining a token](../api-consumers/obtaining-a-token.md)):

```bash
curl -s -H "Authorization: Bearer ${token}" http://<gateway>/modules/echo/v1/caller
```

```json
{"tenantId":"3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61","clientId":"local-client","subject":"…","scopes":["usage.read","echo.invoke"]}
```

The same request sent to Echo directly, past the gateway, is answered with
`401`.
