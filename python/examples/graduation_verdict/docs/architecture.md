<!-- Title: Graduation Verdict — Architecture -->
# Graduation Verdict — Architecture

> Why this project is built the way it is — the naive alternative it
> replaces, and the two design decisions (heterogeneous rule shapes
> from one factory, and reusing the same rule objects in two different
> structures) that make it exercise the full breadth of `verdict` at
> once. See [`../README.md`](../README.md) for how to run it and
> [`maintenance.md`](maintenance.md) for how to extend it.

## The naive way (and why it breaks down)

The obvious first implementation is one large function mixing every
subject's own pass condition, the elective count, and the overall
floors:

```python
async def can_graduate(scores: dict, cgpa: float, attendance_pct: float) -> bool:
    if scores["MATH101"]["written_pct"] < 40:
        return False
    if scores["ENG101"]["written_pct"] < 40:
        return False
    if scores["WORKSHOP201"]["written_pct"] < 40 or scores["WORKSHOP201"]["practical_pct"] < 60:
        return False
    if scores["FRENCH101"]["written_pct"] < 40 and not scores["FRENCH101"].get("has_exemption"):
        return False

    elective_ids = ["ELECTIVE_ART", "ELECTIVE_MUSIC", "ELECTIVE_CS"]
    passed_electives = sum(1 for e in elective_ids if scores[e]["written_pct"] >= 40)
    if passed_electives < 2:
        return False

    if cgpa < 6.0:
        return False
    if attendance_pct < 75:
        return False

    return True
```

This tracks a real curriculum for exactly as long as the curriculum
never changes shape. In practice:

- **Every subject is a hand-copied `if`, in whichever shape its type
  happens to need.** Adding a new vocational subject means remembering
  to write the two-threshold `or` pattern correctly by hand, in a
  function that also contains a language subject's completely different
  `and`-with-exemption pattern a few lines above it — nothing stops the
  two shapes from getting silently swapped during a future edit.
- **The elective list and its "how many" threshold are both hard-coded,
  separately from which subjects are even electives.** Moving a subject
  from elective to core, or raising the requirement from 2-of-3 to
  3-of-4, means finding and editing this exact function correctly —
  there's no single place that says "these are this year's electives."
- **The function only ever answers yes or no.** A student asking "did I
  pass Physics," or a registrar needing every subject's own pass/fail
  for a transcript, gets nothing from this function — both would need
  their own, separately-maintained re-derivation of the same logic.

## The verdict way

Each subject's *policy* — its type, its thresholds, whether it's an
elective — comes from `policies.json`; the `Rule` *shape* each policy
turns into is decided once, by type, in one factory function
(`rule_for_subject` in `graduation_verdict.py`):

```mermaid
graph TB
    Policies[("🗄️ policies.json<br/>(type, thresholds, elective flag)")]
    Factory[["🏭 rule_for_subject()"]]
    Academic["✅ academic subject<br/>→ FunctionRule"]
    Vocational{"🔀 vocational subject<br/>→ AndRule<br/>(written AND practical)"}
    Language{"🔀 language subject<br/>→ OrRule<br/>(written OR exemption)"}

    %% Link 0: Policies -> Factory
    Policies -->|"[1]<br/>read per subject"| Factory
    %% Link 1: Factory -> Academic
    Factory -->|"[2]<br/>subject_type == academic"| Academic
    %% Link 2: Factory -> Vocational
    Factory -->|"[3]<br/>subject_type == vocational"| Vocational
    %% Link 3: Factory -> Language
    Factory -->|"[4]<br/>subject_type == language,<br/>exemption allowed"| Language

    style Policies fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Factory fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Academic fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Vocational fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Language fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: policies are read fresh, one per subject, not baked into code
    %% 1: an academic policy becomes a single plain FunctionRule
    %% 2: a vocational policy becomes an AndRule of two FunctionRules
    %% 3: a language policy with an exemption path becomes an OrRule
    linkStyle 0 stroke:#7EDB8F,stroke-width:2px
    linkStyle 1 stroke:#7EDB8F,stroke-width:2px
    linkStyle 2 stroke:#C9B3FF,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
```

> **Reading the Diagram**: three genuinely different `Rule` shapes come
> out of one factory function reading one uniform row shape — nothing
> about `AndRule`/`OrRule`/`FunctionRule` needs to know *why* a
> particular subject picked its shape, and adding a fourth subject
> *type* later (say, a portfolio-reviewed elective) means one more
> branch in `rule_for_subject`, not a new concept anywhere else.

The built rules then serve **two separate purposes from the same
objects** — a diagnostic engine for lookups and reports, and a fast
composite for the actual pass/fail decision:

```mermaid
graph TB
    Rules[("📦 the same built Rule objects")]
    Engine[["⚙️ shared RulesEngine"]]
    RunNamed("🎯 run_named('MATH101')<br/>student portal lookup")
    RunGroup("🗂️ run_group('core')<br/>per-category diagnostic")
    RunAll("📋 run_all()<br/>full registrar transcript")
    Graduates{"🔀 AndRule 'graduates'<br/>core ∧ electives ∧ cgpa ∧ attendance"}
    Verdict("🎓 pass / fail")

    %% Link 0: Rules -> Engine
    Rules -->|"[1]<br/>registered once"| Engine
    %% Link 1: Engine -> RunNamed
    Engine -->|"[2]"| RunNamed
    %% Link 2: Engine -> RunGroup
    Engine -->|"[3]"| RunGroup
    %% Link 3: Engine -> RunAll
    Engine -->|"[4]"| RunAll
    %% Link 4: Rules -> Graduates
    Rules -->|"[5]<br/>reused, not rebuilt"| Graduates
    %% Link 5: Graduates -> Verdict
    Graduates -->|"[6]<br/>fast, short-circuiting"| Verdict

    style Rules fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style Engine fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000
    style RunNamed fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style RunGroup fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style RunAll fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Graduates fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Verdict fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: every built subject rule is registered on one shared engine
    %% 1-3: the engine's three run modes each serve a different real caller
    %% 4: the SAME rule objects are also reused directly inside a composite
    %% 5: the composite is the fast path — it can short-circuit, the engine's own run_* methods never do
    linkStyle 0 stroke:#FFCB7A,stroke-width:2px
    linkStyle 1 stroke:#7EDB8F,stroke-width:2px
    linkStyle 2 stroke:#7EDB8F,stroke-width:2px
    linkStyle 3 stroke:#7EDB8F,stroke-width:2px
    linkStyle 4 stroke:#C9B3FF,stroke-width:2px
    linkStyle 5 stroke:#C9B3FF,stroke-width:3px
```

> **Why Two Structures, Not One**:
> 1. **`run_named`/`run_group`/`run_all` exist to answer questions about
>    individual subjects** — "did I pass X," "how did I do on my core
>    subjects," "give me the full transcript." None of them decide
>    graduation; all of them are read-only lookups a portal or a
>    registrar screen calls directly.
> 2. **`graduates` decides graduation, as cheaply as possible** — a
>    fresh `AndRule` (itself holding a nested `AndRule` for the core
>    subjects and a custom `AtLeastNRule` for electives, alongside the
>    CGPA/attendance checks) built from the *same* rule objects the
>    engine already holds. Nothing is re-evaluated to build it, and
>    nothing about it is retained between calls — see verdict's own
>    [`architecture.md`](../../../../docs/architecture.md#type-structure)
>    for why a `Rule` instance's lifecycle isn't owned by any one
>    structure that references it.
> 3. **The elective requirement is neither `AndRule` nor `OrRule`** —
>    "at least 2 of 3" is exactly verdict's
>    [`extension.md`](../../../../docs/extension.md#recipe-2--a-genuinely-new-rule-shape)
>    Recipe 2 (`ThresholdRule`), named `AtLeastNRule` here and used for
>    something a real curriculum actually needs, not a toy count.

## Where this lives in the code

| Diagram node | `graduation_verdict.py` |
|---|---|
| `policies.json` → `rule_for_subject()` | `load_curriculum()`, `rule_for_subject()` |
| The three `Rule` shapes | `_written_rule()`, `_practical_rule()`, `_exemption_rule()` compose into whichever shape `rule_for_subject()` returns |
| The custom `AtLeastNRule` | `AtLeastNRule` |
| `RulesEngine` + `graduates` built once | `build_graduation_check()` |
| The demo's two halves | `_demo()` (the detailed `alice` walkthrough, then the batch loop) |

See [`testing.md`](testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle)
for `oracle.py` and `chaos_data.py` — a second implementation and a
deterministic case generator that don't appear in this diagram at all,
since they exist to *check* this design against a much wider space of
inputs, not to be part of it.

## Related

- [`../README.md`](../README.md) — how to run this project.
- [`maintenance.md`](maintenance.md) — how to extend it.
- [`testing.md`](testing.md) — how it's tested, and why it also serves
  as a regression net for `verdict` itself.
- [`../../../docs/samples/7_graduation-requirement-verdict.md`](../../../packages/verdict-rules/docs/samples/7_graduation-requirement-verdict.md) —
  the original framing question this project answers.
