<!-- Title: Verdict Maintenance Guide -->
# Verdict — Maintenance Guide

> For people changing *this package's own code* — not for people building
> rules on top of it (see [`../extending/`](../extending/README.md) for that) and
> not for the design rationale behind what's already here (see
> [`../architecture/`](../architecture/README.md) for that). This directory is
> about keeping the package correct and true to its own stated constraints
> as it grows, split into one focused doc per concern rather than one long
> page, so fetching what a given change needs doesn't mean pulling in
> everything else too. Files are named for what they cover, not numbered —
> there's no real order among them, and a name doesn't need renumbering
> every time a doc is split or merged.

## Where to make a change

Named by what the module *is*, not by one language's path — every SDK has
all four, and each language's own `AGENTS.md` carries its real layout
([Python](../../python/AGENTS.md#layout),
[JS/TS](../../js/AGENTS.md#layout),
[Dart](../../dart/AGENTS.md#layout),
[C#](../../csharp/AGENTS.md#layout)). A fifth language needs no edit here.

| I want to... | Touch | Ripples? |
| --- | --- | --- |
| Add a concrete rule shape (a new composite, a weighted combinator) | The rule module, or a new module beside it; export it from that language's own public surface | No — purely additive |
| Add a run mode (a selection axis beyond by-name/by-group) | The engine module; it needs its own index built at construction, the way the name and group indexes already are | No — purely additive |
| Change what a result carries | The result module | **Yes** — see [`before-merging-checklists.md`](before-merging-checklists.md) |
| Change the rule contract itself (required members, the evaluate signature) | The rule module — the highest-blast-radius change this package can make | **Yes** — every rule implementation anywhere, including a consumer's own |
| Change *why* something is built this way | [`../architecture/`](../architecture/README.md) | — |
| Change the concepts or worked example a newcomer reads | That language's own quickstart, and the [root `README.md`](../../README.md) if the landing example changes — two distinct docs, not copies | — |

A change in either **Yes** row also needs the public-API snapshot
regenerated and, if the concept is shared, a row in the concept map —
see [`api-snapshots.md`](api-snapshots.md).

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
    Consumers["📦 Every real<br/>consumer adapter"]

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
    Checklist -->|"[10]<br/>re-run every suite"| Consumers

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
    %% 9: the checklist means finding and re-testing every consumer's adapter
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
>
> 1. **Most changes are additive**: a new concrete `Rule` shape or a new
>    `RulesEngine` run mode never touches an existing consumer's code —
>    `Rule`'s structural typing means nothing needs to know about a new
>    shape in advance for it to work.
> 2. **Two specific changes are not additive**: touching `RuleResult`/
>    `RunResult`'s shape, or the `Rule` `Protocol` itself, ripples into
>    every consumer that already depends on the old shape.
> 3. **The checklist is the gate, not a suggestion**: find every real
>    adapter you know about and re-run its own test suite before merging
>    either kind of blast-radius change. A consumer vendoring this
>    package by local path (see
>    [`before-merging-checklists.md`](before-merging-checklists.md)) has
>    no version pin to catch a mistake here later.

### When `architecture/` needs updating, and when it doesn't

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

## The rest of this directory

| Doc | Covers |
| --- | --- |
| [`constraints.md`](constraints.md) | The two constraints that must never quietly slip |
| [`adding-a-language.md`](adding-a-language.md) | The one-time ritual for bringing a new language SDK from nothing to a first real release |
| [`adding-a-fixture.md`](adding-a-fixture.md) | The template for a new cross-language parity fixture, if the need arises |
| [`releases/`](releases/README.md) | The shared release pipeline, and each target's own concrete release procedure |
| [`packages-and-changelogs.md`](packages-and-changelogs.md) | Why a package is a directory under `packages/`, and the per-ecosystem workspace layout |
| [`versioned-links.md`](versioned-links.md) | Why links in shipped content are pinned to a version, never to `main` |
| [`supply-chain-and-ownership.md`](supply-chain-and-ownership.md) | Provenance per registry, and how ownership/namespaces are handled |
| [`import-name-and-second-distribution.md`](import-name-and-second-distribution.md) | The Python import-name decision, and what a second distribution or a promoted [`extending/`](../extending/README.md) scenario would look like |
| [`before-merging-checklists.md`](before-merging-checklists.md) | The consumer-impact checklist for a shape change, and what a change needs tested |
| [`discoverability-metadata.md`](discoverability-metadata.md) | Why every package's keywords/topics/tags stay in sync across registries, and pub.dev's one real structural exception |
| [`api-snapshots.md`](api-snapshots.md) | The committed public-API surface per package, and the cross-language concept map that catches drift between them |
| [`mutation-testing.md`](mutation-testing.md) | Why line coverage proves little, which tool each language uses, and what a surviving mutant obliges. Each language's own survivor list sits beside it |
| [`doc-authoring/`](doc-authoring/README.md) | The standard every doc in this repo follows, plus one template per doc category built on top of it |

## A doc category that will grow per-variant is a directory, not a flat file family

`releases/` is the pattern: [`releases/README.md`](releases/README.md)
holds only what's identical across every release target, and each
target — the skill, a language's own package registry — gets its
**own** file next to it
([`releases/verdict-agent-skill.md`](releases/verdict-agent-skill.md),
[`releases/python.md`](releases/python.md), and a future
`releases/<language>.md` per language). Adding a target is a new file
in that directory; nothing already there is edited to make room for it.
A bare directory README also renders automatically on GitHub, so
linking to the directory itself (`releases/`) already shows the right
landing page.

[`supply-chain-and-ownership.md`](supply-chain-and-ownership.md) follows
the same rule in reverse, one file at a time: a registry's detailed
provenance paragraph lives there
only until that language ships and gets its own `releases/<language>.md`,
at which point it moves — PyPI's already has, into
[`releases/python.md`](releases/python.md).

The same test decides it for any future doc, not just these two: if a
second variant showing up would mean editing a file the first variant
already owns, that doc is a directory-with-README, not a flat file.

## Related docs

- [`../../README.md`](../../README.md) — the narrative front door.
- [`../../python/packages/verdict-rules/docs/quickstart.md`](../../python/packages/verdict-rules/docs/quickstart.md) —
  core concepts and the one worked example.
- [`../architecture/`](../architecture/README.md) — type structure, execution
  model, and the reasoning behind each design choice.
- [`../extending/`](../extending/README.md) — building on top of this
  package from a consumer's own code, without changing anything here.
- [`../testing/`](../testing/README.md) — the full testing checklist.
- [`../../fixtures/README.md`](../../fixtures/README.md) —
  worked, domain-flavored examples of where a rule engine like this
  earns its keep.
- [`../future_plan.md`](../future_plan.md) — exploratory, not-yet-decided
  feature candidates and the test used to evaluate them; read before
  proposing a new core `Rule` shape.
