<!-- Title: Before Merging — Consumer Impact and Testing -->
# Before merging: consumer impact and testing

> What a shape change has to prove before it merges, and what a new
> `Rule` shape needs tested regardless of whether it's a shape change.

## Consumer-impact checklist for a shape change

Because `Rule` is a structural `Protocol`, most extensions (a new rule
shape, a new composite) are purely additive and can't break an existing
consumer — nothing has to import from this package or subclass anything
to remain a valid `Rule`. The one class of change that *does* ripple
outward is altering something every consumer already depends on the
shape of:

- `RuleResult`'s or `RunResult`'s field names/types.
- `Rule`'s required attributes (`name`, `group`) or its `evaluate`
  signature.

Before merging a change in either category:

1. `grep -rn "from verdict import" --include="*.py" <path to your consumer codebase>` for every consumer you know about, and read every hit.
2. Confirm every real adapter module still constructs/consumes the
   changed type correctly — walk each one found above by hand; see
   [`../extending/domain-adapter-module/`](../extending/domain-adapter-module/README.md)
   for what a well-formed adapter looks like.
3. Run this package's own test suite, then each consumer's own test
   suite for whatever real adapters exist — a change that's internally
   consistent here can still break a consumer's own assumptions about
   field names it reads out of `RuleResult.data`.
4. Run `uv run pytest examples/graduation_verdict/` (from `python/`) too
   — the one "consumer" always available without needing access to
   anyone else's codebase, and broad enough (heterogeneous rule shapes,
   a custom `Rule` type, all three run modes together) to catch an
   interaction bug the narrower checks above might miss. See its own
   [`../../python/examples/graduation_verdict/docs/testing.md`](../../python/examples/graduation_verdict/docs/testing.md)
   for why it plays this role.

A consumer can also choose to vendor this package via an editable local
path instead of a normal PyPI dependency (e.g. inside their own
monorepo, before it's ready to depend on a public release). That
carries a sharper version of the same risk: no version pin at all
between a change here and that consumer's next process restart, so the
grep-and-test-suite discipline above matters even more in that setup,
not less.

## Testing a change

A new `Rule` shape added under
[`README.md`](README.md)'s "Where to make a change" table needs a
short-circuit-and-vacuous-case test (if it's a composite) or a plain
delegation test (if it isn't) — see [`../testing.md`](../testing.md) for the
full checklist by change type, the current suite's coverage, and why
line coverage alone doesn't prove the contracts that actually matter
here (short-circuiting, vacuous truth).
