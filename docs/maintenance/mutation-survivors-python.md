<!-- Title: Mutation Survivors — Python -->
# Mutation survivors — Python

> Per-language survivor list for the Python package, as described in
> [`mutation-testing.md`](mutation-testing.md). Run via
> [`../../scripts/run_mutation_python.sh`](../../scripts/run_mutation_python.sh).

## Current status: one equivalent mutant, zero real gaps

The most recent run (mutmut 3.8.0, 195 mutants across `src/verdict/`) is the
first since [`RuleResult.decided_by`](../../python/packages/verdict-rules/src/verdict/result.py)
was added and the three static accessor pairs it replaced
(`AndRule.failed`/`AndRule.passing`, `OrRule.passed`/`OrRule.failing`,
`NotRule.negated`) were removed along with their tests — see
[`src/verdict/rule.py`](../../python/packages/verdict-rules/src/verdict/rule.py)
for the design. None of `decided_by`'s own producing code —
`SequentialEvaluator.evaluate`'s generic rule, `ShortCircuitEvaluator`'s
`stop_on`-aware override — had ever been through mutation testing before this
run; the 190/191 (99.5%) result this page previously described predates
`decided_by` entirely.

Starting from a clean cache (the previous cache predated this run's code
changes enough that re-running against it understated the true mutant count —
always `rm -rf python/packages/verdict-rules/mutants/` before trusting a
re-run's totals after a significant source change), the initial run found 10
survivors, 9 of which were real gaps, all in `rule.py`:

- **`SequentialEvaluator.evaluate`'s in-loop early-decision branch** — the
  line computing `decided_by` when `decider` resolves before every sub-rule
  has run (`decided_by = tuple(so_far) if len(so_far) == len(rules) else
  (latest,)`) and the `return` passing it into the built `RuleResult` — had
  no test asserting on `decided_by` at all; every existing
  `TestSequentialEvaluator` test checked `passed`/`sub_results`/call order
  only. Six independent mutants survived here: forcing `decided_by = None`,
  always taking one branch of the ternary (`and False` / `or True`), flipping
  `==` to `!=`, and dropping the `decided_by=decided_by` keyword from the
  `return` entirely (falling back to the dataclass field's own `()`
  default). Two new tests close this, matching both sub-cases the design doc
  calls out: deciding with an item still unevaluated (`decided_by` names just
  the one sub-result that flipped the verdict) and deciding in-loop but
  landing exactly on the last item (`decided_by` names every evaluated
  child, same as a genuine full pass) — the first attempt at the
  "items-left" test decided on the very first sub-rule, which survived one
  mutant (`or True`) anyway because a one-element `so_far` is
  indistinguishable from `(latest,)` on its own; deciding on the *second* of
  three sub-rules instead (so `so_far` and `(latest,)` actually diverge) was
  what it took to kill it.
- **`SequentialEvaluator.evaluate`'s post-loop fallback** had the same gap on
  its own `decided_by` — `test_final_decider_call_after_exhaustion_sees_the_real_arguments`
  pinned every other argument of that final call but never read
  `result.decided_by`, so a mutant forcing it to `None` or dropping the
  keyword survived. One added assertion on the existing test closes it.
- **`ShortCircuitEvaluator.__init__`** had `self._stop_on = stop_on` mutated
  to `self._stop_on = None`, which survived even though it's a real,
  observable bug (confirmed by reverting the mutation in `TestDecidedBy`
  directly: three existing `AndRule`/`OrRule`-level tests fail against it).
  The reason it survived mutmut specifically, not a gap in what the suite
  proves: `AndRule`/`OrRule` each hold a single class-level
  `ShortCircuitEvaluator` instance, constructed once at module import —
  before any individual test runs — so mutmut's coverage-based test
  selection attributes `__init__`'s own line to whichever test happens to
  trigger that import first, not to the `TestDecidedBy` tests that actually
  exercise the resulting behavior. No existing `TestShortCircuitEvaluator`
  test constructs its own evaluator *and* asserts on `decided_by` — every one
  of them only checks `passed`/`sub_results`. Two new tests do both directly
  (`ShortCircuitEvaluator(stop_on=...)` built fresh inside the test, with a
  `decided_by` assertion), which both fixes the real coverage gap and gives
  mutmut's test-selection a test it can correctly attribute `__init__` to.

All 9 were killed by new tests in
[`../../python/packages/verdict-rules/tests/test_composition.py`](../../python/packages/verdict-rules/tests/test_composition.py)
(`TestSequentialEvaluator`:
`test_decider_returning_a_value_stops_evaluation_immediately` (extended),
`test_in_loop_early_decision_landing_on_the_last_item_names_every_evaluated_sub_result`,
`test_final_decider_call_after_exhaustion_sees_the_real_arguments` (extended);
`TestShortCircuitEvaluator`:
`test_decided_by_names_only_the_trigger_when_found_before_exhaustion`,
`test_decided_by_names_every_sub_result_when_exhausted_without_ever_triggering`).
A re-run after adding them scored 194/195 (99.5%), leaving exactly the one
survivor documented below — the same one this page documented before, on the
same line, re-verified against the current code rather than assumed to still
hold.

A tenth survivor from the initial run (`FunctionRule`'s bare leaf result
never having anything read its `decided_by`) turned out not to be something
mutmut could surface at all — see the tooling-limitation note below for why,
and for why a test was still added for it regardless.

## Checked against C#'s own re-run and not assumed to match

A sibling re-run against the C# package (same `decided_by` redesign, same
starting point) found two gaps: `RuleResult`'s `decided_by` default on a bare
leaf result, and `SequentialEvaluator`'s in-loop early-decision branch
specifically. Python's own run was done independently rather than assumed to
match, and landed close but not identical:

- **The in-loop early-decision gap matches** — found independently here too,
  same root cause (no test ever asserted `decided_by` on that code path), see
  above.
- **The bare-leaf `decided_by` default gap is real in the sense that nothing
  tested it, but mutmut cannot surface it as a survivor in Python at all** —
  see the next section. A test was added anyway
  (`test_a_leaf_result_names_nothing_in_decided_by` in
  [`../../python/packages/verdict-rules/tests/test_rule.py`](../../python/packages/verdict-rules/tests/test_rule.py)'s
  `TestFunctionRule`), because the fact is worth pinning with a unit test
  even though this run's tool can't independently confirm the pin by mutating
  the line it protects.
- **Python additionally found a gap C#'s run didn't call out**: the
  `ShortCircuitEvaluator.__init__`/class-level-singleton interaction above.
  This is plausibly specific to mutmut's coverage-based test selection
  combined with Python's module-level-singleton idiom
  (`AndRule`/`OrRule`'s shared `_evaluator` class attribute); Stryker.NET's
  own test-selection strategy may not have the same blind spot, or C#'s
  equivalent code may not construct its shared evaluator at a time coverage
  attributes away from the tests that exercise it. Confirmed as a real,
  independently-reproducible gap here either way — reverting the mutation by
  hand fails three pre-existing tests, with or without mutmut's own opinion
  about who gets credit for killing it.

## A confirmed mutmut limitation, not a code or test gap: `result.py` generates zero mutants

`result.py` — `RuleResult`/`RunResult`, including the new `decided_by`
field and the `leaves`/`failing_leaves` properties — contributed **zero**
mutants to every run above, despite containing real conditional logic
(`if not self.sub_results`, `if self.passed`, two independent recursive list
comprehensions). This was confirmed to be a limitation of mutmut 3.8.0
itself, not a cache artifact or a project misconfiguration:

- Calling mutmut's own mutation-generation function
  (`mutmut.mutation.file_mutation.create_mutations`) directly against
  `result.py`'s source finds 9 raw candidate mutations.
- Calling the next step up (`mutate_file_contents`, what the real run
  actually uses) against the identical source drops all 9 to zero.
- Isolated down to a minimal reproduction: a `@property`-decorated method
  (or a `@functools.cached_property`-decorated one) inside a
  `@dataclass`-decorated class — frozen or not — produces zero mutants from
  mutmut 3.8.0. The identical method body, decorated with `@staticmethod`
  instead, or left as a plain method with no decorator, mutates normally. A
  `@property` in a *plain* (non-dataclass) class also mutates normally. Both
  `RuleResult.leaves` and `RuleResult.failing_leaves` are `@property` methods
  on a `@dataclass(frozen=True)` class, so they hit this directly; a dataclass
  field default assigned in the class body (not inside `__init__`), like
  `decided_by: Sequence[RuleResult] = field(default_factory=tuple)`, hits a
  related version of the same gap — mutmut finds the candidate mutation but
  has no containing function to wrap it in a trampoline, since a class-body
  statement isn't one.
- `__init__.py` also shows zero mutants, but for the ordinary reason — it is
  a pure re-export module with no logic of its own, unrelated to the
  `result.py` finding.

This means mutation testing cannot currently verify any of `result.py`'s own
logic in this language, including the exact case the C# re-run flagged (a
bare leaf's `decided_by` default). That code is still covered by direct unit
tests — `TestLeaves`/`TestFailingLeaves` in
[`../../python/packages/verdict-rules/tests/test_result.py`](../../python/packages/verdict-rules/tests/test_result.py),
and the new `test_a_leaf_result_names_nothing_in_decided_by` in
`test_rule.py` — verified by manual inspection and by deliberately reverting
each property's logic by hand and confirming the existing tests fail, the
same proof-of-adequacy standard a surviving mutant would otherwise supply.
Nothing here is listed as "equivalent" per
[`mutation-testing.md`](mutation-testing.md)'s own bar, because equivalence
requires an actual mutant to be indistinguishable from the original — there
is no mutant to begin with.

## Documented as equivalent

- **`SequentialEvaluator.evaluate`'s empty-list branch, the explicit
  `sub_results=()` keyword argument deleted** (mutmut's own id:
  `verdict.rule.xǁSequentialEvaluatorǁevaluate__mutmut_7`, unchanged since
  the previous run — re-verified against the current code, not assumed to
  still apply). The mutant turns
  `RuleResult(rule_name=name, passed=self._vacuous_result, sub_results=())`
  into `RuleResult(rule_name=name, passed=self._vacuous_result, )`, relying
  on `RuleResult.sub_results`'s own declared default —
  `field(default_factory=tuple)`, i.e. `tuple()`, i.e. `()` — instead of
  passing the same value explicitly. `decided_by` is unaffected either way
  (neither branch sets it, so both also fall back to the identical `()`
  default). The produced `RuleResult` is identical in every observable
  respect whether the keyword argument is present or omitted; no input to
  `SequentialEvaluator.evaluate` could ever make the mutated and original
  code disagree, which is exactly the narrow, provable bar
  [`mutation-testing.md`](mutation-testing.md) sets for equivalence rather
  than a real gap.
