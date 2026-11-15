# Review checks — core

What a change can get wrong in any stack, and no rule document says. Each
heading is one check. A finding names the heading it came from.

## The change does what its own text claims

Names, comments, the header of a generated file, the request description and
the changelog entry all make claims. Hold each against what the code does.

A generated file that says it reproduces licence texts and contains only their
identifiers has left the obligation it exists for undischarged — and the gate
that checks the file is there is green.

## A fallback does not invent data

Where a value is missing, the change fails, or it says that the value is
unknown. It does not fill the gap with something plausible built from a
neighbouring field.

A copyright line derived from a package's author field is a notice nobody
published. It looks exactly like the real ones.

## Fail open and fail closed are chosen, and consistent

Name every path in the diff that handles something missing or broken, and which
way it goes: stop, or carry on. Two such paths side by side that go different
ways for the same kind of gap are a finding. So is a path that carries on where
an obligation depends on the result.

## A gate stays local, fast and deterministic

A pre-commit gate does not reach the network, publish, or build a release. A
gate that does is slow enough to be skipped, and fails for reasons that have
nothing to do with the change.

## One problem, one mechanism

The change does not add a second way of solving something the repository
already solves. A build does not rewrite a tracked file as a side effect: the
working tree is dirty after every build, and the diff stops meaning anything.
