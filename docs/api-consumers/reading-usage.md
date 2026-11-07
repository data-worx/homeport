# Reading usage

Homeport counts what your tenant uses of every module, per hour and per
day, so the operator can show it back or charge it back. You read your own
tenant's usage through the gateway, as JSON or as CSV. Homeport exports
usage; it never creates invoices.

```text
GET /platform/v1/usage?from=…&to=…&granularity=hour|day[&module=…][&meter=…]
```

Your token needs the scope `usage.read`. You only ever see your own
tenant's usage: the tenant comes from your client, never from the request.
The call counts against your tenant's rate limit like any other
([rate limits](rate-limits.md)), but it is not itself counted as usage.

## Parameters

| Parameter | Required | Means |
|---|---|---|
| `from` | yes | The first moment of the period, ISO 8601, such as `2026-11-07T00:00:00Z` |
| `to` | yes | The first moment after the period; later than `from` |
| `granularity` | yes | `hour` or `day` |
| `module` | no | Only this module's usage, by its id, such as `echo` |
| `meter` | no | Only this meter's usage, such as `requests` |

Hours and days are UTC. A bucket is in the export when it **starts** in
[`from`, `to`): `from=2026-11-07T00:00:00Z&to=2026-11-08T00:00:00Z` with
`granularity=day` is exactly 7 November, and with `granularity=hour` its 24
hours. A `from` with an offset is converted to UTC first. Only buckets with
usage appear; a bucket without any is left out rather than listed with `0`.

A missing or invalid parameter is answered with `400` and the problem type
[`validation`](../reference/problems/validation.md); `errors` names the
parameter.

## JSON

JSON is the default, and what `Accept: application/json` returns:

```json
{
  "from": "2026-11-07T00:00:00Z",
  "to": "2026-11-08T00:00:00Z",
  "granularity": "hour",
  "usage": [
    { "period": "2026-11-07T13:00:00Z", "module": "echo", "meter": "items", "quantity": 5 },
    { "period": "2026-11-07T13:00:00Z", "module": "echo", "meter": "requests", "quantity": 3 }
  ]
}
```

`period` is the start of the hour or day, in UTC with `Z`. The entries are
ordered by period, module and meter. With .NET, `UsageResponse` in
`Homeport.Shared.Client.Platform.Metering.V1` reads the body.

## CSV

`Accept: text/csv` returns the same entries as CSV (RFC 4180): a header row,
one row per entry, comma-separated, CRLF line breaks, a field quoted only
where it contains a comma, a quote or a line break.

```text
period,module,meter,quantity
2026-11-07T13:00:00Z,echo,items,5
2026-11-07T13:00:00Z,echo,requests,3
```

## Errors

| Status | Problem type | Cause |
|---|---|---|
| `400` | [`validation`](../reference/problems/validation.md) | A parameter is missing or invalid |
| `401` | [`unauthorized`](../reference/problems/unauthorized.md) | No valid token |
| `403` | [`insufficient-scope`](../reference/problems/insufficient-scope.md) | The token lacks `usage.read` |
| `403` | [`unknown-client`](../reference/problems/unknown-client.md) | The operator has not mapped your client to a tenant |
| `429` | [`rate-limited`](../reference/problems/rate-limited.md) | Your tenant's rate limit is exceeded |

## Try it locally

With Homeport running locally ([running Homeport locally](../operators/running-locally.md)),
the client `local-client` gets `usage.read` by default. From the repository
root, with the AppHost running, and `<gateway>` the gateway's address from
the dashboard, get a token as in
[obtaining a token](obtaining-a-token.md#try-it-locally), call Echo a few
times, then read today's usage per hour:

```bash
secret="$(dotnet user-secrets list --project src/Aspire/Homeport.Aspire.AppHost \
  | sed -n 's/^Parameters:local-client-secret = //p')"
token="$(curl -s \
  -d grant_type=client_credentials \
  -d client_id=local-client \
  -d "client_secret=${secret}" \
  -d scope=echo.invoke \
  http://localhost:8080/realms/homeport/protocol/openid-connect/token \
  | sed -n 's/.*"access_token":"\([^"]*\)".*/\1/p')"
for i in 1 2 3; do
  curl -s -o /dev/null -H "Authorization: Bearer ${token}" http://<gateway>/modules/echo/v1/request
done
today="$(date -u +%Y-%m-%d)"
tomorrow="$(date -u -d tomorrow +%Y-%m-%d)"
query="from=${today}T00:00:00Z&to=${tomorrow}T00:00:00Z&granularity=hour&module=echo"
```

As JSON:

```bash
curl -s -H "Authorization: Bearer ${token}" "http://<gateway>/platform/v1/usage?${query}"
```

```json
{"from":"2026-11-07T00:00:00Z","to":"2026-11-08T00:00:00Z","granularity":"hour","usage":[{"period":"2026-11-07T13:00:00Z","module":"echo","meter":"requests","quantity":3}]}
```

As CSV:

```bash
curl -s -H "Authorization: Bearer ${token}" -H 'Accept: text/csv' "http://<gateway>/platform/v1/usage?${query}"
```

```text
period,module,meter,quantity
2026-11-07T13:00:00Z,echo,requests,3
```

Usage arrives within seconds of a request; a call made a moment ago may not
be in the export yet. The dates and the hour are those of your run.
