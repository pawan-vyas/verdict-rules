<!-- Title: Graduation Verdict — Testing -->
# Graduation Verdict — Testing

> How this project is tested, and why its two test files serve two
> different purposes rather than being one bigger suite of the same
> kind. See [`architecture.md`](architecture.md) for why it's built the
> way it is, and [`maintenance.md`](maintenance.md) for how to extend
> it.

## Two suites, two different jobs

| File | Style | Proves |
|---|---|---|
| `test_graduation_verdict.py` | Curated scenarios | Each subject type builds the right `Rule` shape; `run_named`/`run_group`/`run_all` each behave as documented; all 8 hand-picked, hand-verified students get exactly the verdict their own `expected_passed` says. |
| `test_chaos.py` | Differential/property-based | The real engine agrees with an independent oracle across 500 deterministically-generated, schema-valid random curricula and students — a much wider space than anyone would hand-curate. |

`uv run pytest examples/graduation_verdict/` runs both (547 tests
across the whole package as of this writing — 24 in `verdict`'s own
core suite, 23 curated scenarios here, 500 chaos cases). Also
auto-discovered by the package's own bare `uv run pytest`, no
configuration needed.

## Why this project is a regression net for `verdict` itself, not just a sample

This project isn't only a sample — because its test suite exercises
`FunctionRule`, `AndRule`, `OrRule`, a custom `Rule` shape
(`AtLeastNRule`), and all three `RulesEngine` run modes *together*,
against real, varied data, it functions as an integration/e2e test for
`verdict` itself. It's a different, complementary kind of coverage from
`verdict`'s own `tests/`:

- `verdict`'s own `tests/test_rule.py`/`tests/test_engine.py` prove
  narrow, unit-level contracts in isolation — short-circuit behavior,
  vacuous-truth polarity — each against minimal fixtures built just to
  exercise that one contract. See verdict's own
  [`testing.md`](../../../../docs/testing.md) for the full reasoning.
- This project proves those same primitives compose correctly *together*,
  the way a real consumer's code actually uses them — heterogeneous
  rule shapes built from external data, `Rule` objects shared between
  an engine and a separate composite, a custom rule type nested inside
  a built-in one. A regression that breaks some *interaction* between
  primitives, rather than one primitive's own contract, is exactly the
  class of bug the core suite's narrow, isolated fixtures are least
  likely to catch — and exactly what this project's broader, realistic
  scenario is positioned to catch instead.

```mermaid
graph LR
    Change{"🔧 Change to<br/>src/verdict/"}
    Rerun[["🧪 uv run pytest<br/>examples/graduation_verdict/"]]
    Safe("✅ Still matches every<br/>expected_passed")
    Caught("🚨 Regression caught —<br/>fix before merging")

    %% Link 0: Change -> Rerun
    Change -->|"[1]<br/>before merging"| Rerun
    %% Link 1: Rerun -> Safe
    Rerun -->|"[2]<br/>all pass"| Safe
    %% Link 2: Rerun -> Caught
    Rerun -->|"[3]<br/>any fail"| Caught

    style Change fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Rerun fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000
    style Safe fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Caught fill:#FF6B6B,stroke:#C92A2A,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: any change to verdict's own source is the trigger
    %% 1: a clean run means the change didn't disturb this project's real usage pattern
    %% 2: any failure here is a regression worth fixing before it reaches a real consumer
    linkStyle 0 stroke:#C9B3FF,stroke-width:2px
    linkStyle 1 stroke:#7EDB8F,stroke-width:2px
    linkStyle 2 stroke:#FF9F9F,stroke-width:2px
```

> **Reading the Diagram**: `uv run pytest examples/graduation_verdict/`
> (or the whole package's `uv run pytest`, which auto-discovers this
> project alongside `tests/` with no extra configuration) is the
> concrete action — run it after any change to `src/verdict/`, not just
> a change to this project. This is also cross-linked from verdict's
> own [`maintenance.md`](../../../../docs/maintenance.md#consumer-impact-checklist-for-a-shape-change)
> consumer-impact checklist as one of the things to re-run, alongside
> any external consumer's own suite.

## The chaos suite: differential testing against an independent oracle

`test_graduation_verdict.py` proves 8 hand-picked, hand-verified
scenarios come out right. `test_chaos.py` checks a much wider space,
using a different technique than fixture matching: **differential
testing** against `oracle.py`, a second, deliberately dumb,
`verdict`-free re-implementation of the same decision (the "naive way"
from [`architecture.md`](architecture.md), generalized to score *any*
policy list). If the real engine and the oracle ever disagree on a
generated case, one of them is wrong — that disagreement is the signal,
not a fixed expected value.

```mermaid
graph LR
    Seed[("🎲 CHAOS_SEED + case_index")]
    Gen[["🏭 generate_case()"]]
    Case["📦 (policies, context,<br/>elective_minimum)"]
    Engine{"🔀 build_graduation_check()<br/>→ graduates.evaluate()"}
    Oracle{"🔀 oracle.<br/>expected_graduates()"}
    Compare{"⚖️ Do they agree?"}
    Pass("✅ No breakdown found")
    Fail("🚨 Regression —<br/>reproduce from case_index alone")

    %% Link 0: Seed -> Gen
    Seed -->|"[1]<br/>deterministic"| Gen
    %% Link 1: Gen -> Case
    Gen -->|"[2]<br/>schema-valid, randomized"| Case
    %% Link 2: Case -> Engine
    Case -->|"[3]"| Engine
    %% Link 3: Case -> Oracle
    Case -->|"[4]"| Oracle
    %% Link 4: Engine -> Compare
    Engine -->|"[5]"| Compare
    %% Link 5: Oracle -> Compare
    Oracle -->|"[6]"| Compare
    %% Link 6: Compare -> Pass
    Compare -->|"[7]<br/>match"| Pass
    %% Link 7: Compare -> Fail
    Compare -->|"[8]<br/>mismatch"| Fail

    style Seed fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style Gen fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Case fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Engine fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Oracle fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Compare fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Pass fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Fail fill:#FF6B6B,stroke:#C92A2A,stroke-width:2px,color:#000

    %% Link Index:
    %% 0-1: every case is generated from an explicit, pinned seed, never global random state
    %% 2-3: the same generated case feeds both implementations
    %% 4-5: both verdicts are computed independently
    %% 6-7: agreement is the pass condition; a mismatch is a real, reproducible regression
    linkStyle 0 stroke:#C9B3FF,stroke-width:2px
    linkStyle 1 stroke:#7EDB8F,stroke-width:2px
    linkStyle 2 stroke:#C9B3FF,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
    linkStyle 4 stroke:#C9B3FF,stroke-width:2px
    linkStyle 5 stroke:#C9B3FF,stroke-width:2px
    linkStyle 6 stroke:#7EDB8F,stroke-width:2px
    linkStyle 7 stroke:#FF9F9F,stroke-width:2px
```

> **Why This Needs Two Implementations, Not One Plus Assertions**: for
> the 8 curated students, "what should happen" was known before the
> data was written, so asserting against `expected_passed` works. For
> 500 randomly-generated curricula, nobody hand-computed the right
> answer for each one — there's nothing to assert against *except*
> a second, independent computation. `oracle.py` is that second
> computation: a plain loop with ordinary `if`/`and`/`or`, no `Rule`
> objects anywhere, so it can't share a bug with the thing it's
> checking. Its only job is to be obviously correct by reading it, not
> to be elegant.
>
> **Determinism is the entire point, not an implementation detail**:
> `chaos_data.py`'s functions only ever take an explicit
> `random.Random` instance — never the global `random` module — and
> `test_chaos.py` seeds one per case as `random.Random(CHAOS_SEED +
> case_index)`. "The chaos suite passed" is therefore a real,
> re-checkable claim on every future run, and any single failure
> reproduces on its own from just its `case_index`, with nothing to
> replay first. `CHAOS_SEED` is pinned deliberately — bumping it
> reshuffles every case's data, which trades away coverage rather than
> adding to it; raise `NUM_CASES` instead to test more.

## Running the tests

```bash
# From libraries/python/verdict/
uv run pytest examples/graduation_verdict/
```

## Related

- [`architecture.md`](architecture.md) — why it's built the way it is.
- [`maintenance.md`](maintenance.md) — how to extend it.
- [`../README.md`](../README.md) — how to run the demo.
- [`../../../../docs/maintenance.md`](../../../../docs/maintenance.md) —
  verdict's own maintenance guide, whose consumer-impact checklist
  points back here.
- [`../../../../docs/testing.md`](../../../../docs/testing.md) — verdict's
  own testing guide, which this project complements rather than
  duplicates.
