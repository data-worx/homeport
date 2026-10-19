# AGENTS.md — homeport

Binding rules for anyone working in this repository, human or agent.
**It prescribes, it does not describe** — explanatory prose belongs in `docs/`,
and this file links to it.

## Shared rules

These files come from [raisr/devkit](https://github.com/raisr/devkit) and are
updated by `/devkit-sync`. Do not edit them here; change them upstream.

The `@` lines below are imports, not links: they pull the file into the
session. Turning one into a Markdown link silently switches the rules off.

@AGENTS.core.md — rules that hold in every repository
@AGENTS.dotnet.md — rules for dotnet
@AGENTS.dotnet-core.md — rules for dotnet-core
@AGENTS.github.md — conventions for github

@AGENTS.local.md — personal, machine-specific, git-ignored; wins on conflicts.
It is imported the same way, and the import is simply ignored where the file
does not exist.

## Overview

<!-- What this project is, in three or four sentences. What it does, who runs
     it, and the one or two facts a newcomer gets wrong without being told. -->

## Tech stack

<!-- Only what binds. The descriptive version belongs in docs/tech-stack.md. -->

## Architecture

<!-- The dependency rules that must not be broken, and what happens when they
     are. The current project layout belongs in docs/architecture.md. -->

## Domain terms

<!-- | Term | Means | — the words this project uses in a specific sense. -->

## Deviations from the shared rules

Rules from `AGENTS.core.md` or a stack pack that deliberately do not apply here.
Every row was decided once and is recorded in `devkit.lock.json`, so
`/devkit-sync` does not ask again until the upstream rule changes.

| Rule | Deviation | Why |
|---|---|---|
| — | none yet | — |
