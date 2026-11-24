# Connecting an identity provider

Homeport keeps no credentials of its own. The clients of your tenants
authenticate at your identity provider — Entra ID, Keycloak or any other
OpenID Connect provider — with the OAuth 2.0 client-credentials grant, and
call the gateway with the token they get
([ADR-0006](../architecture/adr/0006-authentication.md)).

For every call to a module the gateway

1. validates the token: signature against the provider's keys, issuer,
   audience and expiry. Anything else is answered with `401`;
2. reads the client id from the token and asks the control plane which
   tenant and plan the client belongs to. A client the control plane does
   not know is answered with `403`;
3. checks that the token grants the scope the route declares in the
   module's manifest. A token without it is answered with `403`;
4. forwards the request with a token of its own in place of the client's
   ([ADR-0011](../architecture/adr/0011-gateway-issued-tokens-for-modules.md)).
   The client's token never reaches the module.

The whole path of a call, with every token on the way, is drawn in
[the request flow](../architecture/request-flow.md).

The operator's own tokens, for the admin API on the gateway's admin port,
are configured apart from the clients'; see [the admin API](admin-api.md).

The Keycloak that the AppHost and the compose file run is for development
and tests only: it runs in development mode with a realm of test clients and
a dev operator. Keycloak itself is as good a production provider as any
other; run your own installation and set it up as
[Keycloak as your identity provider](#keycloak-as-your-identity-provider)
shows.

## Configuration

| Setting | Environment variable | Default | Meaning |
|---|---|---|---|
| `ClientAuthentication:Authority` | `ClientAuthentication__Authority` | — | The issuer of the tokens. The gateway reads the provider's metadata at `<authority>/.well-known/openid-configuration` and its signing keys from there |
| `ClientAuthentication:Audience` | `ClientAuthentication__Audience` | — | The audience a token must carry. Configure the provider to issue it to Homeport's clients |
| `ClientAuthentication:ClientIdClaim` | `ClientAuthentication__ClientIdClaim` | `azp` | The claim that carries the client id. Keycloak uses `azp` (and `client_id`); Entra ID uses `azp` in v2 tokens and `appid` in v1 tokens |
| `ClientAuthentication:ScopeClaim` | `ClientAuthentication__ScopeClaim` | `scope` | The claim that carries the granted scopes, space-delimited or one claim per scope. Keycloak uses `scope`; Entra ID uses `scp` for delegated and `roles` for application permissions |
| `ClientAuthentication:RequireHttpsMetadata` | `ClientAuthentication__RequireHttpsMetadata` | `true` | Whether the metadata must be fetched over HTTPS. Turn it off only for a development provider without TLS |
| `ControlPlane:ClientCacheLifetime` | `ControlPlane__ClientCacheLifetime` | `00:01:00` | How long the gateway remembers which tenant a client belongs to, and that a client is unknown |
| `ModuleTokens:Lifetime` | `ModuleTokens__Lifetime` | `00:01:00` | How long the token the gateway sends a module lives. At most `00:05:00`; the gateway does not start with a longer one |

The gateway does not start without an authority and an audience. Its start
is logged at Information with the authority, the audience, the client id
and scope claims, the cache lifetime and the module token lifetime.

## Scopes

Every route of a module requires one scope, named in the module's manifest
under `scopes`; Echo's is `echo.invoke`, the e-invoicing module's
`e-invoicing.validate` and `e-invoicing.read`. Create each scope of every module
you run at your provider, and grant a client the scopes of the routes it may
call. Homeport does not create them for you.

## Mapping clients to tenants

Create a client at your provider for every machine identity of a tenant,
then register its client id — the value of the client id claim — for the
tenant and a plan on the *Clients* page of
[the admin portal](admin-portal.md#clients), or through
[the admin API](admin-api.md#endpoints). Homeport never creates, changes or
deletes a client at your provider. A client keeps its tenant; to move it to
another one, remove it and register it again.

The gateway keeps the answer for a client for the cache lifetime. A client
you register takes effect at most that long after it was first refused; a
client you remove, or move to another plan, keeps its old answer for at most
that long.

## When the control plane is down

A client the gateway looked up within the cache lifetime keeps working. Any
other is answered with `503` and the problem type
[`control-plane-unavailable`](../reference/problems/control-plane-unavailable.md),
and the gateway logs a warning.

## What is logged

At Warning, without the token: a rejected token with the reason (the type of
the validation failure), a token without the client id claim, a client the
control plane does not know, with its client id, and a token without the
scope of its route, with the scope and the path.

## Keycloak as your identity provider

The steps for a Keycloak installation of your own, in its admin console.
The names in `code` are the ones Homeport is configured with; choose your
own.

### The realm and the scopes

1. Create a realm, such as `homeport`. The gateway's
   `ClientAuthentication:Authority` is `https://<keycloak>/realms/homeport`.
2. Under *Client scopes*, create one scope per module scope you run, such
   as `echo.invoke`, with the protocol OpenID Connect and *Include in token
   scope* on. Add `usage.read` if your clients read their usage.

### A client of a tenant

1. Under *Clients*, create a client with the protocol OpenID Connect and the
   client id you want the tenant's machine identity to have, such as
   `billing-service`.
2. Turn *Client authentication* on, and of the authentication flows leave
   only *Service account roles* on: the client uses the client-credentials
   grant. Copy
   its secret from *Credentials* to the machine that runs the client.
3. Under *Client scopes*, add the scopes of the routes the client may call,
   as *Default* to grant them on every token or as *Optional* to grant them
   when the token request asks for them with `scope=`.
4. Under the client's dedicated scope, add a mapper *By configuration* of
   the type *Audience*, with *Included Custom Audience* set to the
   gateway's `ClientAuthentication:Audience`, such as `homeport`, and *Add
   to access token* on. Without it the gateway refuses the token with `401`.
5. Register the client id for its tenant and a plan on the *Clients* page
   of the admin portal. Keycloak puts the client id into `azp`, which the
   gateway reads by default.

The client gets a token and calls a module:

```bash
token=$(curl -s -d grant_type=client_credentials -d client_id=billing-service \
  -d client_secret="$SECRET" -d scope=echo.invoke \
  https://<keycloak>/realms/homeport/protocol/openid-connect/token | jq -r .access_token)
curl -H "Authorization: Bearer $token" https://<gateway>/modules/echo/v1/request
```

### The portal's sign-in

The admin portal signs the operator in at the same realm
([the admin portal](admin-portal.md#the-client-at-your-identity-provider)
lists what it needs from any provider):

1. Under *Realm roles*, create the role that grants access, such as
   `homeport-admin`, and assign it to the people who operate Homeport. They
   are users of the realm, or come from your directory through Keycloak's
   user federation or an identity provider.
2. Create a client `portal` with the protocol OpenID Connect, *Client
   authentication* on, only *Standard flow* among the authentication flows,
   and *Require PKCE* on with the *PKCE Method* `S256`. Set *Valid redirect
   URIs* to `https://<portal address>/signin-oidc` and *Valid post logout
   redirect URIs* to `https://<portal address>/signout-callback-oidc`. Copy
   its secret into the portal's `SignIn:ClientSecret`.
3. Under the client's dedicated scope, add the mappers *Subject (sub)*,
   *User's full name* as `name`, *User Realm Role* with the token claim name
   `roles`, multivalued, into the ID and the access token, and *Audience*
   with the gateway's `AdminApi:Audience`, such as `homeport-portal`, into
   the access token.
4. Give the client the client scope `profile` as *Default*.

## Keycloak in development

The AppHost imports the realm in
[`deploy/keycloak/realms/`](../../deploy/keycloak/realms/homeport-realm.json):

- the realm `homeport`, with the client scopes `echo.invoke`,
  `e-invoicing.validate`, `e-invoicing.read` and `usage.read`;
- the client the control plane seeds (`local-client` by default, the
  parameter `seed-client-id`), confidential, with only the client-credentials
  grant, the audience `homeport`, `usage.read` by default, and `echo.invoke`,
  `e-invoicing.validate` and `e-invoicing.read` only when the token request asks for them, as
  with `scope=echo.invoke`;
- the client `unmapped-client`, which the control plane does not know, for
  the tests that expect `403`;
- the client `registered-client`, which the control plane does not seed,
  for the tests that register it through the admin API;
- the client `admin-api-test`, which signs the user `operator` in by
  password and gets a token the admin API accepts, for the tests only: a
  test has no browser for the portal's sign-in. Do not copy it into a realm
  of your own;
- the client `portal` of the admin portal, confidential, with authorization
  code and PKCE `S256`, the redirect URIs below `PORTAL_ADDRESS`, the client
  scope `profile`, and the subject, the name, the realm roles as `roles` and
  the audience `homeport-portal` in its tokens;
- the realm role `homeport-admin`, and the user `operator`, *Dev Operator*,
  who holds it. See [the admin portal](admin-portal.md).

The client secrets and the operator's password are not in the realm file.
The AppHost generates them as the parameters `local-client-secret`,
`unmapped-client-secret`, `registered-client-secret`,
`admin-api-test-secret`, `portal-client-secret` and `operator-password`;
the compose file takes them from `LOCAL_CLIENT_SECRET`,
`UNMAPPED_CLIENT_SECRET`, `REGISTERED_CLIENT_SECRET`,
`ADMIN_API_TEST_SECRET`, `PORTAL_CLIENT_SECRET` and `OPERATOR_PASSWORD`.

Keycloak's issuer is `http://localhost:8080/realms/homeport` in both setups,
so a token a client gets on the host is valid at the gateway. On the compose
network the gateway fetches the metadata and keys from `keycloak:8080`.
