---
name: implement-feature
description: Drive a change end to end - ticket, branch, implementation in small steps, the gates, commit, request and the review loop. Use when asked to "implement", "build a feature", "work on issue #N", "fix a bug", "start on the ticket", or "pick up" a piece of work.
---

# implement-feature

The lifecycle of a code change in this repository. Nothing here is optional
and the order matters.

How much of it applies is decided by `DEVKIT_WORKFLOW` in `.devkit/config.sh`:

- **`full`** — all nine steps.
- **`light`** — steps 3, 5 and 6 only: implement, gates, commit. No ticket, no
  request, no review loop.

Read it first:

```bash
. .devkit/config.sh && echo "${DEVKIT_WORKFLOW} / ${DEVKIT_FORGE} / ${DEVKIT_MAIN_BRANCH}"
```

Related skills: `create-issue` (step 1), `commit-message` (step 6).
Paths are relative to the repository root.

**The instruction that started this covers the whole run** (`core.git`): the
branch, the request, the replies in the review round and deleting the branch
afterwards happen without asking again. The one step that still stops is the
commit message in step 6, and only while `DEVKIT_COMMIT_APPROVAL` is `ask`.
Stopping anywhere else means something is genuinely unclear — say what, rather
than asking for permission to carry on.

## 1. There must be a ticket — `full` only

Every change needs one. If there is none, invoke `create-issue` and get it
filed before writing code. If the user pointed at an existing ticket, read it
in full now — the **Acceptance criteria / Definition of Done** is the contract
being fulfilled.

## 2. Branch — `full` only

```bash
git checkout "${DEVKIT_MAIN_BRANCH}" && git pull
git checkout -b feature/<ticket>-<slug>     # fix/<ticket>-<slug> for a fix
```

Only `feature/` and `fix/`, so the tooling parses the number back out. See
`forge.branches`.

Under `light`, work on a branch anyway when the change is more than a moment,
but nothing forces a name.

## 3. Implement in small, reviewable steps

- The rules bind in both modes. `AGENTS.core.md`, the stack rules, the forge
  rules, and the project `AGENTS.md` on top — including its *Deviations* table.
- **New or changed logic without a test counts as unfinished** (`core.tests`),
  even under `light`, even when nobody asked.
- Anything touching more than one file: agree the approach with the user before
  diving in (`core.working-style`).
- Keep the working tree reviewable — no stray files, no reformatted files you
  never touched.
- Documentation is part of the change (`core.docs`). A change that leaves
  `docs/`, the `README.md` or `AGENTS.md` contradicting the code is not done.

## 4. Changelog — `full` only

See `forge.changelog`. A user-visible change needs an entry under
`## [Unreleased]` in the right category. Purely internal work may skip it —
and when it is skipped, one line in the request body says why.

## 5. Gates — both modes

```bash
bash .devkit/gates.sh
```

All green before anything is committed. Not afterwards, not in CI only.

Under `full`, then walk the Definition of Done yourself, box by box. The gates
do not cover reference graphs, exposed APIs or documentation. Proceed only when
every box is genuinely true — and where one is not, say which and why rather
than quietly leaving it.

## 6. Commit — both modes

Invoke `commit-message`. It reads the workflow and approval modes, parses the
ticket out of the branch, drafts in the required format, and commits and
pushes. Under `DEVKIT_COMMIT_APPROVAL=ask` it stops for a yes first — the only
stop in this lifecycle. Repo commits carry no AI footer.

## 7. Open the request — `full` only

```bash
. .devkit/config.sh && . .devkit/forge.sh
forge_pr_create "${DEVKIT_MAIN_BRANCH}" "<branch>" \
  "<type>: <summary> (#<issue>)" "<type>" "${DEVKIT_ASSIGNEE}" /path/to/body.md \
  "$(forge_issue_milestone <issue>)"
```

The last argument gives the request the issue's milestone (`forge.requests`).
An issue without one prints nothing, and the request is created without one.

The body follows `forge.requests`: what and why, an **Evidence** block with the
gate output, the **Definition of Done** copied from the issue as checkboxes and
ticked box by box, a **Changelog** line naming the category or `n. a.` with the
reason, a **Documentation** line naming the document that was updated or `n. a.`
with the reason none needed it, `Closes #<issue>`, and the signature from
`core.signature`.

The documentation line is where step 3 is answered for. `core.docs` is not
satisfied by intending to update a document, and `n. a.` is a claim the
maintainer reads — so write the reason, not the abbreviation alone.

Never call `gh` or `glab` directly.

## 8. Review loop — `full` only

The user reviews in the request and comments there, then says to look:

```bash
. .devkit/config.sh && . .devkit/forge.sh
forge_pr_comments <n>
forge_pr_line_comments <n>
```

Address **every** point, re-run the gates, commit through `commit-message`,
push. Then reply on the request, naming the commit that resolved each point and
signed with the same block:

```bash
forge_pr_reply <n> /path/to/reply.md
```

Nothing is waved away silently; where you disagree, say so in the reply. Where
a point makes the request body itself wrong — a changed Evidence block, a
Definition of Done that now reads differently — replace it rather than
correcting it in a comment:

```bash
forge_pr_edit <n> /path/to/body.md
```

Both take a file, like every other body in these skills: the text is full of
backticks and does not survive being passed as a string.

## 9. Done — `full` only

The work is finished when **the user accepts or merges the request** — not when
the gates pass. After the merge, bring the ticket in line with the request
(`forge.review`):

```bash
. .devkit/config.sh && . .devkit/forge.sh
forge_issue_body <issue> > /path/to/issue.md
# tick every box in /path/to/issue.md that the request body ticked -
# the state only, never the wording of a criterion
forge_issue_edit <issue> /path/to/issue.md
forge_issue_reply <issue> /path/to/open-boxes.md   # only when a box stayed open
```

The request body is the source, as you last wrote it through `forge_pr_create`
or `forge_pr_edit`: you tick it, the maintainer comments. The comment lists
each open box with the reason the request gave, and carries the signature from
`core.signature`. When every box holds, there is no comment.

Then:

```bash
git checkout "${DEVKIT_MAIN_BRANCH}" && git pull && git branch -d <branch>
```

## Gotchas

- Windows is case-insensitive: never `rm` a path that differs from another only
  in case (`src/tests` vs `src/Tests`) — you will delete the wrong one. Use
  `git mv` through a temporary name.
- With `core.autocrlf`, `git` prints `LF will be replaced by CRLF` on staging.
  Harmless; `.gitattributes` normalises to LF in the repository.
- `gates.sh` runs the real build and test. A cold run takes tens of seconds.
  That is expected, not a hang.
- `forge_pr_view <branch>` only finds the request while the branch exists
  locally and on the remote. After step 9, use the number.
- Switching `DEVKIT_WORKFLOW` mid-change is not a way past a failing step.
  Changing the mode is a decision about the repository, taken by the user, not
  a workaround.
