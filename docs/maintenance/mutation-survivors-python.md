<!-- Title: Mutation Survivors — Python -->
# Mutation survivors — Python

> Per-language survivor list for the Python package, as described in
> [`mutation-testing.md`](mutation-testing.md). Run via
> [`../../scripts/run_mutation_python.sh`](../../scripts/run_mutation_python.sh).

## Current status: none

The most recent run against `python/packages/verdict-rules/src/verdict/`
(mutmut 3.8.0, 108 mutants across `__init__.py`, `engine.py`, `result.py`,
`rule.py`) found 14 survivors, none of which were equivalent. Every one
exposed a real gap — `AndRule`/`OrRule` not asserting `rule_name` and
`data` on results their existing tests only checked `passed` for, the
`AndRule` failure-detail `rstrip` call's exact separator behavior going
unverified, and `RulesEngine.run_named`'s `KeyError` message going
unchecked by its own test (unlike the equivalent `run_group` test, which
already asserted it). All 14 were killed by new tests in
[`../../python/packages/verdict-rules/tests/test_rule.py`](../../python/packages/verdict-rules/tests/test_rule.py)
and
[`../../python/packages/verdict-rules/tests/test_engine.py`](../../python/packages/verdict-rules/tests/test_engine.py).
A re-run after adding them scored 108/108 (100%).

Nothing is listed below as equivalent — no mutant in this run survived a
genuine attempt to distinguish it by input, so there was nothing narrow
enough to document here instead of fix.
