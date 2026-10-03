<!-- Title: Mutation Survivors -- Dart -->
# Mutation survivors -- Dart

> Dart's surviving mutants, and the much larger story here: how narrow
> [`mutation_test`](https://pub.dev/packages/mutation_test)'s own operator set
> is, so a clean score is read for what it actually proves. The bar a survivor
> has to clear to be listed instead of fixed is in
> [`mutation-testing.md`](mutation-testing.md#a-surviving-mutant-needs-one-of-two-things);
> the run itself is
> [`../../scripts/run_mutation_dart.sh`](../../scripts/run_mutation_dart.sh).

## Score: 57/57 detected (100%), zero survivors

`mutation_test` 1.8.1 against
[`dart/packages/verdict_rules/lib/`](../../dart/packages/verdict_rules/lib/),
through the 133-test suite. Quality rating A, no timeouts, nothing
uncovered.

**Read that number next to this one: 57 mutations, where mutmut generates 242
and Stryker.NET 176 against comparable source.** That gap is the tool's
operator set, not a cleaner codebase -- see the limitations below before
treating 100% here as a stronger result than C#'s 98.9%.

Remove `mutation-test-report/` and any `.dart_tool/mutation_test*` state
before a re-run, the same discipline the other three languages need.

## What this tool structurally cannot mutate

`mutation_test` is a regex-based text mutator by its own description
("mutations are simple text replacements with regular expressions"), not an
AST-aware one. Inspecting its builtin rule set (`dart run mutation_test -g`)
shows three exemptions that matter for this package specifically:

| Construct | Why it is exempt |
| :-- | :-- |
| An empty list default (`decidedByIndices = const []`) | The one list rule, `builtin.list.clear`, has pattern `([=:>]\s*)\[([^\],]+),([^\]]+)\]` -- it needs at least one comma between two captured elements, so it only collapses a literal of two or more. An already-empty default has no comma to match. |
| Any ternary | There is no ternary-swap rule at all. `decidedBy`'s index mapping, `leaves`'s leaf-or-recurse choice and every `? :` in the evaluators are untouched. |
| A brace-less `if` | `builtin.if` has pattern `[\s]if[\s]*\((.*?)\)[\s]*{` -- it requires a `{`, so every `if (cond) return ...;` in `rule.dart` and `result.dart` is invisible. |

So mutation testing does not verify the derived accessors in this language
at all -- `leaves`, `failingLeaves` and `decidedBy` are each a single
ternary or a brace-less guard plus a comprehension. Nothing here is listed
as equivalent for them, because equivalence requires an actual mutant to be
indistinguishable and no mutant is generated to begin with.

Those accessors are instead held by direct unit tests in
[`test/result_test.dart`](../../dart/packages/verdict_rules/test/result_test.dart)
and
[`test/serialization_test.dart`](../../dart/packages/verdict_rules/test/serialization_test.dart),
and by the differential suite in
[`dart/examples/graduation_verdict/`](../../dart/examples/graduation_verdict/README.md),
which checks 1,271 cases against a `verdict_rules`-free re-implementation.
That oracle is the real guard on this language's derived views, and it is
worth knowing that rather than reading the A rating as covering them.

## Where `SequentialEvaluator` needs a direct test, not a composite one

A standing trap rather than a past survivor: `AndRule`/`OrRule` both route
through `ShortCircuitEvaluator`, which **recomputes and overwrites**
`decidedByIndices` from its own `stopOn` after delegating to
`SequentialEvaluator`. No `AndRule`/`OrRule` test, however thorough, can
observe `SequentialEvaluator`'s own generic computation -- it is always
discarded before reaching a public result.

Only a test instantiating `SequentialEvaluator` directly can exercise it,
and two cases are needed rather than one, because a single early decision
cannot tell them apart:

- **Deciding with items still unevaluated** -- two of three evaluated, so
  "the one that flipped it" and "everything seen so far" genuinely differ. A
  one-element `soFar` is indistinguishable from `[latest]` either way, which
  is why an existing early-decision test could not cover this by having a
  `decidedBy` assertion added to it.
- **Deciding in-loop but landing exactly on the last item** -- identical to
  a genuine exhaustion by count alone, reached by the other branch, and
  required to produce the same answer.

Both live in the `SequentialEvaluator` group in
[`test/composition_test.dart`](../../dart/packages/verdict_rules/test/composition_test.dart).
The sibling C#, Python and JS runs each found the same gap independently in
their own equivalent code.

## Why the coverage-attribution artifact class does not apply here

C#'s run has a mutant that survives only because coverage-based test
selection misattributes a shared static evaluator's construction line (see
[`mutation-survivors-csharp.md`](mutation-survivors-csharp.md)). Two things
keep that out of this run:

- [`run_mutation_dart.sh`](../../scripts/run_mutation_dart.sh) passes no
  `-c`/`--coverage` flag, so every mutant is validated against the entire
  `dart test` suite rather than a coverage-narrowed subset. There is no
  attribution step to get wrong.
- `AndRule`/`OrRule` each hold their `ShortCircuitEvaluator` as an
  **instance** field, not a shared static one -- Dart does not allow a
  generic class's static member to reference that class's own type
  parameter, so there is no process-lifetime singleton to misattribute.

If a future version of the script adopts `-c` for speed, re-examine this
rather than assuming it still holds.

## Equivalent mutants

None. If a future run finds one genuinely indistinguishable from the
original, record it here rather than skipping it.

## Related docs

- [`mutation-testing.md`](mutation-testing.md) -- why mutation testing, which
  tool per language, what a survivor obliges.
- [`../testing/dart.md`](../testing/dart.md) -- the suite these mutants are
  run against.
