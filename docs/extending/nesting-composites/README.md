<!-- Title: Extending — Nesting Composites Arbitrarily -->
# Extending verdict: nest composites arbitrarily

> Because `AndRule`/`OrRule` satisfy `Rule` themselves, they can hold
> each other as sub-rules to any depth, with no special-casing anywhere.
> Each language's own file in this directory — [`python.md`](python.md)
> today — shows the concrete code.

```mermaid
graph TB
    Qualifies{"🔀 AndRule<br/>'qualifies'"}
    Active["✅ is_active_account"]
    Reason{"🔀 OrRule<br/>'has_a_valid_reason'"}
    Premium["✅ is_premium_member"]
    Promo["✅ has_promo_code"]
    Spend["✅ meets_spend_threshold"]

    %% Link 0: Qualifies -> Active
    Qualifies -->|"[1]<br/>sub-rule"| Active
    %% Link 1: Qualifies -> Reason
    Qualifies -->|"[2]<br/>sub-rule, itself a composite"| Reason
    %% Link 2: Reason -> Premium
    Reason -->|"[3]<br/>sub-rule"| Premium
    %% Link 3: Reason -> Promo
    Reason -->|"[4]<br/>sub-rule"| Promo
    %% Link 4: Reason -> Spend
    Reason -->|"[5]<br/>sub-rule"| Spend

    style Qualifies fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Active fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Reason fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Premium fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Promo fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Spend fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: qualifies' first sub-rule is a plain leaf rule
    %% 1: its second sub-rule is itself a composite — arbitrary nesting, no special-casing
    %% 2-4: the nested OrRule has its own three plain leaf sub-rules
    linkStyle 0 stroke:#7EDB8F,stroke-width:2px
    linkStyle 1 stroke:#C9B3FF,stroke-width:3px
    linkStyle 2 stroke:#7EDB8F,stroke-width:2px
    linkStyle 3 stroke:#7EDB8F,stroke-width:2px
    linkStyle 4 stroke:#7EDB8F,stroke-width:2px
```

> **What Makes This Free**: the purple diamonds (`qualifies`,
> `has_a_valid_reason`) are composites; the green boxes are plain leaf
> rules — but nothing in the tree's shape marks a depth *limit*, and
> nothing holding `qualifies` (a `RulesEngine`, another `AndRule`, a
> direct caller) can tell from the outside that one of its two
> sub-rules is itself a three-rule `OrRule` rather than another leaf.
> That's the whole payoff of `Rule` being a structural type rather than
> a fixed type hierarchy — see
> [`../../architecture/README.md`](../../architecture/README.md#type-structure).

`qualifies` reads exactly like a plain rule to anything holding it — a
`RulesEngine`, another `AndRule`, or a direct call to its own
`evaluate` — since a composite rule is structurally indistinguishable
from a plain one from the outside.

## What this demonstrates

- A composite rule nested inside another composite is structurally
  indistinguishable from a plain leaf rule to whatever holds it.
- Nesting has no built-in depth limit — the tree's shape is entirely a
  consumer decision, not a constraint this package imposes.

## Related

- [`../../architecture/README.md`](../../architecture/README.md#type-structure) —
  why `Rule` being a structural type is what makes nesting free.
- [`../new-rule-shape/README.md`](../new-rule-shape/README.md) — a
  custom combinator nests exactly the same way as `AndRule`/`OrRule` do.
