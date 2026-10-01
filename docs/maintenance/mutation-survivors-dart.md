<!-- Title: Mutation Survivors -- Dart -->
# Mutation survivors -- Dart

> Dart's own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [`mutation_test`](https://pub.dev/packages/mutation_test) found that the
> test suite didn't catch, and what happened to it.

## Current status: none

The most recent run (`dart/packages/verdict_rules/lib/` via
[`scripts/run_mutation_dart.sh`](../../scripts/run_mutation_dart.sh), 40
mutations across the package's four source files) found 5 surviving
mutants, all against `lib/src/engine.dart` and `lib/src/rule.dart`. Every
one turned out to be a real gap, not an equivalent mutant -- each got a
new test that kills it, and a re-run confirmed 40/40 mutations detected
(quality rating A). None were documented here as equivalent.

The 5 survivors and the tests that now kill them:

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `engine.dart` line 62 (x2) | Argument order swapped in `ArgumentError.value(name, 'name', 'No rule with this name')` | `engine_test.dart`: `unknown name error reports value, field, and reason` |
| `engine.dart` line 89 (x2) | Argument order swapped in `ArgumentError.value(group, 'group', 'No rules in this group')` | `engine_test.dart`: `unknown group error reports value, field, and reason` |
| `rule.dart` line 116 | `subResults.add(result);` deleted from `OrRule.evaluate` | `rule_test.dart`: `data carries sub-results up to success` |

The `ArgumentError.value` survivors existed because `throwsArgumentError`
alone proves only the exception's type, not which value landed in which
of the constructor's three positions -- a mutant that swapped two
arguments still throws an `ArgumentError`, so the existing assertion
passed against both the original and the mutant. The new tests catch the
actual value, `name`, and `message` on the thrown exception.

The `OrRule` survivor existed because `AndRule` already had a "data
carries sub-results up to failure" test proving its `data` field is
populated along the way, but `OrRule` had no equivalent for its own
success path -- so deleting the line that populates `subResults` before
an early return went unnoticed.

If a future run finds a mutant genuinely indistinguishable from the
original (no input would make the mutated and original code disagree),
record it here as a new row instead of silently skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies.
