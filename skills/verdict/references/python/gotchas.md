# Gotchas

Real pitfalls — several found the hard way while building verdict's own
example projects, not hypothetical edge cases.

## Short-circuiting is a behavioral contract, not an optimization to skip verifying

`AndRule`/`OrRule` must not just *return* the right answer — they must
*never evaluate* the rule that comes after the deciding one. A rule
whose predicate has a real side effect (a DB write, an external call,
recording usage) will still fire if you accidentally evaluate every
sub-rule instead of stopping. When testing a composite, prove the
skipped rule was never called at all (a counter, a mutable list a later
predicate would have appended to) — not just that the final `passed`
value came out right. See `testing-patterns.md`.

## Vacuous truth has a polarity, and it's easy to get backwards

`AndRule([])` **passes** (nothing to fail on). `OrRule([])` **fails**
(nothing to pass on). These are not symmetric, and picking the wrong
default for a custom rule shape (see Extension Recipe 2) is a real,
easy mistake — decide it explicitly, don't assume.

## Emptiness is not absence, and they get opposite treatment

An **empty** composite folds to its identity, as above. An **absent**
lookup raises: both `run_named("typo")` and `run_group("typo")` raise
`KeyError`.

The asymmetry is deliberate. An empty rule list is a set the caller
handed over, and "nothing configured" is a legitimate state — Extension
Recipe 4 depends on it. An unknown group is a question about something
that does not exist; since a group exists only because some rule
declared it, a lookup matching nothing can only be a mistake. Returning
a vacuous pass there would mean a misspelled group name silently
approves, which in an access-control or eligibility adapter is the worst
possible failure.

If a group may legitimately be absent, check `engine.group_names` rather
than catching — that is what it is for.

## A `Rule`'s own `.name` and its `RuleResult.rule_name` are two different things

`FunctionRule(name, predicate)` sets `.name` on the `Rule` object
itself — that's what `RulesEngine.run_named()` looks up. But the
predicate is a plain function that constructs and returns its own
`RuleResult`, with its own `rule_name` field, and **nothing enforces
that the two match**. If a predicate is reused in more than one context
(e.g. the same "written score" check used both standalone for an
academic subject and nested inside an `AndRule` for a vocational one),
it's easy to give it a `RuleResult.rule_name` that doesn't match the
`FunctionRule.name` it's currently wrapped in — `run_named("X")` will
still find and evaluate the right rule, but the *result* it hands back
will report a different name than what was looked up, which is
confusing for anyone reading logs or a diagnostic report. If a
predicate's naming needs to vary by context, parameterize the name
explicitly rather than hardcoding it inside the predicate closure.

## Don't conflate the engine's diagnostic run-modes with a fast-verdict composite

`RulesEngine.run_all()`/`run_group()` exist to answer questions about
*individual* rules — they never short-circuit, by design, because their
job is a full picture, not a fast answer. A separate `AndRule`/`OrRule`
composite is what actually decides pass/fail cheaply. Don't rebuild
either the engine or the composite per lookup — build both **once**
from the same underlying `Rule` objects, and reuse them across every
call. Rebuilding per call defeats the "build once, apply many times"
value data-driven rule construction is supposed to provide, and — for
anything reading external configuration — means re-fetching that
configuration far more often than necessary.

## `RuleResult.data` is opaque — verdict never validates its shape for you

Nothing stops a predicate from putting anything in `data`, and nothing
stops two different rules in the same composite from putting
differently-shaped things there. If code downstream needs to walk
`RunResult.results` or a composite's own `data` generically (e.g. to
build a JSON-able report), it has to make its own decision about how to
tell "this `data` is itself a `list[RuleResult]`" (the composite
convention) apart from "this `data` is an opaque domain payload" — a
duck-typed check
(`isinstance(data, list) and all(isinstance(x, RuleResult) for x in data)`)
is the usual answer, but it's a decision your own code has to make,
not something verdict decides for you.

## An empty rule list can silently mean "everyone qualifies" when that's dangerous

`AndRule([])` vacuously passing is the right default for "no configured
requirements means no requirements to fail" — but it's the *wrong*
default for something like an access-grant condition, where "no
condition configured" should mean "nobody matches," not "everybody
does." When building rules from configuration (Extension Recipe 4),
explicitly decide which vacuous-truth polarity is correct for the
domain — don't inherit `AndRule`'s or `OrRule`'s default by accident
when the empty case is a genuinely dangerous one to get wrong.
