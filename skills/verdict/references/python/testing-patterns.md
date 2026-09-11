# Testing Patterns

What a test for verdict-based logic actually needs to prove, and a
technique for validating a rule-based decision across a much wider
input space than anyone would hand-curate.

## Line coverage isn't the bar — these specific contracts are

A test that only checks the final `passed` boolean can pass while
missing exactly the thing that matters:

- **Short-circuiting**: prove the rule *after* the deciding one was
  never evaluated at all — a call-counter or a mutable list a later
  predicate would have appended to, not just the final boolean. A bug
  that evaluates every sub-rule but still computes the right `passed`
  value passes a weaker, boolean-only test and silently defeats the
  entire reason `AndRule`/`OrRule` short-circuit in the first place.
- **Vacuous truth, explicitly**: `AndRule([])` passes, `OrRule([])`
  fails. Each needs its own dedicated test — "empty means pass" and
  "empty means fail" are both defensible in isolation, and only one is
  correct per shape.
- **Absence, separately from emptiness**: `run_named()` and
  `run_group()` both raise `KeyError` for a name or label nothing
  registered. Test that they raise, not that they return something —
  an unknown lookup returning a pass is the failure mode this
  distinction exists to prevent.
- **Non-short-circuiting run modes, proven not to short-circuit**:
  `run_all`/`run_group` should report *every* rule even after an
  earlier one has already failed — the direct mirror-image regression
  guard from the short-circuit proof above.
- **`data` never gets flattened or padded**: a composite's own `data`
  holds exactly the sub-results that actually ran, nested — never
  merged into the caller's outer result list.

## Testing something built with Extension Recipe 3's adapter pattern

Test the adapter's own translation logic directly (does it build the
right `Rule` shapes from your domain input, does it unpack
`RuleResult.data` back into the right domain type) — you don't need to
re-test verdict's own short-circuit/vacuous-truth behavior inside your
adapter's tests; that's already covered by verdict's own test suite,
assuming you haven't reimplemented anything.

## Differential/oracle testing for a wide random input space

Hand-picked scenario tests prove a handful of cases you already knew
the right answer for. They say nothing about the much larger space of
inputs nobody hand-picked. A technique for that wider space:

1. **A second, independent, verdict-free implementation** — a plain
   loop with ordinary `if`/`and`/`or`, deliberately dumb, so it's
   trustworthy by inspection rather than by being clever. Its only job
   is to compute the same decision a different way, so it can't share a
   bug with the real, `Rule`-based implementation it's checking.
2. **A deterministic generator** — every randomization function takes
   an explicit `random.Random` instance, never the global `random`
   module. Seed one per generated case (e.g.
   `random.Random(SEED + case_index)`, not one shared generator
   advanced across every case) so any single failure is exactly
   reproducible from just its index, with nothing to replay first.
3. **The assertion**: for N generated, schema-valid random cases, the
   real implementation and the oracle must agree. A disagreement is the
   regression signal — there's no fixed expected value to assert
   against for data nobody hand-computed an answer for.

```python
import random

CHAOS_SEED = 20260907  # pinned — bumping this reshuffles coverage, doesn't add to it
NUM_CASES = 500

async def test_engine_agrees_with_independent_oracle(case_index: int) -> None:
    rng = random.Random(CHAOS_SEED + case_index)
    inputs = generate_random_case(rng)          # your own deterministic generator
    actual = await real_verdict_based_decision(inputs)
    expected = oracle_decision(inputs)            # your own verdict-free re-implementation
    assert actual == expected, f"case_index={case_index}: {actual} != {expected}"
```

Keep the seed pinned deliberately — changing it doesn't add coverage,
it trades away whatever edge cases the old seed happened to generate
for a different, unrelated set. Raise `NUM_CASES` instead when more
coverage is actually the goal.
