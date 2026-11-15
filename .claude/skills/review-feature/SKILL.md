---
name: review-feature
description: Review the change on the current branch against its ticket, its request and the repository rules, in a subagent that has not seen the conversation the change was written in. Run only when the user asks for it with /review-feature.
disable-model-invocation: true
context: fork
agent: general-purpose
---

# review-feature

You are reviewing a change you did not write. You have not seen the
conversation it came from, and that is the point: the author's reasons are not
yours to accept. Judge the diff by what it does, not by what it was meant to do.

You report. You change nothing, post nothing, commit nothing. The maintainer
decides what becomes a review comment.

Paths below are relative to the repository root.

## 1. Collect

```bash
bash .claude/skills/review-feature/collect.sh
```

You get the branch, the base, the command for the diff, the files touched, and
— under `WORKFLOW: full` — the ticket and the request body.

- `STOP` → report that line and nothing else.
- `UNCOMMITTED` → review the diff anyway, and say at the top of the report that
  the working tree holds more than was reviewed.
- No ticket or no request → review what there is, and say which was missing.

Then read the diff with the command in the `DIFF` line. Read a whole file
wherever the hunk alone does not show what a change does to its neighbours.

## 2. Read what binds

- The rule documents this repository vendors: `AGENTS.core.md`, every
  `AGENTS.<stack>.md` and `AGENTS.<forge>.md` at the root, and the project
  `AGENTS.md` on top of them.
- The *Deviations* table of `AGENTS.md`. **A recorded deviation is not a
  finding**: where the table says a rule does not apply, it does not.
- Every file in the `checks/` directory next to this file. `core.md` is always
  there; a stack pack adds one of its own. They hold what no rule says — each
  heading is one thing to look for.

## 3. Review

Walk the diff against every rule and every check. A finding needs a concrete
failure: an input, a state or a sequence, and the wrong result it produces. **A
suspicion you cannot turn into one is not a finding — drop it.** Where the
answer depends on what the author intended, it is a question, not a blocker.

Do not run the gates. Their output is in the request's *Evidence* block, and
what they prove is the part this review is not for.

Then walk the ticket's Definition of Done box by box against the diff — not
against the ticks in the request. A box the request ticked that the diff does
not bear out is a blocker.

## 4. Report

The report is relayed to the maintainer as it stands, so write it to be read
without an editor in between. Open with this line, verbatim:

> Review by a subagent that did not see the conversation. Relay it unchanged.

Then the findings, numbered, most severe first:

```
1. [blocker] <path>:<line> — <rule slug or checks/<file>: heading>
   <what is wrong, in one or two sentences>
   Failure: <input or state> → <wrong result>
```

- `blocker` — a wrong result, an obligation left undischarged, a criterion of
  the ticket that does not hold, or a rule broken without a recorded deviation.
- `question` — cannot be judged without knowing the author's intent. Say what
  you would need to know.
- `note` — everything below that.

No findings is a valid result; say so in one line rather than inventing some.

Then the Definition of Done:

```
Definition of Done
- [holds] <criterion, shortened> — <one sentence why>
- [does not hold] <criterion> — <one sentence why>
- [not checkable] <criterion> — <what would be needed to check it>
```

Nothing after that: no summary, no praise, no recommendation to merge.
