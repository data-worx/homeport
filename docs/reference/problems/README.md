# Problem types

Homeport and its modules answer every error as Problem Details (RFC 9457),
media type `application/problem+json`. The `type` names the kind of error as
an absolute URI. Homeport's own types live under
`https://homeport.data-worx.de/problems/<name>`; each has a page here. A
module's own types live under a prefix of its own and are documented by the
module.

A type URI is an identifier. Homeport never fetches it, and a client compares
it as a string. Once published, a type is part of the public API.

Every error response carries these members:

| Member | Meaning |
|---|---|
| `type` | The problem type, an absolute URI |
| `title` | A short English summary, the same for every occurrence of the type |
| `status` | The HTTP status |
| `detail` | An English explanation of this occurrence, where there is one |
| `traceId` | The W3C trace id of the request, to find its trace |

Errors that ASP.NET answers on its own, such as `404` for an unknown path or
`405` for a wrong method, carry its default type, a link to the section of
RFC 9110 that defines the status. So does a module's error body that the
gateway replaces, unless a type below applies; see
[errors](../../api-consumers/errors.md).

| Type | Status | Page |
|---|---|---|
| `https://homeport.data-worx.de/problems/internal` | 500 | [internal](internal.md) |
| `https://homeport.data-worx.de/problems/module-timeout` | 504 | [module-timeout](module-timeout.md) |
| `https://homeport.data-worx.de/problems/module-unavailable` | 503 | [module-unavailable](module-unavailable.md) |
| `https://homeport.data-worx.de/problems/module-unreachable` | 502 | [module-unreachable](module-unreachable.md) |
| `https://homeport.data-worx.de/problems/unknown-module` | 404 | [unknown-module](unknown-module.md) |
| `https://homeport.data-worx.de/problems/validation` | 400 | [validation](validation.md) |
