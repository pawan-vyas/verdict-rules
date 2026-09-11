<!-- Title: Graduation Verdict Example -->
# Graduation Requirement Verdict

> A college graduation-eligibility check, built as real, tested code
> instead of a doc snippet — the flagship example exercising the full
> breadth of `verdict` at once: heterogeneous `Rule` shapes built from
> external policy data, a custom `Rule` type, and all three
> `RulesEngine` run modes serving three different real callers. See
> [`docs/samples/7_graduation-requirement-verdict.md`](../../docs/samples/7_graduation-requirement-verdict.md)
> for the original framing question this project answers.

## Run it

```bash
# From python/
uv run python examples/graduation_verdict/graduation_verdict.py
```

Prints a detailed lookup for one student, then a batch verdict for all
8 — one engine and one `graduates` composite, built once, applied to
every record.

## Test it

```bash
# From python/
uv run pytest examples/graduation_verdict/
```

Auto-discovered by the package's own `uv run pytest` too — no separate
invocation is required day to day.

## Read more

- [`docs/architecture.md`](docs/architecture.md) — the naive way this
  policy is usually implemented, why it breaks down, and both diagrams
  behind the design actually used here.
- [`docs/maintenance.md`](docs/maintenance.md) — how to add a subject,
  a student scenario, or a new subject type.
- [`docs/testing.md`](docs/testing.md) — the two test suites and what
  each proves, including why this project's own tests also serve as an
  integration/e2e regression net for `verdict` itself.

## Files

| File | What it is |
|---|---|
| [`../../../fixtures/graduation_verdict/policies.json`](../../../fixtures/graduation_verdict/policies.json) | The curriculum — the elective-count threshold plus one row per subject, no code. **Shared across every language.** |
| [`../../../fixtures/graduation_verdict/students.json`](../../../fixtures/graduation_verdict/students.json) | 8 varied students, each carrying its own expected outcome — including how many rules should run, which proves short-circuiting. **Shared.** |
| [`../../../fixtures/graduation_verdict/edge_cases.json`](../../../fixtures/graduation_verdict/edge_cases.json) | Degenerate curricula proving vacuous-truth polarity. **Shared.** |
| `graduation_verdict.py` | The real implementation, plus a runnable `__main__` demo. |
| `test_graduation_verdict.py` | The curated-scenario suite — loads both JSON files, asserts generically. |
| `oracle.py` | A second, `verdict`-free implementation, used as ground truth by the chaos suite. |
| `chaos_data.py` | A deterministic generator for randomized, schema-valid curricula and students. |
| `test_chaos.py` | 500 generated cases, checked against `oracle.py` — see [`docs/testing.md`](docs/testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle). |
