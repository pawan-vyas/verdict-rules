<!-- Title: Sample Spec — Loyalty Tier Promotion -->
# Sample spec: Loyalty Tier Promotion

> What this scenario is, and what any language's worked implementation
> of it must demonstrate — language-agnostic, read once here rather
> than restated per language.

**The question**: should this customer be promoted to the next loyalty
tier?

**Why it's a good fit**: promotion requires *every* one of several
independent criteria (trailing-12-month spend, order count, a low
return rate, an account in good standing) — a natural AND — but the
customer-facing "your progress toward Gold" screen needs to show which
criteria are already met and which aren't, not just a final yes/no.
That need is exactly what running every rule unconditionally is for, as
distinct from a composite's own short-circuiting evaluate.

## What the naive approach gets wrong

The obvious first implementation ends up as *two* functions that have
to be kept in sync by hand — one for the yes/no decision, one for the
customer-facing checklist:

```text
function gold_checklist(customer):
    return {
        "spend": customer.trailing_12mo_spend >= customer.gold_spend_threshold,
        "orders": customer.trailing_12mo_orders >= customer.gold_order_threshold,
        "returns": customer.return_rate <= customer.gold_max_return_rate,
        "standing": customer.account_status == "active",
    }

function is_eligible_for_gold(customer):
    checklist = gold_checklist(customer)
    return all(checklist.values())
```

The coupling between the two functions is enforced by nothing except a
developer remembering it exists:

- **A new criterion means editing two places, not one.** Adding a fifth
  requirement to `is_eligible_for_gold` without also adding it to
  `gold_checklist` produces a promotion decision the UI's own checklist
  can't explain — a customer who "shouldn't" be eligible sees a screen
  showing all checks passed.
- **Nothing catches the two functions drifting apart.** There's no
  compiler error, no test failure by default — just a UI that quietly
  stops matching the real decision the day someone edits only one of
  the two functions.
- **The pattern gets worse with every additional consumer.** A third
  place that needs "which criteria are met" (an internal ops dashboard,
  an email template) is a third function to keep in sync, or a
  reference back to one of the first two that's easy to get wrong.

## The `verdict` way

The four criteria are registered as **independent named rules on one
engine — not nested inside an AND composite** — so that running every
rule unconditionally reports each criterion's own outcome, with no
short-circuiting hiding a later criterion's result. A short-circuiting
AND built from the same rule objects still exists, for whichever call
site wants the fast boolean gate instead — both read from the same
four rule objects, never two independently maintained implementations.

Each criterion is independently unit-testable this way too: proving
`return_rate_below_max` fails needs only that one rule's own inputs,
never the other three's — the naive version's two hand-written
functions have no such isolation, since both would need every field on
`customer` populated just to exercise one check.

```mermaid
graph TB
    Combined{"🔀 AndRule<br/>'eligible_for_gold'"}
    Spend["✅ meets_spend_threshold"]
    Orders["✅ meets_order_count"]
    Returns["❌ return_rate_below_max"]
    Standing["⏭️ account_in_good_standing"]
    Engine[["⚙️ RulesEngine.run_all()"]]
    UI("📋 Progress checklist —<br/>3 of 4 met")

    %% Link 0: Spend -> Combined
    Spend -->|"[1]<br/>sub-rule"| Combined
    %% Link 1: Orders -> Combined
    Orders -->|"[2]<br/>sub-rule"| Combined
    %% Link 2: Returns -> Combined
    Returns -->|"[3]<br/>sub-rule"| Combined
    %% Link 3: Standing -> Combined
    Standing -->|"[4]<br/>sub-rule"| Combined
    %% Link 4: Combined -> Engine
    Combined -->|"[5]<br/>as one named rule"| Engine
    %% Link 5: Engine -> UI
    Engine -->|"[6]<br/>full per-criterion result"| UI

    style Spend fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Orders fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Returns fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Standing fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Combined fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Engine fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000
    style UI fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0-3: each criterion is a sub-rule of the AndRule
    %% 4: the composite is handed to the engine as one named rule
    %% 5: run_all surfaces every criterion's own outcome, not just the composite's
    linkStyle 0 stroke:#C9B3FF,stroke-width:2px
    linkStyle 1 stroke:#C9B3FF,stroke-width:2px
    linkStyle 2 stroke:#C9B3FF,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
    linkStyle 4 stroke:#C9B3FF,stroke-width:3px
    linkStyle 5 stroke:#FFCB7A,stroke-width:3px
```

> **Why the checklist needs run-everything, not the composite's own
> evaluate**:
>
> 1. **A short-circuiting evaluate alone would stop at `Returns`** — the
>    diagram shows a customer meeting the first two criteria, failing
>    the third; a plain short-circuiting call would never even reach
>    `Standing`, so its own result would be unknown to the caller.
> 2. **Running every rule the engine holds, unconditionally, evaluates
>    all four** — wrapping the same AND as the engine's one registered
>    rule and calling the run-everything mode instead of the composite's
>    own evaluate gets a result for every sub-rule regardless of where
>    the composite itself would have stopped.
> 3. **A composite's own result is truncated at its own short-circuit
>    point, even under run-everything mode** — short-circuiting is the
>    composite's own behavior, unaffected by how it's invoked — so a
>    checklist that must show *every* criterion regardless of an early
>    failure needs the sub-rules run individually, not nested in one
>    composite.

## What a solution must demonstrate

- All four criteria combine with AND-like (every-one-must-pass)
  semantics for the fast yes/no path.
- The same four criteria, registered individually with the engine,
  produce a full per-criterion breakdown under the run-everything mode
  — never truncated by short-circuiting.
- Both paths (fast gate, full checklist) are built from the same rule
  objects, never two independently maintained implementations.
- Each criterion is unit-testable on its own, without populating every
  other criterion's own inputs first.
- The naive-way section names a concrete drift risk (two functions kept
  in sync by hand, with nothing that catches them diverging), not a
  hypothetical one.

## Related

- [`content-moderation-routing/`](../content-moderation-routing/README.md) —
  another run-mode example, for when a single engine needs to serve
  more than one independent decision.
