<!-- Title: Verdict Maintenance Guide -->
# Verdict — Maintenance Guide

> For people changing *this package's own code* — not for people building
> rules on top of it (see [`extension.md`](extension.md) for that) and not
> for the design rationale behind what's already here (see
> [`architecture.md`](architecture.md) for that). This doc is about
> keeping the package correct and true to its own stated constraints as it
> grows.

## The two constraints that must never quietly slip

Both are called out in the root [`README.md`](../README.md) as
load-bearing, not incidental — a maintainer's first job on any change
is checking it doesn't erode either one:

1. **Zero external dependencies.** `pyproject.toml`'s `dependencies` list
   is empty on purpose. A change that reaches for a third-party package —
   even a small, well-regarded one — is a discussion-worthy exception,
   not a default. If a future need genuinely can't be met without one,
   that's a real design conversation (does it belong in this package at
   all, or in a consumer's own adapter?), not a routine dependency bump.
2. **No knowledge of any specific domain.** Nothing under `src/verdict/`
   should ever import or reference rate limiting, access grants,
   discounts, or any other consumer's vocabulary. Domain-specific logic
   belongs in the *consumer's own* adapter module — see
   [`extension.md`](extension.md#recipe-3--keep-your-own-domain-out-of-verdict-in-one-adapter-module)
   for two living examples of where that vocabulary actually goes. If a
   change to this package only makes sense described in terms of one
   consumer's problem, it's in the wrong file.

## How this package is typically consumed — plan for no release step

If this package is vendored into a consumer's own monorepo rather than
published and versioned, it's commonly wired in as an **editable local
path** dependency (e.g. `uv`'s `[tool.uv.sources]`,
`{ path = "../verdict", editable = true }`) rather than a version
pinned from PyPI or an internal index. That has a real consequence for
how careful a change needs to be whenever it's true:

- There is no "cut a release, consumers upgrade when ready" safety net.
  A change saved to this package is live for every consumer's very next
  process restart — there's no version pin standing between a bug here
  and it reaching production code that depends on this package.
- `pyproject.toml`'s `version` field is documentation, not necessarily a
  gate — check whether anything in your own setup actually reads or
  enforces it. Bump it on a meaningful change regardless (it's the one
  place a future reader can see how far things have drifted from
  `0.1.0`).
- Practically: run this package's own test suite (`uv run pytest`,
  below) *and* grep every consumer codebase you know about for
  `from verdict import` before merging a change to a public type's
  shape — see "Consumer impact checklist" below.

## Where to make a change

| I want to... | Touch this file |
|---|---|
| Add a new concrete `Rule` shape (a new composite, a weighted combinator) | `src/verdict/rule.py` — or a new module if it doesn't naturally fit alongside `FunctionRule`/`AndRule`/`OrRule`; export it from `src/verdict/__init__.py`'s `__all__` either way |
| Change what `RuleResult`/`RunResult` carries | `src/verdict/result.py` — both are frozen dataclasses, so adding a *required* field breaks every construction site in `rule.py` and `engine.py`, and in both real consumers (see checklist below) |
| Add a new `RulesEngine` run mode (a new selection axis beyond "by name"/"by group") | `src/verdict/engine.py` — needs its own index built in `__init__`, the same way `_by_name`/`_by_group` already are |
| Change the `Rule` `Protocol` itself (its required attributes/method signature) | `src/verdict/rule.py` — the highest-blast-radius change this package can make; every existing `Rule` implementation anywhere (including in consumers) must still satisfy the new shape |
| Update *why* something is built this way | `docs/architecture.md` |
| Update the quickstart's concepts/example | `docs/quickstart.md` |
| Update the narrative front door | the root `README.md` — a distinct doc from `docs/quickstart.md`, not a copy of it |

```mermaid
graph TB
    Change{"🔧 What kind of change?"}
    RuleShape["📄 New concrete Rule shape<br/>→ rule.py"]
    ResultShape["📄 RuleResult / RunResult shape<br/>→ result.py"]
    RunMode["📄 New RulesEngine run mode<br/>→ engine.py"]
    ProtocolShape["📄 Rule Protocol itself<br/>→ rule.py"]
    Additive("✅ Additive —<br/>ship it")
    Blast{"⚠️ Blast-radius change?"}
    Checklist[["🔍 Consumer-impact checklist"]]
    Consumers["📦 Every real<br/>production adapter"]

    %% Link 0: Change -> RuleShape
    Change -->|"[1]<br/>a new rule shape"| RuleShape
    %% Link 1: Change -> ResultShape
    Change -->|"[2]<br/>change an existing shape"| ResultShape
    %% Link 2: Change -> RunMode
    Change -->|"[3]<br/>a new run mode"| RunMode
    %% Link 3: Change -> ProtocolShape
    Change -->|"[4]<br/>change the interface"| ProtocolShape
    %% Link 4: RuleShape -> Additive
    RuleShape -->|"[5]<br/>purely additive"| Additive
    %% Link 5: RunMode -> Additive
    RunMode -->|"[6]<br/>purely additive"| Additive
    %% Link 6: ResultShape -> Blast
    ResultShape -->|"[7]<br/>ripples outward"| Blast
    %% Link 7: ProtocolShape -> Blast
    ProtocolShape -->|"[8]<br/>ripples outward"| Blast
    %% Link 8: Blast -> Checklist
    Blast -->|"[9]<br/>before merging"| Checklist
    %% Link 9: Checklist -> Consumers
    Checklist -->|"[10]<br/>grep + re-run both suites"| Consumers

    style Change fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style RuleShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style ResultShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style RunMode fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style ProtocolShape fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style Additive fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000
    style Blast fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Checklist fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Consumers fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000

    %% Link Index:
    %% 0: a new rule shape routes to rule.py
    %% 1: a shape change to RuleResult/RunResult routes to result.py
    %% 2: a new run mode routes to engine.py
    %% 3: a Protocol change routes to rule.py
    %% 4: a new rule shape is purely additive
    %% 5: a new run mode is purely additive
    %% 6: a result-shape change is a blast-radius change
    %% 7: a Protocol change is a blast-radius change
    %% 8: every blast-radius change goes through the checklist first
    %% 9: the checklist means grepping and re-testing both real adapters
    linkStyle 0 stroke:#C9B3FF,stroke-width:2px
    linkStyle 1 stroke:#C9B3FF,stroke-width:2px
    linkStyle 2 stroke:#C9B3FF,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
    linkStyle 4 stroke:#E0E0E0,stroke-width:2px
    linkStyle 5 stroke:#E0E0E0,stroke-width:2px
    linkStyle 6 stroke:#E0E0E0,stroke-width:2px
    linkStyle 7 stroke:#E0E0E0,stroke-width:2px
    linkStyle 8 stroke:#C9B3FF,stroke-width:3px
    linkStyle 9 stroke:#FFCB7A,stroke-width:3px
```

> **Reading the Diagram**:
> 1. **Most changes are additive**: a new concrete `Rule` shape or a new
>    `RulesEngine` run mode never touches an existing consumer's code —
>    `Rule`'s structural typing means nothing needs to know about a new
>    shape in advance for it to work.
> 2. **Two specific changes are not additive**: touching `RuleResult`/
>    `RunResult`'s shape, or the `Rule` `Protocol` itself, ripples into
>    every consumer that already depends on the old shape.
> 3. **The checklist is the gate, not a suggestion**: grep every real
>    adapter you know about and re-run their own test suites before
>    merging either kind of blast-radius change — an editable-path
>    consumption model (see above) means there's often no version pin
>    to catch a mistake here later.

## Consumer-impact checklist for a shape change

Because `Rule` is a structural `Protocol`, most extensions (a new rule
shape, a new composite) are purely additive and can't break an existing
consumer — nothing has to import from this package or subclass anything
to remain a valid `Rule`. The one class of change that *does* ripple
outward is altering something every consumer already depends on the
shape of:

- `RuleResult`'s or `RunResult`'s field names/types.
- `Rule`'s required attributes (`name`, `group`) or its `evaluate`
  signature.

Before merging a change in either category:

1. `grep -rn "from verdict import" --include="*.py" <path to your consumer codebase>` for every consumer you know about, and read every hit.
2. Confirm every real adapter module still constructs/consumes the
   changed type correctly — walk each one found above by hand; see
   [`extension.md`](extension.md#recipe-3--keep-your-own-domain-out-of-verdict-in-one-adapter-module)
   for what a well-formed adapter looks like.
3. Run this package's own test suite, then each consumer's own test
   suite for whatever real adapters exist — a change that's internally
   consistent here can still break a consumer's own assumptions about
   field names it reads out of `RuleResult.data`.
4. Run `uv run pytest examples/graduation_verdict/` too — the one
   "consumer" always available without needing access to anyone else's
   codebase, and broad enough (heterogeneous rule shapes, a custom
   `Rule` type, all three run modes together) to catch an interaction
   bug the narrower checks above might miss. See its own
   [`../examples/graduation_verdict/docs/testing.md`](../examples/graduation_verdict/docs/testing.md)
   for why it plays this role.

## When `architecture.md` needs updating, and when it doesn't

- A new concrete `Rule` shape usually **doesn't** need the class diagram
  updated unless it changes the Composite-pattern shape itself (e.g. a
  combinator that *doesn't* implement `Rule` the same way `AndRule`/
  `OrRule` do, breaking the "arbitrarily deep nesting is free" claim).
- A new `RulesEngine` run mode **does** need the "Three ways to run
  rules" table extended — that table is meant to be exhaustive.
- A change to the execution model (e.g. introducing concurrent sub-rule
  evaluation somewhere) is architecture-doc-worthy by definition — that
  section exists specifically to explain why evaluation is sequential
  today, so reversing that decision anywhere needs its own updated
  rationale, not just a code diff.

## Testing a change

A new `Rule` shape added under "Where to make a change" above needs a
short-circuit-and-vacuous-case test (if it's a composite) or a plain
delegation test (if it isn't) — see [`testing.md`](testing.md) for the
full checklist by change type, the current suite's coverage, and why
line coverage alone doesn't prove the contracts that actually matter
here (short-circuiting, vacuous truth).

## Related docs

- [`../README.md`](../README.md) — the narrative front door.
- [`quickstart.md`](quickstart.md) — core concepts and the one worked example.
- [`architecture.md`](architecture.md) — type structure, execution
  model, and the reasoning behind each design choice.
- [`extension.md`](extension.md) — building on top of this package from
  a consumer's own code, without changing anything here.
- [`testing.md`](testing.md) — the full testing checklist and current
  suite coverage.
- [`samples/`](samples/1_README.md) — worked, domain-flavored examples of
  where a rule engine like this earns its keep.
- [`future_plan.md`](future_plan.md) — exploratory, not-yet-decided
  feature candidates and the test used to evaluate them; read before
  proposing a new core `Rule` shape.
