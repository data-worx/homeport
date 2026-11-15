# Review checks — .NET

What a .NET change can get wrong that builds, passes its tests and no rule
document names. Each heading is one check. A finding names the heading it came
from.

## Line endings come from the repository, not the platform

`StringBuilder.AppendLine`, `TextWriter.WriteLine` and `Environment.NewLine`
write `\r\n` on Windows. Where `.gitattributes` forces `eol=lf` on what is
written, a file produced on Windows differs from the same file produced
anywhere else, and a check that compares a generated file with its committed
version fails on one machine and passes on the next. Text that lands in a
tracked file writes `\n` explicitly.

## A child process's output is read without deadlocking

Reading `StandardOutput` to the end before reading `StandardError` blocks for
good once the process fills the error pipe: it waits for a reader, the reader
waits for it to exit. Both streams are read concurrently — the asynchronous
`*DataReceived` events, or `ReadToEndAsync` on both before waiting for exit.

## An MSBuild target hooked to the build runs in design-time builds too

A target attached to `Build` with `AfterTargets` or `BeforeTargets` also runs
when the IDE evaluates the project in the background, on every edit. A target
that writes files, starts a process or reaches the network does that
unannounced and repeatedly. Such a target is conditioned on
`'$(DesignTimeBuild)' != 'true'`, or hooked to a target the design-time build
does not run.
