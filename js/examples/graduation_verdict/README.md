<!-- Title: Graduation Verdict Example -->
# Graduation Requirement Verdict

> A college graduation-eligibility check, built as real, tested code
> instead of a doc snippet -- the flagship example exercising the full
> breadth of verdict-rules at once: heterogeneous `Rule` shapes built
> from external policy data, a custom `Rule` type, and all three
> `RulesEngine` run modes serving three different real callers. See
> [`docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md)
> for the original framing question this project answers, the design,
> and what a solution must demonstrate.

## Run it

```bash
# From js/
npm install
npm run build --workspace=verdict-rules
node examples/graduation_verdict/graduation-verdict.js
```

Prints a detailed lookup for one student, then a batch verdict for all
8 -- one engine and one `graduates` composite, built once, applied to
every record.

## Test it

```bash
# From js/
npm test --workspace=@verdict-rules/example-graduation-verdict
```

Also picked up by the workspace root's own `npm test`, alongside
`verdict-rules`' own suite -- no separate invocation is required day to
day.

## Read more

- [`../../../docs/samples/graduation-requirement-verdict/`](../../../docs/samples/graduation-requirement-verdict/README.md) --
  the naive way this policy is usually implemented, why it breaks down,
  and both diagrams behind the design actually used here.
- [`../../../fixtures/graduation_verdict/README.md`](../../../fixtures/graduation_verdict/README.md) --
  how to add a subject, a student scenario, or a new subject type, and
  the shared cross-language fixture contract.
- [`docs/testing.md`](docs/testing.md) -- the two test suites and what
  each proves, including why this project's own tests also serve as an
  integration/e2e regression net for verdict-rules itself.

## Files

| File | What it is |
| --- | --- |
| [`../../../fixtures/graduation_verdict/policies.json`](../../../fixtures/graduation_verdict/policies.json) | The curriculum -- the elective-count threshold plus one row per subject, no code. **Shared across every language.** |
| [`../../../fixtures/graduation_verdict/students.json`](../../../fixtures/graduation_verdict/students.json) | 8 varied students, each carrying its own expected outcome -- including how many rules should run, which proves short-circuiting. **Shared.** |
| [`../../../fixtures/graduation_verdict/edge_cases.json`](../../../fixtures/graduation_verdict/edge_cases.json) | Degenerate curricula proving vacuous-truth polarity. **Shared.** |
| `graduation-verdict.js` | The real implementation, plus a runnable demo. |
| `test/graduation-verdict.test.js` | The curated-scenario suite -- loads both JSON files, asserts generically. |
| `oracle.js` | A second, verdict-rules-free implementation, used as ground truth by the chaos suite. |
| `chaos-data.js` | A deterministic generator for randomized, schema-valid curricula and students. |
| `rng.js` | A small seeded pseudo-random generator, since `Math.random()` cannot be seeded. |
| `test/chaos.test.js` | 500 generated cases, checked against `oracle.js` -- see [`docs/testing.md`](docs/testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle). |
