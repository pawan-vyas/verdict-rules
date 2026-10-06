<!-- Title: Extending — Walking a Rule Tree -->
# Extending verdict: walking a rule tree

> A composite exposes the rules it was built from, before anything is
> evaluated. That makes a rule *tree* walkable — listing every named rule a
> decision could consult, checking for duplicate names, resolving each one
> against your own registry — all without running it.

The result surface answers *what ran*. This answers *what was built*, and the
two genuinely differ, because a composite short-circuits: a rule that never
got evaluated is absent from the result and present in the tree.

The capability is a contract separate from `Rule` — a composite satisfies
both, a leaf only `Rule`. That separation is the whole design, and the reason
is the trap below.

## The trap: a type switch silently under-reports

The obvious walk tests for the three built-in composites:

```text
if rule is AndRule  -> recurse into its parts
if rule is OrRule   -> recurse into its parts
if rule is NotRule  -> recurse into its one part
otherwise           -> it is a leaf
```

That is wrong the moment anyone nests a fourth kind of composite — a
threshold rule from
[`new-rule-shape/`](../new-rule-shape/README.md), an adapter from
[`reusing-a-rule-across-contexts/`](../reusing-a-rule-across-contexts/README.md),
or your own. The switch falls through to "it is a leaf", so every rule inside
that subtree is reported as **absent**. Nothing throws. A registry lookup
silently resolves fewer rules than the decision will actually consult, and a
duplicate-name check silently misses the duplicates.

```mermaid
graph TB
    Top{"🔀 AndRule<br/>top"}
    Leaf1("✅ rule_a")
    Custom{"🔀 your own composite"}
    Leaf2("❓ rule_b")
    Leaf3("❓ rule_c")
    Switch[/"⚠️ type switch<br/>no case matches"/]
    Reported("🚨 reports: rule_a only")

    %% Link 0: Top -> Leaf1
    Top -->|"[1]<br/>matched, recursed"| Leaf1
    %% Link 1: Top -> Custom
    Top -->|"[2]<br/>reached"| Custom
    %% Link 2: Custom -> Switch
    Custom -->|"[3]<br/>no case"| Switch
    %% Link 3: Switch -> Reported
    Switch -.->|"[4]<br/>treated as a leaf"| Reported
    %% Link 4: Custom -> Leaf2
    Custom -.->|"[5]<br/>never visited"| Leaf2
    %% Link 5: Custom -> Leaf3
    Custom -.->|"[6]<br/>never visited"| Leaf3

    style Top fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Custom fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Leaf1 fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style Leaf2 fill:#D0D0D0,stroke:#909090,stroke-width:2px,color:#000
    style Leaf3 fill:#D0D0D0,stroke:#909090,stroke-width:2px,color:#000
    style Switch fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Reported fill:#FF6B6B,stroke:#C92A2A,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: a built-in composite matches a case, so the walk recurses correctly
    %% 1-3: a composite with no case falls through and is treated as terminal
    %% 4-5: everything inside it is never visited, and reported as absent
    linkStyle 0 stroke:#A9E8B5,stroke-width:2px
    linkStyle 1 stroke:#C9B3FF,stroke-width:2px
    linkStyle 2 stroke:#FFCC80,stroke-width:3px
    linkStyle 3 stroke:#FF9999,stroke-width:3px,stroke-dasharray:3 3
    linkStyle 4 stroke:#B0B0B0,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 5 stroke:#B0B0B0,stroke-width:2px,stroke-dasharray:3 3
```

> **Reading the Diagram**: the walk is correct for everything the switch has
> a case for, which is exactly why the bug survives review — it works on
> every tree built only from built-ins. The failure needs a *fourth* kind of
> composite to appear, which is normal the moment anyone follows
> [`new-rule-shape/`](../new-rule-shape/README.md). Testing for the contract
> instead has no cases to be missing: a composite is anything that says it
> has parts.

## The fix: test for the contract

One capability test replaces the switch, and the walk reaches every composite
— including ones verdict has never seen, because yours satisfies the same
contract with no registration:

- **Python** — [`python.md`](python.md)
- **JS/TS** — [`js.md`](js.md)
- **Dart** — [`dart.md`](dart.md)
- **C#** — [`csharp.md`](csharp.md)

## What this demonstrates

- A composite's parts are readable without evaluating it, so the shape of a
  decision is inspectable before any predicate runs.
- A **leaf deliberately does not** satisfy the contract, so "structure or
  terminal check" never needs a concrete type.
- The member is **uniform**: a negation reports a one-element list rather
  than a differently-named single rule, so a walk needs no knowledge of which
  composite it is holding. A vacuous composite reports an empty list, which
  is a composite with no parts, not a leaf.
- Your own composite joins in by satisfying the contract. Nothing registers,
  and verdict does not need to know the type exists — the same property that
  makes `Rule` itself work.

## What this does not give you

**Rebuilding a composite over new parts.** Reading parts is total; rebuilding
is partial, because a composite may hold state that is not in its parts —
`new-rule-shape/`'s own threshold rule carries a minimum alongside its
sub-rules, and "the same composite over new parts" is not something a contract
can promise on its behalf. If you need to substitute a rule inside a tree,
rebuild the composites whose construction *you* know and treat anything else
as an error rather than guessing: that fails loudly instead of quietly
dropping a composite's own configuration.

## Related

- [`new-rule-shape/`](../new-rule-shape/README.md) — writing the kind of
  composite that makes the type-switch trap above real.
- [`nesting-composites/`](../nesting-composites/README.md) — the nesting this
  walks over.
- [`../../architecture/README.md`](../../architecture/README.md#type-structure)
  — why a structural contract is what every part of this package dispatches
  on.
