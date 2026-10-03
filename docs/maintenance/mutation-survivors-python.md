<!-- Title: Mutation Survivors — Python -->
# Mutation survivors — Python

> The Python package's surviving mutants, each with the proof it is
> behaviorally equivalent rather than a real gap, plus the one thing
> mutmut cannot reach here. The bar a survivor has to clear to be listed
> instead of fixed is in
> [`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things);
> the run itself is
> [`../../scripts/run_mutation_python.sh`](../../scripts/run_mutation_python.sh).

## Score: 224/225 killed (99.6%), one equivalent mutant, zero real gaps

mutmut 3.8.0, against `python/packages/verdict-rules/src/verdict/`:

| Module | Mutants | Survivors |
| :-- | --: | --: |
| `rule.py` | 157 | 1 (equivalent) |
| `engine.py` | 41 | 0 |
| `result.py` | 27 | 0 |
| `__init__.py` | 0 | -- |

`rm -rf python/packages/verdict-rules/mutants/` before re-running after a
source change. A stale cache reports against code that no longer exists and
understates the mutant count, which reads as a clean run rather than a
skipped one.

## The one equivalent mutant

**`SequentialEvaluator.evaluate`'s empty-list branch, with the explicit
`sub_results=()` keyword argument deleted** — mutmut id
`verdict.rule.xǁSequentialEvaluatorǁevaluate__mutmut_7`. The mutant turns

```python
return RuleResult(rule_name=name, passed=self._vacuous_result, sub_results=())
```

into the same call without the keyword, falling back to
`RuleResult.sub_results`'s own declared default: `field(default_factory=tuple)`,
i.e. `()`. `decided_by` is unaffected — neither form sets it, so both take the
identical default. No input to `SequentialEvaluator.evaluate` can make the two
disagree in any observable respect, which is the narrow provable bar for
equivalence rather than a gap.

## What mutation testing does not cover here: `leaves`/`failing_leaves`

`result.py`'s two `__post_init__` methods mutate normally (27 mutants, all
killed, by
[`tests/test_immutability.py`](../../python/packages/verdict-rules/tests/test_immutability.py)).
Its four `@property` accessors — `leaves` and `failing_leaves` on both
`RuleResult` and `RunResult` — generate **zero** mutants, despite containing
real conditional logic (`if not self.sub_results`, `if self.passed`, two
independent recursive comprehensions).

This is a mutmut 3.8.0 limitation, isolated to a minimal reproduction: a
`@property`- or `@functools.cached_property`-decorated method inside a
`@dataclass`-decorated class — frozen or not — produces zero mutants. The
identical body as a `@staticmethod`, or as an undecorated method, mutates
normally; a `@property` in a plain non-dataclass class mutates normally.
mutmut finds the candidate mutations when its generator
(`mutmut.mutation.file_mutation.create_mutations`) is called directly, and
drops them at the next step up (`mutate_file_contents`), which is what a real
run uses.

Nothing here is listed as equivalent, because equivalence requires an actual
mutant to be indistinguishable from the original and there is no mutant to
begin with. The accessors are instead held by direct unit tests —
`TestLeaves`/`TestFailingLeaves` in
[`tests/test_result.py`](../../python/packages/verdict-rules/tests/test_result.py)
— whose adequacy was confirmed the way a survivor would otherwise confirm it:
by reverting each property's logic by hand and watching those tests fail.

`__init__.py` generates zero mutants for the ordinary reason — it re-exports
and holds no logic.

## Related docs

- [`mutation-testing.md`](mutation-testing.md) — why mutation testing, which
  tool per language, what a survivor obliges.
- [`../testing/python.md`](../testing/python.md) — the suite these mutants are
  run against.
