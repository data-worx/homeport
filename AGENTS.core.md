# AGENTS.core.md

Rules that hold in every repository, whatever it is built with and wherever it
is hosted. They are binding for humans and agents alike.

Managed by [raisr/devkit](https://github.com/raisr/devkit) — change them there,
not here. Where a project deliberately departs from a rule, the exception is
recorded in the *Deviations* table of its own `AGENTS.md`, not by editing this
file.

Section slugs (`{#core.…}`) are stable identifiers. A recorded deviation points
at one, so they survive rewording of the text beneath them.

## Working style {#core.working-style}

- **When something is unclear, ask instead of guessing. No answer beats a bad
  answer.** This outranks every other rule here.
- Anything touching more than one file: agree the approach first, then write
  code. A plan is what you will change and why, not an essay.
- Single-file, obvious changes: just make them.
- Work in small, individually reviewable steps. A large correct change nobody
  can review is worse than three small ones.
- Prefer the boring solution. Introduce a pattern only where it earns its
  complexity.
- Do not fix what nobody asked about. Mention it instead and let the maintainer
  decide whether it is worth a detour.
- **Verify before you claim.** Run the build, run the tests, read the file back.
  "Should work" is not a result.
- Keep the working tree reviewable: no stray scratch files, no reformatted
  files nobody touched.

## Language {#core.language}

Everything written into a repository is **English**: identifiers, comments, API
documentation, commit messages, branch names, tickets, request descriptions and
the documentation itself. Discussion may happen in any language; the repository
does not care what language was spoken.

## Documentation {#core.docs}

Three places, three roles. Keep them apart:

| Where | Role |
|---|---|
| `README.md` | First contact: what this is, why it exists, how to get it running, status, licence |
| `docs/` | Explanation and reference: how it works and why it was built that way. `docs/README.md` is the index, one line per document |
| `AGENTS.md` | Binding rules and conventions. **It prescribes, it does not describe** — explanatory prose belongs in `docs/`, and the file links to it |

**Documentation is part of the change.** When a change makes a document in
`docs/`, the `README.md` or `AGENTS.md` wrong, incomplete or misleading,
updating it belongs in the same branch and the same request — not in a
follow-up ticket. A change that leaves documentation contradicting the code is
not done.

Conversely: no documentation is written for something that does not exist yet.

## Architecture {#core.architecture}

- **The dependency arrow points inwards, never outwards.** The centre holds the
  models and the rules that operate on them and knows nothing about the outside
  world. Every layer around it may depend only on layers closer to the centre.
- Infrastructure is never referenced from the layers it serves. When a use case
  needs the database, the file system or the clock, it declares an interface
  and infrastructure implements it.
- Only a composition root may wire concrete infrastructure to a use case. Every
  other project that reaches for infrastructure is a design error.
- **Nothing outside the centre decides what is valid.** Guard clauses belong in
  the model, not in the controller.
- **Two questions decide where a dependency belongs, and who published it is
  neither of them.** First: does it run in a unit test without touching
  anything outside the process? If it does, it is ordinary code and may be
  referenced where it is used. If it does not — network, database, file system,
  clock, randomness, environment, another service — it belongs outside the
  centre. Second: who calls whom? Your code calls the library, so the inner
  layer declares an interface and infrastructure implements it. The library
  calls your code — a web framework, a CLI, a scheduler, a message consumer —
  so it belongs to the presentation or host layer and gets no interface. A
  framework is not injected.
- Whether the infrastructure layer is one unit of code or several is a decision
  the repository takes and writes into its own `AGENTS.md`. Where nothing is
  written there, it is one. The centre references a framework in neither
  direction.
- Where the rules can be asserted by a test, assert them. A reference-graph
  rule that only lives in prose is a rule that gets broken.
- **Do not weaken a layer rule to make something compile.** If the dependency
  direction is in the way, the design is wrong, not the rule.

## Design {#core.design}

- **The SOLID principles are mandatory, not aspirational** — single
  responsibility, open/closed, Liskov substitution, interface segregation,
  dependency inversion. A change that violates one may be rejected in review
  even when it compiles and the tests pass.
- **No magic strings or numbers.** A literal that carries meaning is a named
  constant; where the platform already defines one, use it. A literal in a
  test that states the expected wire format, such as an expected JSON body, is
  the expectation itself, not a magic value.
- The editor configuration in the repository root is binding and overrules any
  differing opinion.

### Patterns we do not use — never suggest them {#core.antipatterns}

- Exceptions as control flow for expected business outcomes. Use a result type.
- Static helpers that hold state; a service locator.
- DDD building blocks: value objects, aggregate roots,
  repositories-per-aggregate. Plain models and the rules that operate on them
  are enough. An event that records something that happened — inside a
  service or between services — is not a DDD building block and is allowed;
  what is excluded is the DDD machinery, not the idea of an event.

## Tests {#core.tests}

- **New or changed logic without a test counts as unfinished**, even when
  nobody asked for one.
- One behaviour per test. Arrange, act, assert. No logic in the test itself.
- A test name says what happens and what is expected, not which unit is being
  called — the structure already says that.
- A reader finds the test from the source and the source from the test without
  searching. How that is laid out is a stack concern, not a rule here.
- Integration tests run against a throwaway database created for the run, never
  against a shared one.

## Security {#core.security}

- No secrets in the repository. Use the platform secret store locally,
  environment variables when deployed. Secrets never go into a log or a chat
  message either.
- No concatenated SQL. Parameterised queries or a query API only.
- Validate input on the server, encode output, use anti-forgery tokens on
  forms.
- Authorise every endpoint explicitly. An anonymous endpoint is a justified
  exception, not the default.
- No personal data in logs.

## Logging {#core.logging}

Code that can fail in production logs enough to diagnose the failure without
a debugger. Each level is required when its condition occurs:

| Level | Required when |
|---|---|
| Error | An exception is caught at a boundary and not rethrown (global handler, background job, message consumer), or a guarantee is broken: data is lost, a message lands in the dead-letter queue |
| Warning | A degraded path that carries on: a fallback kicks in, retries are exhausted, an external dependency fails, a request is rejected for a security reason (with the reason, never the credential), configuration falls back to a default |
| Information | A state change of the process or the system: start with the effective configuration without secrets, registration, a changed routing table, key rotation, the end of a job with counts. Not per request |
| Debug | Per-request decisions that help a diagnosis: which route, a cache hit, why a 4xx |

- **No log-and-rethrow.** An error is logged once, where it is handled.
- **No `catch` that swallows.** Every `catch` logs, rethrows or turns the
  error into a result.
- **No secrets, tokens, personal data, or request and response bodies in a
  log.**

How to log is a stack concern, not a rule here.

## Gates {#core.gates}

Every repository defines the checks that must pass before a commit, and makes
them runnable by a human in one command. They pass before anything is
committed — not afterwards, not in CI only.

A gate that is regularly skipped is either wrong or unnecessary. Fix it or
delete it; do not learn to ignore it.

## Git {#core.git}

- An agent acts on instruction, never on its own initiative. The instruction
  that starts a change covers the lifecycle that follows from it: the branch,
  the request, the replies in the review round, and deleting the branch
  afterwards. It is not asked for again step by step.
- **The commit message is the exception.** `core.signature` exempts commit
  messages from the agent marking, so it is the one text that goes out under a
  human name without saying an agent drafted it — and published history is
  never rewritten, so a bad one stays. It is shown and approved before the
  commit, unless `DEVKIT_COMMIT_APPROVAL` in `.devkit/config.sh` says `auto`.
- Where a change is large, say how it would be sliced into commits before
  making them.
- Never rewrite published history.

## Boundaries {#core.boundaries}

- **Never** run a migration, a script or a write of any kind against a
  production system.
- No new dependency without justification — check whether the standard library
  already covers it.
- No major-version upgrade of an existing dependency without asking.
- Do not touch generated files.

## Personal notes {#core.local}

`AGENTS.local.md` next to the project `AGENTS.md` is imported from there and
wins on conflicts. It carries personal and machine-specific settings, is
git-ignored, and is never required for anyone else to work on the repository —
where it does not exist, the import is simply ignored.

## Signing AI-generated content {#core.signature}

When an agent creates content in **external** systems — issues, requests,
wikis, comments, boards — it must be recognisable as not typed by a human.
Append the signature, separated by `---`:

```markdown
---
🤖 *Claude was here. No hands, but opinions.*
*<YYYY-MM-DD>*
```

The wording is fixed — do not rephrase it per context, or it stops being
reliably searchable. Search term: **`Claude was here`**.

This does not apply to files inside the repository, nor to commit messages:
there the Git history is the provenance.

## Recording a deviation {#core.deviations}

A project may depart from a rule here. It does so openly:

1. A row in the *Deviations* table of the project `AGENTS.md`: which rule, what
   is done instead, and why.
2. An entry in `devkit.lock.json` naming the section slug and the devkit commit
   the decision was made against.

Both, or neither. A deviation that exists only in the code is a rule violation,
and a deviation recorded only in the lock file is invisible to the next person
who reads the rules.
