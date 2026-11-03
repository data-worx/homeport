# Testing

Why Homeport's tests are arranged the way they are. The binding rules are
in the *Tests* section of [`AGENTS.md`](../../AGENTS.md#tests).

A test suite that fails now and then is worse than a smaller one that never
does: a red run stops meaning anything, and a real failure gets re-run until
it turns green. Every rule below comes from a test that hung or failed
intermittently.

## Clocks

Tests that depend on time — a heartbeat, a poll interval, a cache lifetime —
run on a `FakeTimeProvider` they advance, so they take milliseconds rather
than the real interval.

That clock goes to the component under test, never into the container as
*the* `TimeProvider`. Everything else in the host takes its clock from the
container: the standard resilience handler that
`Homeport.Shared.ServiceDefaults` adds to every HTTP client measures attempt
timeouts, retry delays and the wait for in-flight calls on disposal on it.
On a clock the test advances in large steps, an attempt still in flight
timed out, its retry then waited on a clock nobody advanced again, and the
host hung on disposal (#111).

What a fake clock cannot reach — a TTL inside a Valkey container — is
asserted by asking the store, not by waiting the time out.

## Waiting

An assertion on something asynchronous waits for the thing itself: a log
entry, a request at a stand-in, a status code. It polls with a short
interval and a timeout, or awaits a signal. A fixed `Task.Delay` is either
too long, and every run pays for it, or too short on a loaded CI runner.

## Stand-ins

A stand-in for another component — a registry, a control plane, an identity
provider — records a request once its response is complete. A test that
waits for "the stand-in saw the request" and then tears everything down
otherwise races the response still being written, and the caller's retry
outlives the test.

## Stopping hosts

Disposing a running host does not stop it gracefully: hosted services are
not asked to stop, and what they await is cancelled from under them. Every
test host is stopped first. `WebApplicationFactory` does this itself on
disposal; a `WebApplication` a test starts directly is stopped by the test.

## End-to-end tests

The AppHost tests start the whole topology in containers, which takes long
and has the most moving parts. They therefore cover only what nothing else
can: that every resource starts healthy, that `migrate` completes before the
control plane, that only the gateway (and the development identity provider)
is exposed, and that the components are wired to each other — a request with
a token from the realm reaches Echo through the gateway, and a client of the
realm the control plane does not know is refused.

How the gateway strips headers, propagates traces, keeps its routes while
the registry is down or drops a module whose instance expired is tested in
the gateway's and registry's own tests, where a stand-in replaces the other
side and the test controls time. One stack is started for the whole run.
