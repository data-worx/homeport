# Errors

Every error Homeport answers with — whether the gateway or a module produced
it — has the same shape: Problem Details (RFC 9457), media type
`application/problem+json`. You handle one error format, whichever module
you call. See [ADR-0016](../architecture/adr/0016-error-responses.md).

## The body

```json
{
  "type": "https://homeport.data-worx.de/problems/unknown-module",
  "title": "No module is mounted at this path.",
  "status": 404,
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
}
```

| Member | Meaning |
|---|---|
| `type` | What kind of error it is, as an absolute URI. Compare it as a string; Homeport never fetches it |
| `title` | A short English summary, the same for every occurrence of the type |
| `status` | The HTTP status, the same as the response's |
| `detail` | An English explanation of this occurrence, where there is one |
| `traceId` | The W3C trace id of the request. Quote it when you report an error to the operator |

A type may add members of its own, such as `errors` on a
[validation](../reference/problems/validation.md) error. Branch on `type`,
not on `title` or `detail`: they are for a developer to read and may be
reworded.

## Who answered

The `type` tells you who produced the error.

- **The gateway** answers with Homeport's own types under
  `https://homeport.data-worx.de/problems/`, listed in
  [problem types](../reference/problems/README.md). They say the request
  never reached a module, or the module could not answer:

  | Status | Type | Means |
  |---|---|---|
  | `404` | [`unknown-module`](../reference/problems/unknown-module.md) | No module is mounted at the path |
  | `502` | [`module-unreachable`](../reference/problems/module-unreachable.md) | The gateway could not reach the module |
  | `503` | [`module-unavailable`](../reference/problems/module-unavailable.md) | No instance of the module is available |
  | `504` | [`module-timeout`](../reference/problems/module-timeout.md) | The module did not answer in time |

- **A module** answers with its own types, under a prefix of its own and
  documented by the module, or with Homeport's
  [`validation`](../reference/problems/validation.md) and
  [`internal`](../reference/problems/internal.md).

## What the gateway does to a module's error

- **A 4xx in Problem Details** reaches you as the module sent it, with
  `traceId` added. Its `type` and `detail` are the module's.
- **A 4xx in any other format** — no body, HTML, other JSON — is replaced.
  The status stays; the body carries the default type for the status, a link
  to the section of RFC 9110 that defines it, and no `detail`.
- **A 5xx** is always replaced, so no internals of a module reach you. A
  `500` reaches you as [`internal`](../reference/problems/internal.md); any
  other 5xx carries the default type for its status. The module's `detail` is
  dropped; the cause is in the logs and the trace behind `traceId`.

Errors ASP.NET answers on its own, such as `405` for a wrong method, carry
the default type for their status, too.

## Reading an error in .NET

`Homeport.Shared.Client` reads the body as `ProblemResponse`, in the
namespace `Homeport.Shared.Client.Problems`. It carries every member above,
`errors` as `Errors`, and any member a module adds in `Extensions`:

```csharp
var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
```

`ProblemConventions` in the same namespace holds the prefix of Homeport's
problem types and the names of its extensions. How to reference the project
is in [calling a module](calling-a-module.md#request-and-response-types-in-net).
