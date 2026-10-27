# AGENTS.dotnet-core.md

Rules for a repository on **.NET 5 or later** — the SDK-style, cross-platform
runtime. They add to [`AGENTS.dotnet.md`](AGENTS.dotnet.md), which holds
everything that is true of .NET whatever the runtime, and through it to
[`AGENTS.core.md`](AGENTS.core.md). Neither is contradicted here.

For .NET Framework, see the `dotnet-legacy` pack. The two never ship together.

> **On the name.** ".NET Core" is a retired product name — it ended at 3.1, and
> net5 and later are officially just ".NET", so this pack names a platform its
> users do not run. That was argued and the maintainer chose the name anyway.
> Renaming it is a breaking change for every recorded deviation pointing at a
> `dotnet-core.` slug, so do not change it back without asking.

Managed by [raisr/devkit](https://github.com/raisr/devkit) — change them there,
not here. Deviations go in the *Deviations* table of the project `AGENTS.md`.

## Compiler settings {#dotnet-core.compiler}

Keep `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
switched on. A warning that is worth suppressing is worth suppressing
explicitly, at the narrowest scope, with a reason.

A property lives where it takes effect. MSBuild imports `Directory.Build.props`
before a project file's own properties, so a value the project file sets always
wins over it.

- Target framework, `Nullable` and `ImplicitUsings` stay in each project file,
  where `dotnet new` writes them. Every project targets the same framework; one
  that targets another is a deviation and needs a reason.
- Language version and version number are set once in `Directory.Build.props`.
  No project file sets them; one that does is a deviation and needs a reason.

## Language style {#dotnet-core.style}

**`.editorconfig` is the source of truth for style, not this section.** It is
binding (`core.design`), it ships with this pack, and `dotnet format` enforces
it. Restating a setting here would only give it a second place to drift. What
is meant to bind stands there at `warning`, which `TreatWarningsAsErrors` turns
into a build error; what stands at `suggestion` is a preference, and raising it
is a deliberate change to the block, not a sentence in this file.

What the file cannot express, and therefore belongs here:

- Records for DTOs and commands.
- The platform constants instead of literals (`core.design`):
  `HttpMethods.Post`, `StatusCodes.Status404NotFound`,
  `MediaTypeNames.Application.Json`.

## Logging {#dotnet-core.logging}

Log through `ILogger<T>` with structured templates:

```csharp
logger.LogInformation("Order {OrderId} shipped", id);
```

No `Console.WriteLine`, and no string interpolation inside a log template — it
destroys the structure the template exists for. User-facing console output,
such as a startup banner, is not logging and is the one exception.

## Configuration {#dotnet-core.configuration}

Configuration through `IOptions<T>`. No magic strings scattered around, no
`IConfiguration` reached into from the middle of a use case.

## Web APIs {#dotnet-core.web}

HTTP API endpoints are controllers: classes marked `[ApiController]`,
deriving from `ControllerBase`, routed by attribute. Minimal APIs — `MapGet`,
`MapPost`, route groups — are not used for them. A repository that uses
minimal APIs records that as a deviation of this rule (`core.deviations`).

Endpoints the framework maps itself, such as `MapHealthChecks` and
`MapOpenApi`, are not API endpoints in this sense and are unaffected.

## Gates {#dotnet-core.gates}

Four, all green before a commit, all runnable by hand:

| Gate | Command | Expected |
|---|---|---|
| build | `dotnet build <solution> -warnaserror` | 0 warnings, 0 errors |
| test | `dotnet test <solution>` | green |
| format | `dotnet format <solution> --verify-no-changes` | clean |
| notices | the generator into a temporary file, diffed against `THIRD-PARTY-NOTICES.md` | no difference, or skipped |

They live in `.devkit/gates.sh`, which the project owns and adapts.

`-warnaserror` is on the gate itself rather than left to
`TreatWarningsAsErrors` in a `Directory.Build.props` the project owns. It also
promotes the NuGet audit warnings, so a newly published advisory can turn the
build red without a code change. That is intended: a known-vulnerable package
is a reason not to commit.

`dotnet test` exits non-zero when a test project contains **no** tests. A new
test project needs at least one real test, not a placeholder.

**The `test` gate carries the architecture tests.** The pack installs
`src/Tests/Architecture.Tests/`, the project `dotnet.layout` makes mandatory,
as a template. Bootstrap does not edit the solution, so it is added once by
hand — until it is, the gate passes without running it:

```bash
dotnet sln add src/Tests/Architecture.Tests/Architecture.Tests.csproj
```

The tests read the `.csproj` files rather than the built assemblies, because
the compiler drops a reference the code never uses. They find the repository
root by its `.git`, so they run in a checkout, not in a copied build output.

## Third-party notices {#dotnet-core.notices}

**A repository that ships a distributable build carries the notices gate.**
MIT, Apache-2.0 and most other package licences make it a condition that the
copyright notice and the licence text travel with the software. A library or an
internal service ships nothing to give notice for and leaves it off.

- `NOTICES_PROJECT` in `.devkit/gates.sh` names the project whose
  `dotnet publish` output is shipped. Empty skips the gate.
- `.devkit/generate-third-party-notices.cs` writes `THIRD-PARTY-NOTICES.md` at
  the repository root from that publish's `deps.json` — what the build actually
  ships, not what the project references — once per runtime identifier the
  project lists. It needs the .NET 10 SDK. After a package change, regenerate
  and commit the file:

  ```bash
  dotnet run --file .devkit/generate-third-party-notices.cs -- --project <NOTICES_PROJECT>
  ```

- Licence texts live in `.devkit/licenses/<SPDX>.txt`; the generator fails on a
  licence it has no text for. Copyright notices a package does not declare, and
  shipped files that are not packages, go into
  `.devkit/third-party-notices.overrides.json` — copied from the source, never
  made up.
- To regenerate on every publish, hook the generator into the shipped project
  with `BeforeTargets="Publish"`, never `AfterTargets="Build"`: an editor's
  background builds fire `Build` constantly, and the generator publishes the
  project itself, so the hook retriggers itself without end. Call it as
  `dotnet run --file`, since a bare file argument inside a project directory
  resolves to the project.
