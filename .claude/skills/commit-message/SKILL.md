---
name: commit-message
description: Draft a commit message in this repository's required format, then commit and push after approval. Use when asked to "commit", "write a commit message", "commit and push", or "draft a commit".
---

# commit-message

Drafts a commit message, then commits and pushes. Whether it waits for a yes
first is `DEVKIT_COMMIT_APPROVAL` in `.devkit/config.sh`; it defaults to `ask`
and to asking when the key is absent.

The format is defined by the forge rules in `AGENTS.<forge>.md`, section
`forge.commits`. Read that file — this skill drives the process, the rules
define the text.

Paths below are relative to the repository root.

## 1. Collect the change set

```bash
bash .claude/skills/commit-message/collect.sh
```

You get: the workflow mode, the approval mode, the branch, the ticket number
parsed out of it, the scope of the diff shown, the file list and the diff
itself.

If `SCOPE` is `working tree (nothing staged)`, decide with the user which files
belong in the commit before staging anything.

An `OUTSIDE THE COMMIT` line means the tree is only partly staged, so the diff
you were given is not the whole change. That is usually an accident — `git mv`
and `git rm` stage themselves — so ask before drafting against half of it.

## 2. Check the workflow mode

- `WORKFLOW: full` and no ticket → **stop**. Under `full`, a commit belongs to
  a ticket, on a `feature/<n>-…` or `fix/<n>-…` branch. Say so and ask what the
  user wants: create the ticket and rename the branch, or drop to `light` for
  this repository. Committing anyway is not one of the options.
- `WORKFLOW: light` → carry on. No ticket is expected.
- On the default branch, in either mode, say so before drafting: a commit
  straight to the default branch is usually not what was intended.

## 3. Draft

Follow `forge.commits`. In short:

```
<type>: <summary>[ (#<ticket>)]

- <change one>
- <change two>
```

First line at most 50 characters including the ticket suffix; one bullet per
logical change; English; nothing else in the message. No attribution footer and
no session link — inside the repository the Git history is the provenance.

## 4. Present, as `COMMIT_APPROVAL` says

- `ask` — show the drafted message and wait. Do not proceed on anything less
  than a clear yes; if the user wants changes, redraft and show again.
- `auto` — show the message and carry straight on to step 5. It is still
  printed, so the user reads what went out, but nothing waits.

This is the one step in the lifecycle that still asks (`core.git`): the commit
message is the only text that reaches others under a human name without the
agent marking, and published history is never rewritten.

## 5. Commit and push

```bash
git commit -F - <<'EOF'
<the approved message>
EOF
git push
```

Report the short hash and the result of the push.

## Gotchas

- With `core.autocrlf` set, `git` prints `LF will be replaced by CRLF` when
  staging text files. Harmless — not an error, and not something to fix.
- `collect.sh` shows `git diff HEAD` in the unstaged case, which covers staged
  and unstaged changes together. It does **not** show the content of untracked
  files — only their paths appear in the file list. Read those yourself if they
  belong in the commit.
- The ticket pattern accepts a bare number (`42`) or a prefixed key
  (`PROJ-42`), and requires a `-` and a description after it.
- A first commit into an empty repository has no `HEAD`; `git diff HEAD` fails
  there. Use `git status --short` and read the files.
