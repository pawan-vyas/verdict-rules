<!-- Title: Extending — Isolating A Flaky Predicate -->
# Extending verdict: stop one flaky predicate from taking out the whole run

> Worth stating plainly, because it is a fact a consumer has no way to
> arrive at except by being told. Each language's own file in this
> directory — [`python.md`](python.md) today — shows the concrete code.

**Nothing in this package catches an exception a predicate raises.** Not
`AndRule`/`OrRule`, not a run-everything mode, not a named or grouped
lookup. If a predicate's own code raises — an HTTP call to a
promo-validation service timing out, a database lookup failing — that
exception propagates straight out of whichever call you made, exactly
as if you'd called the failing code yourself with nothing in between.

This is worth a scenario of its own, and not just a line in
[`../../architecture/README.md`](../../architecture/README.md), because
"rules *engine*" invites the opposite assumption. A consumer reaching
for this package is not reading its source to find out what it does with
a predicate's exception — verdict is something you install and call, not
something you read line-by-line before trusting — so the natural guess
is that an *engine* is the fault-tolerant layer, the thing that keeps a
batch of twenty independent checks going even if one of them breaks. It
isn't, deliberately, and the cost of that assumption being wrong is
severe: one flaky external call takes out every other rule's diagnostics
in the same run, not just its own.

If that's not what you want, wrap the predicate so its own exception
becomes a failing result instead of propagating out of the run that
contains it. Now a timeout in the wrapped check reports as a normal
failing entry, same as any other failing rule — and every other rule in
that run still runs and still reports.

## Why the library does not catch this for you

Because whether a flaky external check failing should count as "the
condition failed" or should stop everything and surface the exception is
a call only the rule's author can make, and the two answers are both
right somewhere:

- **A promo-code service timing out** probably should report as
  "not valid" and let checkout continue — the customer just doesn't get
  that discount this time.
- **A database connection failing inside an access-control check**
  probably should *not* quietly report "access denied" — that's a system
  fault, not a policy decision, and swallowing it would misreport an
  outage as a legitimate rejection.

A default either way is wrong for one of those. Catching every exception
unconditionally would also mean this package deciding it doesn't need to
care whether what it just caught was a legitimate domain outcome or a
genuine code bug — and those are not the same thing. A discount
calculation that divides by a quantity of `0` is not "this rule failed,"
it's a bug in the predicate. Catch it unconditionally and it *becomes*
"this rule failed" — a silent, wrong result sitting in the run's own
results, looking exactly like every other legitimate rejection. Let it
propagate and it's a stack trace pointing at the exact line that's
wrong, in front of the developer who can fix it, the moment it happens.
Silent-and-wrong beats loud-and-obvious for no one. That is the same
failure mode [`../absence-vs-failure/README.md`](../absence-vs-failure/README.md)
already rejects a built-in fallback for: a library default that is right
for some consumers is wrong, silently, for the rest — so the choice
stays with whoever wrote the predicate, opted into per rule, not assumed
for all of them.

## What this demonstrates

- A predicate's own exception propagates unchanged; nothing in this
  package's own code intercepts it.
- Turning a predicate's exception into a failing result is a consumer
  decision, opted into per rule that needs it, never a blanket default.
- Isolating one flaky check from the rest of a run is possible without
  any change on this package's side.

## Related

- [`../../architecture/README.md`](../../architecture/README.md) —
  the execution-model guarantees this scenario's exception behavior sits
  alongside.
- [`../absence-vs-failure/README.md`](../absence-vs-failure/README.md) —
  the same "the library will not guess for you" reasoning, applied to a
  missing lookup instead of a predicate's own exception.
