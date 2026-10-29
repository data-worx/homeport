# Documentation

Explanation and reference: how it works and why it was built that way. The
binding rules live in [`AGENTS.md`](../AGENTS.md), not here.

Arranged by reader. A directory exists once it has its first document.

## Operators — installing, configuring and running Homeport

| Document | About |
|---|---|
| [`operators/running-locally.md`](operators/running-locally.md) | Starting Homeport on one machine, with the AppHost or with Docker Compose |
| [`operators/signing-key.md`](operators/signing-key.md) | Generating, configuring and rotating the gateway's signing key |

## Module developers — building a module on the contract and the SDK

| Document | About |
|---|---|
| [`module-developers/declaring-a-manifest.md`](module-developers/declaring-a-manifest.md) | Declaring and serving a module's manifest with `Homeport.Sdk` |
| [`module-developers/answering-errors.md`](module-developers/answering-errors.md) | Answering errors as Problem Details with `Homeport.Sdk` |

## API consumers — obtaining a token, calling modules, reading usage

| Document | About |
|---|---|
| [`api-consumers/calling-a-module.md`](api-consumers/calling-a-module.md) | The URL scheme for calling a module through the gateway |
| [`api-consumers/errors.md`](api-consumers/errors.md) | The error format, and what the gateway does to a module's errors |

## Architecture — why Homeport is built the way it is

| Document | About |
|---|---|
| [`architecture/vision.md`](architecture/vision.md) | What Homeport is and is not, why it exists, and the target picture |
| [`architecture/architecture.md`](architecture/architecture.md) | The projects, the directory layout and why they are cut this way |
| [`architecture/adr/`](architecture/adr/README.md) | Architecture decision records |

## Reference — normative specifications

| Document | About |
|---|---|
| [`reference/module-contract/`](reference/module-contract/README.md) | The language-neutral contract every module implements |
| [`reference/problems/`](reference/problems/README.md) | Homeport's problem types, one page each |

## Design

| Document | About |
|---|---|
| [`../design/`](../design/README.md) | The design system: tokens, icons, fonts, logos and illustrations |
