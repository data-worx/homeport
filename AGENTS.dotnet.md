# AGENTS.dotnet.md

Rules that hold for any .NET repository, whichever runtime it targets. They add
to [`AGENTS.core.md`](AGENTS.core.md) and never contradict it — where this file
is silent, the core rule applies unchanged.

This file is **shared**: it is not a stack pack of its own and cannot be picked
with `--stack`. Both `dotnet-core` (net5 and later) and `dotnet-legacy` (.NET
Framework) install it, and each adds the rules that only hold for its runtime
in `AGENTS.dotnet-core.md` or `AGENTS.dotnet-legacy.md`.

Managed by [raisr/devkit](https://github.com/raisr/devkit) — change the rules
there, not here. Deviations go in the *Deviations* table of the project
`AGENTS.md`.

## Namespaces {#dotnet.namespaces}

**The namespace always follows the directory structure.** A file in
`src/Acme.Web/Configuration/` is in `Acme.Web.Configuration`, with no exception
for extension classes or anything else. The analyser enforces it (`IDE0130`),
so a mismatch fails the gate rather than surviving in review.

**That holds from `src/` on.** A grouping directory between `src/` and a
project is a segment of the project's name, and so of its root namespace and
assembly: a file in `src/Core/Acme.Core.Domain/Orders/` is in
`Acme.Core.Domain.Orders`. `src/Tests/` is the one directory that is not a
segment (`dotnet.tests`).

## Solution layout {#dotnet.layout}

The concrete cut of `core.architecture`. It describes the target state: a
repository that has not reached it yet records the gap as a deviation, rather
than the rule bending to the code.

**The unit of the cut is the stack**: a directory under `src/` holding the
layers of one application, service or module side by side. A solution with
one stack calls it `Core`. A solution of several services, or a modular
monolith, has one stack per service or module, optionally grouped one level
deep.

```
<Name>.slnx
src/
  Core/                           one stack ...
    <Name>.Core.Contracts/
    <Name>.Core.Domain/
    <Name>.Core.Application/
    <Name>.Core.Infrastructure/
  <Group>/                        ... or several, grouped or not
    <Stack>/
      <Name>.<Group>.<Stack>.Domain/
      <Name>.<Group>.<Stack>.Api/
  <Name>.<Ui>/
  Shared/
    <Name>.Shared.Contracts/
    <Name>.Shared.<Lib>/
  Sdk/
    <Name>.Sdk/
  Tools/
    <Name>.Tools.<Tool>/
  Tests/
    Architecture.Tests/
```

Within a stack — `<Stack>` below stands for the stack's path, `Core` or
`<Group>.<Stack>`; a layer named in the table is the stack's own, and Shared
is everything in `src/Shared/`:

| Project | May reference |
|---|---|
| `<Name>.<Stack>.Contracts` — optional | Shared.Contracts; nothing else, not even beyond the BCL |
| `<Name>.<Stack>.Domain` | Contracts, Shared.Contracts; nothing else beyond the BCL, unless named as an exception |
| `<Name>.<Stack>.Application` | Domain, Contracts, Shared.Contracts, Shared libraries |
| `<Name>.<Stack>.Infrastructure[.<Source>]` | Application, Domain, Contracts, Shared, another stack's Contracts |
| `<Name>.<Stack>.<Host>` — any other project in the stack, such as `.Api` or `.Worker` | its own stack's layers, Shared, the SDK, another stack's Contracts |

Outside the stacks:

| Project | Lives in | May reference |
|---|---|---|
| `<Name>.<Ui>` — web, MAUI, WinForms, WPF, console | `src/` directly | anything but another UI |
| `<Name>.Shared.Contracts` — optional | `src/Shared/` | nothing beyond the BCL, without exception |
| `<Name>.Shared.<Lib>` — any other name | `src/Shared/` | Shared.Contracts, other Shared libraries, packages; no stack project |
| `<Name>.Sdk` — at most one | `src/Sdk/` | Shared, packages; no stack project |
| `<Name>.Tools.<Tool>` — a development tool | `src/Tools/` | anything, when it is a composition root |
| a test project | `src/Tests/` | whatever it needs to assert something, Infrastructure included |

- **A project is named `<Name>`, then every grouping directory between `src/`
  and the project, then its own name** (`dotnet.namespaces`). `<Name>` is the
  name of the solution file. The project directory carries that full name, and
  so do the `.csproj`, the assembly and the root namespace. A UI directly under
  `src/` has no grouping directory and is `<Name>.<Ui>`. `src/Tests/` is not a
  grouping directory: a test project is named as `dotnet.tests` says.
- **A stack lives in `src/<Stack>/` or `src/<Group>/<Stack>/`**, never deeper.
  `Shared`, `Sdk`, `Tools` and `Tests` are not stack names, and a stack pack
  may reserve more.
- **A stack holds its layers and its hosts, nothing else.** A host — an API, a
  worker — is the composition root of exactly one stack and holds no business
  logic. A stack without business rules may be a single host, named
  `<Name>.<Stack>` without a suffix; when rules arrive, the layers grow next
  to it and the host takes a suffix.
- **A UI directly under `src/` composes**: it may wire several stacks, and
  several UIs may share one stack. It is meant to be replaceable. A UI never
  references another UI; what two of them share belongs in Application.
- **A stack reaches another stack only through its Contracts, and only from
  Infrastructure or a host.** Calling another service is I/O, so Application
  declares an interface and Infrastructure implements it with the other
  stack's Contracts. Domain and Application never see another stack.
- **The solution file lives in the repository root**, not in `src/`. Its
  solution folders mirror the directories under `src/`.
- **Contracts is what a stack shares with its callers**: request and response
  types, messages, and the types both sides need. It exists once there is
  something to share. It references nothing but Shared.Contracts — no package
  or framework reference — so every caller can take it without taking anything
  else. A type that needs ASP.NET, such as `ProblemDetails`, stays in the host
  or UI, and Contracts describes the wire format with its own types where a
  caller needs it.
- **The Domain uses only the shared types from Contracts**, never the wire
  types. Contracts never references the Domain.
- **Shared.Contracts holds the types several stacks share**, such as a result
  type. It is BCL-only and references nothing, and every project may
  reference it.
- **A Shared library is code several stacks use**, named freely. Shared holds
  no Domain and no Application: business logic shared between independently
  deployed services is the coupling the stacks exist to prevent.
  Application may reference only a Shared library that runs in-process — the
  first question of `core.architecture`. One that needs the network, a
  database or a web framework, such as authentication on ASP.NET, is reached
  through an interface that Application declares. The tests cannot tell the
  two apart, so this half of the rule is kept in review.
- **The SDK is what developers outside the solution take to use its APIs.**
  It is one project, `<Name>.Sdk`; the types it shares live in
  Shared.Contracts, not in an SDK contracts project of its own.
- **A Domain reference beyond the BCL is an exception, and it is written
  down.** It is allowed only when it runs in-process — the first question of
  `core.architecture` — such as existing business logic the domain builds on.
  Each one is listed in `src/Tests/Architecture.Tests/DomainExceptions.cs`, and
  the *Architecture* section of the project `AGENTS.md` points at that file
  rather than restating the list.
- **`Architecture.Tests` is mandatory.** It asserts the reference graph, the
  layout, the project names and the Domain exceptions against that list, so a reference nobody
  wrote down fails the build. It is the solution-wide test project
  `dotnet.tests` allows, and the one project without the `<Name>.` prefix:
  the stack pack ships it as a template, and a template cannot land at a path
  that depends on the solution's name.
- Infrastructure split into several projects is a decision the repository
  takes and writes into its own `AGENTS.md`.

## Entry point {#dotnet.entry-point}

**No top-level statements.** The entry point is an ordinary `Program` class
with an explicit `Main`, in a namespace like every other type. The composition
root is the one place a newcomer reads first — it deserves a name, an XML doc
and a file that looks like the rest of the codebase.

## Async {#dotnet.async}

`async`/`await` end to end, and pass the `CancellationToken` through. No
`.Result`, no `.Wait()`, no `async void` except an event handler that the
framework demands.

## Dependency injection {#dotnet.di}

Constructor injection only. No service locator, no `new` on a service, no
static mutable state.

## Persistence {#dotnet.persistence}

The domain layer contains no attributes from the ORM or the web framework.
Persistence details live in configurations under the infrastructure project,
mapped onto plain models. The rules must be readable, and testable, without a
database.

## Web {#dotnet.web}

`[Authorize]` is the default on every endpoint; `[AllowAnonymous]` is a
justified exception, written down where it is used.

## Patterns we do not use {#dotnet.antipatterns}

In addition to the ones in `AGENTS.core.md`:

- `DateTime.Now` or `DateTime.UtcNow` in domain code. Inject a clock.
- `Task.Run` to make synchronous code look asynchronous.

## Tests {#dotnet.tests}

**A test project belongs to exactly one production project and is named
`<Project>.Tests.Unit` or `<Project>.Tests.Integration`.** It lives under
`src/Tests/`, and folder, `.csproj`, assembly name and root namespace all carry
that same name. A project gets a test project once it actually has tests — no
empty projects on stock.

**`src/Tests/` is not a name segment.** A test project is named after the
project under test, never `<Name>.Tests.…`, so test and source sort side by
side.

Tests that assert solution-wide rules — such as which project may reference
which — belong to no single project and carry no `.Unit`/`.Integration`
suffix. That is the only exception; a second one needs a ticket that argues
for it.

**One test class per class under test, one nested class per method under
test:**

```csharp
public sealed class OrderServiceTests            // class under test
{
    public sealed class PlaceOrderAsync          // method under test
    {
        [Fact]
        public async Task Returns_Failure_When_Cart_Is_Empty() { }

        [Fact]
        public async Task Persists_Order_When_Cart_Is_Valid() { }
    }
}
```

- The test file mirrors the source path *inside its project*:
  `src/Core/Acme.Core.Application/Orders/OrderService.cs` becomes
  `src/Tests/Acme.Core.Application.Tests.Unit/Orders/OrderServiceTests.cs`. The
  project name already says which project is under test, so it is not repeated
  as a folder.
- The method name reads `Scenario_ExpectedResult`. The method under test is
  already the nested class — do not repeat it.

## Dependencies {#dotnet.dependencies}

Check the BCL first. A NuGet package needs a reason that survives being said
out loud. No major-version upgrade without asking.

Package versions belong in the project files, which cannot go stale — not in
documentation.
