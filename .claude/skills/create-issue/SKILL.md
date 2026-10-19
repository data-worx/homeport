---
name: create-issue
description: Draft a ticket in this repository's required schema, then file it on the configured forge after approval. Use when asked to "create an issue", "open a ticket", "file a ticket", "new issue", "draft a ticket", or before starting any change that has no ticket yet.
---

# create-issue

Drafts a ticket in the schema the repository requires, shows it, waits for an
explicit yes, then files it and reports the number and the branch name to use.

The schema lives in the forge rules, `AGENTS.<forge>.md`, section
`forge.issues`. Read it — this skill drives the process, that file defines the
content. Whether a ticket is required at all comes from `DEVKIT_WORKFLOW`.

Paths below are relative to the repository root.

## 1. Gather context

```bash
bash .claude/skills/create-issue/context.sh
```

You get the forge and workflow mode, who a new ticket is assigned to, that the
CLI is authenticated and against which project, which of the six type labels
exist, and the open tickets so a near-duplicate can be spotted before filing.

Under `WORKFLOW: light` a ticket is not required. Say so once, and ask whether
the user wants one anyway — sometimes they do, and that is fine.

## 2. Draft

Follow `forge.issues`. Title `<type>: <short imperative>`; body with
**Description**, **Non-goals**, **Acceptance criteria / Definition of Done**
and **Type**; then the signature block from `core.signature`, separated by
`---`, with today's date.

The three that get skipped and should not be:

- **English**, even when the conversation is in another language.
- **Only up-front knowledge.** What you find out while working the ticket goes
  in a comment or a follow-up ticket, never back into the description:

  ```bash
  forge_issue_reply <n> /path/to/note.md
  ```

  A ticket that was already wrong when it was filed — a mistake in the draft,
  or an acceptance criterion the user agrees to change before work starts — is
  corrected in place instead, with the whole approved body:

  ```bash
  forge_issue_edit <n> /path/to/body.md
  ```

  Once work has started, a change of mind is a finding, and it goes into
  `forge_issue_reply`. The one later edit is ticking the Definition of Done
  after the merge, which `implement-feature` does (`forge.review`).
- **Verifiable acceptance criteria.** A concrete command and its expected
  result beats a sentence about things being set up correctly.

## 3. Present and wait

Show the full title and body in the chat. Create nothing on less than a clear
yes. If the user wants changes, redraft and show again.

## 4. File it

Write the approved body to a temporary file, then:

```bash
. .devkit/config.sh && . .devkit/forge.sh
forge_issue_create "<type>: <summary>" "<type>" /path/to/body.md [<assignee>]
```

The fourth argument is the `ISSUE_ASSIGNEE` that `context.sh` printed. When it
printed `none`, leave the argument off: the ticket stays unassigned, as
`forge.issues` prescribes. It is `DEVKIT_ASSIGN_ISSUES` in `.devkit/config.sh`
that turns assignment on, never a guess made while filing.

Never call `gh` or `glab` directly — `forge.sh` is what makes this skill work
on both.

If a type label is missing, `context.sh` said so. Create it with
`forge_label_create` after asking.

## 5. Report back

- the ticket number and its URL
- the branch name to use: `fix/<n>-<slug>` for a `fix` ticket, otherwise
  `feature/<n>-<slug>`

```bash
echo "scaffold the solution structure" \
  | tr '[:upper:]' '[:lower:]' | tr -cs 'a-z0-9' '-' | sed 's/^-//;s/-$//'
```

Only `feature/` and `fix/` are used, so the ticket number can be parsed back
out of the branch name. A `chore`, `docs` or `refactor` ticket still gets a
`feature/` branch.

## Gotchas

- `context.sh` needs a network round-trip. It is not offline-safe.
- The six type labels are created for this workflow. A forge's stock labels
  (`enhancement`, `bug`, `documentation`) carry no conventional-commit meaning
  here — do not use them for the type.
- Passing a body on stdin is fragile once the Markdown contains backticks.
  Write a real temporary file and pass its path.
- On GitLab a ticket is addressed by its project-internal `iid`, which is what
  `forge_issue_create` prints. Do not use the global issue id.
