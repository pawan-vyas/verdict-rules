<!-- Title: Mutation Survivors -- Dart -->
# Mutation survivors -- Dart

> Dart's own per-language record required by
> [`mutation-testing.md`](mutation-testing.md) -- every mutant
> [`mutation_test`](https://pub.dev/packages/mutation_test) found that the
> test suite didn't catch, and what happened to it.

## Current status: zero survivors, two real gaps found and killed

This is Dart's **first** mutation run since the composite-rule-and-leaves
redesign (`SequentialEvaluator`/`ShortCircuitEvaluator`/`NotRule`/
`PredicateOutcome`/`RuleResult.decidedBy` -- see
[`.agents/plans/composite-rule-and-leaves-redesign/README.md`](../../.agents/plans/composite-rule-and-leaves-redesign/README.md)
§3-§3e) -- the prior version of this doc described the pre-redesign code (40
mutations, 5 survivors, all against `ArgumentError.value` argument order and
one `OrRule` `subResults` gap) and has been replaced in full, not amended.

Starting from a clean cache (`mutation-test-report/` and any
`.dart_tool/mutation_test*` state removed before both the first run and the
re-run below, the same discipline Python's mutmut run required this
session), the run (`dart/packages/verdict_rules/lib/` via
[`scripts/run_mutation_dart.sh`](../../scripts/run_mutation_dart.sh),
`mutation_test` 1.8.1, 53 mutations across the package's four source files --
up from 40 before `decidedBy`/`SequentialEvaluator`/`ShortCircuitEvaluator`/
`NotRule` existed) found exactly **one** surviving mutant on the first pass.
It was a real gap; a new test closes it, and a re-run confirmed 53/53
mutations detected (quality rating A). No mutant in this run was equivalent,
so there is no equivalence section below -- see the note at the end for what
that would take.

A second gap -- real, but structurally invisible to this tool rather than a
survivor -- is also documented below, the same way Python's record documents
mutmut's own blind spot on `result.py`.

## The one surviving mutant, and the test that now kills it

| Survivor | Mutation | Killed by |
| :-- | :-- | :-- |
| `rule.dart` line 169 -- `SequentialEvaluator.evaluate`'s in-loop early-decision ternary, `soFar.length == rules.length ? ... : ...` | `==` replaced with `!=` | `composition_test.dart`: `decided in-loop with items left unevaluated names only the deciding sub-result` and `decided in-loop on exactly the last sub-rule names every evaluated sub-result` |

### Why it existed

`AndRule`/`OrRule` are the only shipped composites, and both route through
`ShortCircuitEvaluator`, which always *recomputes and overwrites*
`decidedBy` from its own `_stopOn` after delegating to `SequentialEvaluator`
(`rule.dart` lines 234-245) -- so no `AndRule`/`OrRule` test, however
thorough, can ever observe `SequentialEvaluator`'s own generic `decidedBy`
computation at line 169; it is always discarded before reaching a public
result. Only a test instantiating `SequentialEvaluator` directly (the
composable, custom-composite extension point it is designed to be) can
exercise it, and the existing `SequentialEvaluator` group in
`composition_test.dart` had four tests but none that read `decidedBy` at
all -- they checked `passed`/`subResults`/call order only.

The existing "a decider returning a value stops evaluation immediately"
test does decide early (`soFar.length == 1` of 3), but a one-element
`soFar` is indistinguishable from `[latest]` either way, so even adding a
`decidedBy` assertion to it could not have killed this mutant. Two new
tests were needed instead, matching exactly what the sibling C#/Python/JS
mutation re-runs each found in their own equivalent code: deciding with
items still unevaluated (`soFar.length == 2` of 3 -- `decidedBy` must name
just the one sub-result that flipped it, not both seen so far) and deciding
in-loop but landing exactly on the last item (`soFar.length == 3 == total`
-- indistinguishable from the post-loop fallback by count alone, but
reached via the in-loop branch and required to produce the same "every
evaluated sub-result" answer as a genuine exhaustion would).

## A real gap this tool structurally cannot surface: a leaf result's `decidedBy` default

`RuleResult`'s `decidedBy` default (`this.decidedBy = const []` in the
constructor, `result.dart` line 51) had never been read on a bare leaf
result anywhere in the suite before this run -- every existing `decidedBy`
test exercised a *composite's* result (`AndRule`/`OrRule`/`NotRule`), never
a plain `FunctionRule` leaf's. This is the identical gap the C#, Python, and
JS mutation re-runs each found independently in their own leaf type.

Unlike those three languages, `mutation_test` cannot generate a mutant for
this line at all, so the gap never appeared as a survivor to chase --
confirmed by inspecting the tool's own builtin rule set (`dart run
mutation_test -g`). Its one list-mutating rule,
`builtin.list.clear` (pattern `([=:>]\s*)\[([^\],]+),([^\]]+)\]`, requiring
at least one comma between two captured elements), only rewrites a
list literal with **two or more** elements down to `[]`; an already-empty
`const []` default has no comma to match and is structurally exempt. The
tool is a plain regex-based text mutator (its own description: mutations
are "simple text replacements with regular expressions"), not an
AST-aware one -- it also has no ternary-swap rule at all, and its `if`-
negation rule (`builtin.if`) only matches an `if` whose condition is
followed by `{`, so every brace-less `if (cond) return ...;` in this
package (several in `rule.dart` and `result.dart`) is invisible to it
too. This is why this run's overall mutation count (53) stays
proportionally low next to the three-digit counts StrykerJS/Stryker.NET/
mutmut produce against comparable source -- a narrower operator set, not a
cleaner codebase.

A test was added anyway, mirroring Python's
`test_a_leaf_result_names_nothing_in_decided_by`: `rule_test.dart`'s
`FunctionRule` group now has `a leaf result names nothing in decidedBy`,
evaluating both a passing and a failing bare `FunctionRule` and asserting
`decidedBy` is empty in both directions. The fact is worth pinning with a
direct unit test even though this run's tool can't independently confirm
the pin by mutating the line it protects -- the same standard Python's
record applied to its own `result.py` blind spot.

## Why the C#/Python coverage-attribution artifact class doesn't apply here

C#'s and Python's mutation re-runs each found a mutant that survived only
because their tool's *coverage-based test selection* misattributed a
shared, lazily-initialized singleton's own construction line to whichever
test happened to import/touch it first, rather than to the tests that
actually depend on its behavior. Two things keep that class of artifact out
of this run entirely:

- `scripts/run_mutation_dart.sh` invokes `mutation_test` with no `-c`/
  `--coverage` flag, so every mutant here is validated against the *entire*
  `dart test` suite, never a coverage-narrowed subset -- there is no
  attribution step that could misattribute anything.
- `AndRule`/`OrRule` each hold their `ShortCircuitEvaluator` as an
  **instance field**, not a shared static one (`rule.dart` lines 275-285,
  with the reasoning spelled out there: Dart does not allow a generic
  class's static member to reference that class's own type parameter, so
  there is no single process-lifetime singleton for a coverage pass to
  misattribute in the first place).

If a future version of `run_mutation_dart.sh` adopts `-c` for speed, this
section should be re-examined rather than assumed to still hold.

## Equivalent mutants

None found this run. If a future run finds a mutant genuinely
indistinguishable from the original (no input would make the mutated and
original code disagree), record it here as a new row instead of silently
skipping it -- see
[`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things)
for what qualifies.
