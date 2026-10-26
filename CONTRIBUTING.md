# Contributing to Homeport

Thank you for considering a contribution. This page covers what you need before
your first pull request. The binding rules for the code itself are in
[AGENTS.md](AGENTS.md).

## Contributor License Agreement

**Every contribution requires a signed [Contributor License Agreement](CLA.md).**
Without one, a pull request cannot be merged.

You sign it once, in the pull request itself: the CLA bot comments on your
first pull request, and you reply with the sentence it asks for. The signature
is stored with your GitHub account name and covers all your later
contributions.

Homeport is licensed under AGPL-3.0-or-later. The CLA lets the copyright
holder license your contribution under other terms as well — see
[ADR-0001](docs/architecture/adr/0001-distribution-and-licensing.md).

## Before you start

- **Questions, ideas and proposals** go to
  [GitHub Discussions](https://github.com/data-worx/homeport/discussions), not
  to issues.
- **Every change starts with an issue.** Look for an existing one first. If
  there is none, open one and wait for it to be accepted before writing code.
- **Architecture decisions are binding.** A change that contradicts one of the
  [ADRs](docs/architecture/adr/README.md) needs a new ADR that supersedes it,
  agreed before the code is written.
- **Security issues** are never reported in public. See
  [SECURITY.md](SECURITY.md).

## Workflow

1. Fork the repository and create a branch named
   `feature/<issue>-<short-description>` or `fix/<issue>-<short-description>`.
2. Make the change in small, reviewable commits. Commit messages follow the
   conventional-commit format described in
   [AGENTS.github.md](AGENTS.github.md#commits-forgecommits).
3. Run the gates and make sure they are green:

   ```bash
   bash .devkit/gates.sh
   ```

4. Open a pull request against `main`, titled like a commit
   (`<type>: <summary> (#<issue>)`), with `Closes #<issue>` in the body.
5. Answer review comments in the pull request. Pull requests are
   squash-merged; the title becomes the commit on `main`.

## Requirements for a change

- New or changed logic comes with tests.
- Documentation that the change makes wrong is updated in the same pull
  request.
- A user-visible change has an entry in [CHANGELOG.md](CHANGELOG.md).
- Every source file starts with `SPDX-License-Identifier: AGPL-3.0-or-later`.
- New dependencies are permissively licensed (MIT, Apache-2.0, BSD, ISC,
  PostgreSQL) and recorded in `THIRD-PARTY-NOTICES.md`.

## Language

Everything in the repository is English: code, comments, commit messages,
issues, pull requests and documentation.
