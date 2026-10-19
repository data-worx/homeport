---
name: devkit-sync
description: Update this repository's shared rules, skills and settings from raisr/devkit, settling every conflict with the user before writing. Use when asked to "sync the devkit", "check for changed rules", "update AGENTS.core.md", "pull the shared rules", or "are the rules still current".
---

# devkit-sync

Brings the vendored devkit files in this repository up to the current state of
[raisr/devkit](https://github.com/raisr/devkit), and puts every judgement call
to the user instead of guessing.

It writes files. It never stages and never commits.

Paths below are relative to the repository root.

## 1. Read the current state

```bash
bash .claude/skills/devkit-sync/collect.sh
```

It clones the devkit, works out which files this repository receives, and
prints for each one the **upstream** hash and the **installed** hash. The clone
stays on disk — the printed `CLONE:` path is where the new content is read
from.

Then read `devkit.lock.json` yourself. It holds the third hash, the one
`collect.sh` cannot know: what was installed **last time**, per file, plus the
deviations already decided and the devkit commit each was decided against.

## 2. Classify every file

Three hashes per file: `recorded` (from the lock), `upstream` and `installed`.
All three are hashes of **devkit source content**, which is what makes them
comparable at all.

For `managed` and `block` entries:

| recorded vs upstream | recorded vs installed | Meaning | Do |
|---|---|---|---|
| same | same | untouched on both sides | nothing, do not mention it |
| same | **differs** | edited here, not upstream | **present it** |
| **differs** | same | changed upstream, clean here | see below |
| **differs** | **differs** | changed on both sides | **present it** |

For *changed upstream, clean here*, read the actual diff:

- wording, typography, a clarified example, a reordered list — **apply it
  silently** and show it in the collected diff at the end
- a rule added, removed, tightened or loosened — **present it**

**A `template` is judged on one axis only.** Its `installed` column reads
`owned`, not a hash: the file was written once through token substitution and
belongs to the project ever since, so comparing it to the source proves
nothing. The only question is whether the upstream template moved on — if it
did, show the user what changed and let them decide whether to take anything
across by hand. Never overwrite a template.

`installed` reading `no-marker` means a block target lost its markers. Do not
patch it, do not append a fresh block — stop and ask (step 5).

A file that upstream no longer has, or a skill that vanished, is presented too.

## 3. Read the project rules against the new ones

This is the part no hash can do. Read the project `AGENTS.md`, its
*Deviations* table, and `AGENTS.local.md` if it exists. Then check every
**new or changed** upstream rule against them.

A new core rule that contradicts something the project decided is a conflict
even when every hash matches. Present it.

Skip what is already settled: a deviation recorded in `devkit.lock.json`
answers for its rule as long as that rule has not changed since the
`decidedAt` commit. When it has changed, the old decision was made against a
text that no longer exists — ask again, and say so.

## 4. Put the open points to the user

One at a time. Never a list of five questions.

For each: what the rule said, what it says now, what the project does today,
and **a recommendation with a reason**. The user decides. Offer the three
outcomes plainly:

- take the upstream rule, and change the code or the project rules to match
- record a deviation
- take it for now and open a ticket for the code change

## 5. Write

- Apply what was accepted: managed files replaced, blocks replaced between
  their markers, templates left alone.
- A missing marker in a block target is not repaired silently. Stop, say which
  file and which marker, and ask.
- Record each new deviation **twice**: a row in the *Deviations* table of
  `AGENTS.md`, and an entry in `devkit.lock.json` with the rule slug, the
  scope, the reason and `decidedAt` set to the upstream commit.
- Update `version` in `devkit.lock.json` to the upstream commit and date, and
  refresh the `hash` of every file that was written.

## 6. Report

- what was applied without asking, in one collected diff
- what was decided, and how
- what was deliberately not taken
- any ticket that now needs writing

Then stop. `git status` is the user's to review; the commit is theirs to make.

## Gotchas

- `collect.sh` needs the network. It is not offline-safe, and it fails loudly
  rather than working from a stale clone.
- The clone lands under the system temp directory. Read from it in the same
  session; do not assume it survives.
- The lock records the hash of the **devkit source file**, never of what ended
  up in the repository. For a managed file the two are identical, which is what
  makes local edits detectable. For a block it is the block content alone, not
  the whole target.
- `orphan` in the file list means a file exists under `.claude/skills/` here
  that the devkit no longer ships. It may be a locally added skill, which is
  fine — ask before deleting anything.
- Section slugs are the identity of a rule. If a slug disappeared upstream and
  a deviation points at it, the rule was renamed or dropped: that is always a
  question for the user, never a silent fix.
