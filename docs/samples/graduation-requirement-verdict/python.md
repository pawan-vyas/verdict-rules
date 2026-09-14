<!-- Title: Sample — Graduation Requirement Verdict -->
# Sample: Graduation Requirement Verdict

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page points at the Python implementation of it, which is
> a full tested project rather than a markdown code block.

See [`../../../python/examples/graduation_verdict/`](../../../python/examples/graduation_verdict/README.md)
for how to run it, how to test it, and the full file listing. Its own
[`docs/testing.md`](../../../python/examples/graduation_verdict/docs/testing.md)
covers the two test suites and why this project's own test suite also
serves as an integration/e2e regression net for `verdict` itself.

## Where the design lives in the code

| Spec concept | `graduation_verdict.py` |
| --- | --- |
| Policies → the factory function | `load_curriculum()`, `rule_for_subject()` |
| The three rule shapes | `_written_rule()`, `_practical_rule()`, `_exemption_rule()` compose into whichever shape `rule_for_subject()` returns |
| The custom at-least-N rule shape | `AtLeastNRule` |
| The engine and the AND composite, built once | `build_graduation_check()` |
| The demo's two halves | `_demo()` (the detailed `alice` walkthrough, then the batch loop) |

**Adding a subject of a genuinely new type** — not just a new row of an
existing type — needs a new branch in `rule_for_subject()`, a real code
change, since a new *kind* of pass condition is a new concept, not new
data. Every other curriculum change (a threshold, a new subject of an
existing type, a student scenario, the elective-count minimum) is a
data-only edit to the shared fixture — see
[`../../../fixtures/graduation_verdict/README.md`](../../../fixtures/graduation_verdict/README.md).

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../../../python/examples/graduation_verdict/`](../../../python/examples/graduation_verdict/README.md) —
  the full project: implementation, tests, and a runnable demo across 8
  varied students.
- [`../dynamic-discounts/python.md`](../dynamic-discounts/python.md) —
  a smaller `AndRule`-from-config example without the heterogeneous-shape
  or dual-structure elements this sample adds.
- [`../data-driven-rule-sets/python.md`](../data-driven-rule-sets/python.md) —
  the simpler version of "build rules from stored config," with one
  uniform rule shape per row instead of three.
- [`../../extending/new-rule-shape/`](../../extending/new-rule-shape/README.md) —
  the `ThresholdRule`/`AtLeastNRule` scenario this sample's elective
  requirement is a real instance of.
- [`../../architecture/python.md`](../../architecture/python.md#three-ways-to-run-rules-concretely) —
  the general reasoning behind reaching for `run_named`/`run_group`/
  `run_all` vs. a bare composite, applied here to three concrete
  callers at once.
