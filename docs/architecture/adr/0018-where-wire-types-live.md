# ADR-0018: Where wire types live

- Status: Accepted
- Date: 2026-10-30

## Context

`dotnet-core.web` requires a dedicated request and response record for every
API action. It does not say where those records live, and Homeport has two
kinds of readers for them:

- **Module developers** build a module with `Homeport.Sdk` and implement the
  module contract: the manifest, and later registration and heartbeat.
- **API consumers** call the platform APIs and the catalog modules through
  the gateway. Some of them are .NET applications.

`Homeport.Sdk` references the ASP.NET Core shared framework, OpenAPI and the
service defaults. Whatever a package references travels with it, so a
consumer that took the SDK for its types would take module hosting along,
and every project the SDK references is shipped.

`Homeport.Shared.Contracts` is referenced by the SDK and by every Domain.
`AGENTS.md` also names it as the home of messages between services, such as
`UsageRecorded`. Once that message exists, the SDK would ship it.

The contract itself stays language-neutral HTTP (ADR-0003): a module may be
written in any language, and a consumer can generate a client from the
OpenAPI documents.

## Decision

- **Three Shared projects, three roles.**

  | Project | Holds | Shipped |
  |---|---|---|
  | `Homeport.Shared.Contracts` | The generic core several components share: the result type and its error | yes, with the SDK |
  | `Homeport.Shared.Client` | The wire types of the platform APIs and the catalog modules, and the types of the module contract | yes, on its own and with the SDK |
  | `Homeport.Shared.Internal` | Everything services share that must never leave Homeport: messages, integration events and the like | never |

- **What goes into `Homeport.Shared.Client`.** The request and response types
  of every endpoint a consumer reaches through the gateway, the types of the
  module contract, and the Problem Details read model. Nothing else: not a
  message between services, not a type of an endpoint that is internal or
  reserved to the platform admin.
- **`Homeport.Shared.Client` references nothing**, not even
  `Homeport.Shared.Contracts`, so a consumer takes it into any .NET
  application without the ASP.NET Core runtime.
- **Namespaces mirror `src/` and the API major version** (ADR-0013):
  `Homeport.Shared.Client.Platform.<Service>.V1`,
  `Homeport.Shared.Client.Modules.<Module>.V1`. A new major version is a new
  namespace next to the old one, which stays as long as that version is
  served. `ModuleContract/` carries no version, as the module contract is
  versioned by `contractVersion`. `Problems/` carries none either: it holds
  `ProblemResponse`, the BCL-only read model of the error format of ADR-0016,
  and the constants of that format. Producing errors stays with the ASP.NET
  Core `ProblemDetails` in the SDK and the gateway.
- **References.**

  | Project | `Shared.Contracts` | `Shared.Client` | `Shared.Internal` |
  |---|---|---|---|
  | Domain | yes | no | no |
  | Application | yes | yes | yes |
  | Infrastructure | yes | no | yes |
  | `.Api` host | yes | yes | yes |
  | `.Worker` host | yes | no | yes |
  | `Homeport.Sdk` | yes | yes | no |
  | `Homeport.Shared.Client` | no | – | no |

- **No business logic in `src/Shared/`** (`dotnet.layout`). A rule that
  Homeport decides belongs to one service, and other services ask it. A rule
  an external standard defines, such as an IBAN check, is a library: a
  package where one exists, otherwise a BCL-only project
  `Homeport.Shared.<Topic>` that knows no Homeport concept. Either is listed
  in `src/Tests/Architecture.Tests/DomainExceptions.cs`.
- **The shipped surface is guarded.** `Microsoft.CodeAnalysis.PublicApiAnalyzers`
  runs on `Homeport.Sdk` and on every project it references directly or
  transitively, each with `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.
  A new public member fails the build until it is listed, so every addition
  to what Homeport ships is a line in the pull request. `Homeport.Sdk` may
  reference only an explicit list of Shared projects.
- **One package for consumers, types first.** `Homeport.Shared.Client` holds
  types only. A typed client — calls to the platform and catalog module APIs,
  tokens by client credentials — is the stated target and goes into the same
  package when it is built.
- **Version and distribution.** The package carries the Homeport release
  version, like every project. It is always published together with
  `Homeport.Sdk`, on the same channel; the channel and the first release are
  decided by the release work.
- **Internal modules** are built outside Homeport, in any language. Homeport
  recommends the same pattern for their wire types and prescribes nothing.

## Consequences

- A .NET consumer takes one small package and calls Homeport typed, without
  writing DTOs; a module developer still takes only `Homeport.Sdk`.
- Nothing leaves Homeport without showing up in a pull request, and a message
  between services cannot reach the SDK, as `Homeport.Sdk` cannot reference
  `Homeport.Shared.Internal`.
- One package carries the types of every platform service and catalog
  module. A change in one of them is a change to the package, and retiring an
  API major version removes its namespace, a breaking change of the package.
- Every public member of a shipped project is maintained in a text file next
  to it.
- The Domain never sees a wire type, so a change on the wire never forces a
  change in a Domain.
- `AGENTS.md` changes: `Homeport.Shared.Contracts` no longer holds messages
  between services. The code is brought in line with #105;
  `Homeport.Shared.Internal` is created with its first type.
