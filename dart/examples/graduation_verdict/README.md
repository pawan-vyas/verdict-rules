<!-- Title: Graduation Verdict Example -->
# Graduation Requirement Verdict

> A college graduation-eligibility check, built as real, tested code
> instead of a doc snippet -- the flagship example exercising the full
> breadth of verdict_rules at once: heterogeneous `Rule` shapes built
> from external policy data, a custom `Rule` type, and all three
> `RulesEngine` run modes serving three different real callers. See
> [`docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From dart/examples/graduation_verdict/
dart pub get
dart run bin/graduation_verdict.dart
```

Prints a detailed lookup for one student, then a batch verdict for all
8 -- one engine and one `graduates` composite, built once, applied to
every record.

## Test it

```bash
# From dart/examples/graduation_verdict/
dart test
```

This is its own standalone package (its own `pubspec.yaml`, depending
on `verdict_rules` via a `path:` dependency) rather than a member of a
pub workspace -- see `dart/AGENTS.md` for why there is deliberately no
root `pubspec.yaml` yet.

## Read more

- [`../../../docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md) --
  the naive way this policy is usually implemented, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/graduation_verdict/README.md`](../../../fixtures/graduation_verdict/README.md) --
  how to add a subject, a student scenario, or a new subject type, and
  the shared cross-language fixture contract.
- [`docs/testing.md`](docs/testing.md) -- the two test suites and what
  each proves, including why this project's own tests also serve as an
  integration/e2e regression net for verdict_rules itself.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/graduation_verdict/policies.json`](../../../fixtures/graduation_verdict/policies.json) | The curriculum -- the elective-count threshold plus one row per subject, no code. **Shared across every language.** |
| [`../../../fixtures/graduation_verdict/students.json`](../../../fixtures/graduation_verdict/students.json) | 8 varied students, each carrying its own expected outcome -- including how many rules should run, which proves short-circuiting. **Shared.** |
| [`../../../fixtures/graduation_verdict/edge_cases.json`](../../../fixtures/graduation_verdict/edge_cases.json) | Degenerate curricula proving vacuous-truth polarity. **Shared.** |
| `lib/src/subject_policy.dart` | The per-subject policy class, read from `policies.json`. |
| `lib/src/at_least_n_rule.dart` | The custom `Rule` shape for the elective threshold. |
| `lib/src/graduation_check.dart` | The real implementation -- rule factory, JSON loading, and `buildGraduationCheck`. |
| `lib/src/oracle.dart` | A second, verdict_rules-free implementation, used as ground truth by the chaos suite. |
| `lib/src/chaos_data.dart` | A deterministic generator for randomized, schema-valid curricula and students, using `dart:math`'s own seedable `Random`. |
| `bin/graduation_verdict.dart` | The runnable demo. |
| `test/graduation_verdict_test.dart` | The curated-scenario suite -- loads both JSON files, asserts generically. |
| `test/chaos_test.dart` | 500 generated cases, checked against `oracle.dart` -- see [`docs/testing.md`](docs/testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle). |
