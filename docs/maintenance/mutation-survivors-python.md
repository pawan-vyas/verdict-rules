<!-- Title: Mutation Survivors — Python -->
# Mutation survivors — Python

> Per-language survivor list for the Python package, as described in
> [`mutation-testing.md`](mutation-testing.md). Run via
> [`../../scripts/run_mutation_python.sh`](../../scripts/run_mutation_python.sh).

## Current status: one equivalent mutant, zero real gaps

The most recent run against `python/packages/verdict-rules/src/verdict/`
(mutmut 3.8.0, 191 mutants across `__init__.py`, `engine.py`, `result.py`,
`rule.py`) is the first run since the composite-rule-and-leaves redesign —
`RuleResult.sub_results`/`leaves`/`failing_leaves`, `SequentialEvaluator`,
`ShortCircuitEvaluator`, the shipped `NotRule`, `AndRule`/`OrRule` rebuilt on
those evaluators, and `PredicateOutcome`/`RulePredicate` replacing the old
predicate contract. None of that code had ever been through mutation testing
before; the previous 100%/0-survivors result on this page described the old,
pre-redesign `AndRule`/`OrRule`/`FunctionRule`, which no longer exist in that
form.

The initial run found 16 survivors, 15 of which were real gaps:

- `FunctionRule.evaluate` had no test asserting that `PredicateOutcome.data`
  actually reaches the built `RuleResult.data`, and no test pinning the
  *exact* type name in its `TypeError` message (a mutant hardcoding
  `type(None).__name__` survived because every existing predicate-shape test
  happened to use `None`-shaped wrong values).
- `SequentialEvaluator.evaluate`'s post-loop "final decider call" (the one
  made after the main loop exhausts without deciding) had every one of its
  arguments, and the resulting `RuleResult.rule_name`, unverified — every
  existing test's decider ignored its own arguments, so mutating which
  sub-result, which `so_far` list, or which `total` got passed into that
  call changed nothing observable. One mutant even deleted the call outright
  (`final = None`) and survived, because the existing fallback-to-vacuous
  test never gave the decider a chance to decide on that final call.
- The in-loop decider call had its own `total` argument unverified for the
  same reason — no existing decider's behavior depended on the real
  `len(rules)` value.
- `AndRule.failed`/`OrRule.passed` both had an `index -1` vs `index +1` bug
  invisible at every existing fixture's sub-rule count: with exactly two
  sub-rules, `sub_results[-1]` and `sub_results[+1]` are the same element,
  so no 2-rule fixture could ever tell the mutant from the original.
- `NotRule.evaluate` had no test proving `context` actually reaches the
  wrapped rule rather than, say, `None`.

All 15 were killed by new tests in
[`../../python/packages/verdict-rules/tests/test_rule.py`](../../python/packages/verdict-rules/tests/test_rule.py)
(`TestFunctionRule`: `test_type_error_names_the_actual_wrong_type_returned`,
`test_result_carries_the_predicate_s_data`) and
[`../../python/packages/verdict-rules/tests/test_composition.py`](../../python/packages/verdict-rules/tests/test_composition.py)
(`TestSequentialEvaluator`:
`test_in_loop_decider_call_receives_the_real_total`,
`test_final_decider_call_after_exhaustion_sees_the_real_arguments`,
`test_final_decider_call_can_still_resolve_the_outcome`;
`TestAndRuleAccessors`:
`test_failed_returns_the_last_sub_result_not_some_other_index`;
`TestOrRuleAccessors`:
`test_passed_returns_the_last_sub_result_not_some_other_index`;
`TestNotRule`: `test_passes_the_context_through_to_the_inner_rule`). A re-run
after adding them scored 190/191 (99.5%), leaving exactly the one survivor
documented below.

## Documented as equivalent

- **`SequentialEvaluator.evaluate`'s empty-list branch, the explicit
  `sub_results=()` keyword argument deleted** (mutmut's own id:
  `verdict.rule.xǁSequentialEvaluatorǁevaluate__mutmut_7`). The mutant turns
  `RuleResult(rule_name=name, passed=self._vacuous_result, sub_results=())`
  into `RuleResult(rule_name=name, passed=self._vacuous_result, )`, relying
  on `RuleResult.sub_results`'s own declared default —
  `field(default_factory=tuple)`, i.e. `tuple()`, i.e. `()` — instead of
  passing the same value explicitly. The produced `RuleResult` is identical
  in every observable respect (same `sub_results` value, same type, same
  identity-irrelevant empty tuple) whether the keyword argument is present
  or omitted; no input to `SequentialEvaluator.evaluate` could ever make the
  mutated and original code disagree, which is exactly the narrow,
  provable bar [`mutation-testing.md`](mutation-testing.md) sets for
  equivalence rather than a real gap.
