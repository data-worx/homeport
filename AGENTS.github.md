# AGENTS.github.md

How work is organised on GitHub: branches, commits, issues, pull requests and
the review loop. They add to [`AGENTS.core.md`](AGENTS.core.md) and never
contradict it.

Managed by [raisr/devkit](https://github.com/raisr/devkit) — change them there,
not here. Deviations go in the *Deviations* table of the project `AGENTS.md`.

How much of this applies depends on `DEVKIT_WORKFLOW` in `.devkit/config.sh`:
under `full` all of it does, under `light` only *Branches* and *Commits*.

Slugs are prefixed `forge.` rather than `github.`, and the GitLab pack uses the
same ones — a repository that moves between forges keeps its recorded
deviations pointing at the same rules.

## Branches {#forge.branches}

```
feature/<ticket>-<short-description>
fix/<ticket>-<short-description>
```

Only these two prefixes. A `chore`, `docs` or `refactor` ticket still gets a
`feature/` branch, so the tooling parses the ticket number back out of the
branch name.

The slug is the lowercased title, words joined by `-`. The default branch
carries no ticket, by design.

## Commits {#forge.commits}

Conventional commit types: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`,
`chore:`.

```
<type>: <summary>[ (#<ticket>)]

- <change one>
- <change two>
```

- The whole first line is **at most 50 characters**, ticket suffix included. If
  it is tight, trim the summary, not the type.
- Append ` (#<ticket>)` when the branch carries one. When it does not, append
  nothing — never a placeholder.
- Second line empty. Then one `-` bullet per logical change, concise and
  technical, no trailing punctuation.
- Nothing else: no explanations, no emoji, no attribution footer, no session
  link. Inside the repository the Git history is the provenance
  (`core.signature`).

## Issues {#forge.issues}

Every change under `full` needs an issue first, in this schema:

**Title:** `<type>: <short imperative>`, with `<type>` one of `feat` `fix`
`refactor` `test` `docs` `chore` — the same word as the label, and the same
word the commit will carry.

**Body:**

```markdown
## Description

What should be done, and why. Only what is known up front — no speculation.
Facts and decisions already taken.

## Non-goals

One or two lines on what is deliberately out of scope.

## Acceptance criteria / Definition of Done

- [ ] Each item verifiable by both the maintainer and the agent.
- [ ] Where possible a concrete command and its expected result.
- [ ] Checkable facts over prose ("file X exists and contains Y", not
      "X is set up correctly").

## Type

`<type>`
```

Plus the external-system signature from `core.signature`.

- **Only up-front knowledge.** What is discovered while working the ticket goes
  into a comment or a follow-up issue, never retro-fitted into the description.
  The one edit after work starts is the checkbox state of the Definition of
  Done once the request is merged, synced from the request (`forge.review`).
  The wording of a criterion is never changed.
- No estimates, no assignees, no milestone, and no label beyond the type unless
  asked for. A repository asks for an assignee once, with
  `DEVKIT_ASSIGN_ISSUES=yes` in `.devkit/config.sh`; every new ticket then goes
  to `DEVKIT_ASSIGNEE`.
- Check the open issues for a near-duplicate before filing.
- Dependencies are part of filing: every ticket that must be done first is set
  as a native *blocked by* link right after the issue is created. Only the
  direct blockers, never the transitive ones. They are links, not prose in the
  body.

The **Acceptance criteria** is the contract being fulfilled. Read it in full
before writing code, and walk it box by box before calling the work done.

## Labels {#forge.labels}

The six type labels `feat` `fix` `refactor` `test` `docs` `chore` carry the
conventional-commit meaning and are created per repository. They are the same
six words `forge.commits` allows, so a ticket, its commits and its label never
disagree. GitHub's stock `enhancement` / `bug` / `documentation` labels are not
used for the type — they carry no such meaning here.

## Pull requests {#forge.requests}

Opened against the default branch, titled like the commit
(`<type>: <summary> (#<issue>)`), labelled with the type, and assigned. It
carries the milestone of the issue it closes, when that issue has one, so the
forge's milestone view shows the request that did the work.

The body carries:

- **what** changed and **why**
- an **Evidence** block with the gate output
- the **Definition of Done** copied from the issue as checkboxes, ticked where
  it holds and left unticked with the reason where it does not — a deviation is
  called out, never hidden
- a **Changelog** line naming the category the entry went under, or `n. a.`
  with the reason it was skipped
- a **Documentation** line naming the document that was updated, or `n. a.`
  with the reason none needed it
- `Closes #<issue>`
- the external-system signature from `core.signature`

The **Documentation** line exists for the reason the changelog one does.
`core.docs` binds whether or not anyone asks, and a rule nobody ever has to
answer for is a rule that quietly stops being kept.

## Changelog {#forge.changelog}

`CHANGELOG.md` follows [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/).

A user-visible change **requires** an entry under `## [Unreleased]` in the
right category (`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`,
`Security`). Write it for a person reading release notes — what changed and why
it matters, not the commit subject.

Purely internal work with no user-visible effect — tests, tooling, CI,
documentation only — may skip the entry. When it is skipped, say so in one line
in the request body.

## Review loop {#forge.review}

The maintainer reviews in the pull request and comments there. For each round:
address every point, re-run the gates, commit, push, and reply on the request
naming the commit that resolved each point. Sign the reply.

**The work is finished when the maintainer accepts or merges the request** —
not when the gates pass. After the merge, bring the ticket in line with the
request: tick every box of its Definition of Done that the request ticked, and
list every box the request left open in one signed comment on the ticket, with
the reason the request gave. Then delete the branch.
