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

```
<Name>.slnx
src/
  Core/
    <Name>.Core.Domain/
    <Name>.Core.Application/
    <Name>.Core.Infrastructure/
  <Name>.<Ui>/
  Tools/
    <Name>.Tools.<Tool>/
  Tests/
    Architecture.Tests/
```

| Project | Lives in | May reference |
|---|---|---|
| `<Name>.Core.Domain` | `src/Core/` | nothing beyond the BCL, unless named as an exception |
| `<Name>.Core.Application` | `src/Core/` | Domain |
| `<Name>.Core.Infrastructure[.<Source>]` | `src/Core/` | Application, Domain |
| `<Name>.<Ui>` — web, MAUI, WinForms, WPF, console | `src/` directly | anything but another UI |
| `<Name>.Tools.<Tool>` — a development tool | `src/Tools/` | anything, when it is a composition root |
| a test project | `src/Tests/` | whatever it needs to assert something, Infrastructure included |

- **A project is named `<Name>`, then every grouping directory between `src/`
  and the project, then its own name** (`dotnet.namespaces`). `<Name>` is the
  name of the solution file. The project directory carries that full name, and
  so do the `.csproj`, the assembly and the root namespace. A UI directly under
  `src/` has no grouping directory and is `<Name>.<Ui>`. `src/Tests/` is not a
  grouping directory: a test project is named as `dotnet.tests` says.
- **`src/Core/` holds the application, not its UIs.** A UI is a composition
  root: it wires Infrastructure to the use cases and holds no business logic.
  It is meant to be replaceable, and a repository may have several side by
  side.
- **A UI never references another UI.** What two of them share belongs in
  Application.
- **The solution file lives in the repository root**, not in `src/`. The
  solution folders `/Core/`, `/Tools/` and `/Tests/` list the same projects as
  the directories of the same name.
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
- A separate host project, or Infrastructure split into several projects, is a
  decision the repository takes and writes into its own `AGENTS.md`.

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
