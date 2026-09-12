<!-- Title: Sample — Dynamic Discount Eligibility -->
# Sample: Dynamic Discount Eligibility

> **The question**: does this cart qualify for a promotional discount,
> under a promo policy that always checks the same three things — a
> minimum cart value, an eligible region, and first-purchase status — but
> whose *thresholds* (the minimum amount, which regions count) are set by
> marketing and change on their own schedule? **Why it's a good fit for a
> rule engine**: the checkout code shouldn't need a deploy every time
> marketing runs a weekend campaign with a lower minimum, and a customer
> asking "why didn't my discount apply?" deserves a real answer, not just
> a `False`.

## The naive way (and why it breaks down)

The obvious first implementation is a nested `if` chain, checked
straight against the cart, with the current campaign's numbers baked
directly into the code:

```python
async def qualifies_for_promo(cart: dict) -> bool:
    if cart["total"] >= 100:
        if cart["region"] in ("US", "CA"):
            if cart["is_first_purchase"]:
                return True
    return False
```

This is fine for exactly as long as the campaign's numbers never change.
In practice:

- **Every marketing adjustment is a code change.** Lowering the minimum
  from `100` to `75` for a weekend campaign, or adding `"MX"` to the
  region list, means editing this function and shipping a deploy — for
  a change that has nothing to do with how the checkout code itself
  works.
- **A fourth condition means a fourth nesting level.** The function only
  gets harder to read as more conditions accumulate, and nothing stops
  two conditions from silently depending on evaluation order in a way
  nobody intended.
- **"Why didn't my discount apply?" has no good answer.** The function
  returns a single `bool` — a support agent (or the customer-facing UI)
  can't tell *which* of the three conditions failed without re-deriving
  it by hand from the cart data.

## The verdict way

The three *kinds* of check stay fixed in code — a minimum-value
comparison, a region-membership check, a boolean flag — each a genuine,
distinct predicate rather than one generic comparison forced to cover
all three. What marketing actually changes (the minimum amount, the
eligible-region list) travels in as plain values on the context, sourced
from wherever the current campaign's config actually lives — a settings
table, a feature-flag service, whatever the checkout code already reads
campaign data from. Changing a threshold is a data change; it never
touches this code.

Each condition is built **once**, as its own `FunctionRule`, and reused
for two different needs rather than declared twice: a fast boolean gate
for checkout itself, and a full per-condition breakdown for "why didn't
this apply?" — the same three objects feed both.

```mermaid
graph TB
    Config[("🗄️ Current campaign's<br/>minimum & regions")]
    Context[/"📥 cart context<br/>(cart facts + campaign values)"/]
    RuleA["✅ cart_meets_minimum"]
    RuleB["✅ is_eligible_region"]
    RuleC["✅ is_first_purchase"]
    Combined{"🔀 AndRule<br/>'qualifies_for_promo'"}
    RunAll[["⚙️ RulesEngine.run_all()"]]
    Gate("🚦 Fast boolean gate<br/>(checkout)")
    Breakdown("📋 Full per-condition<br/>breakdown (support)")

    %% Link 0: Config -> Context
    Config -->|"[1]<br/>merged in at check time"| Context
    %% Link 1: Context -> RuleA
    Context -->|"[2]<br/>read by"| RuleA
    %% Link 2: Context -> RuleB
    Context -->|"[3]<br/>read by"| RuleB
    %% Link 3: Context -> RuleC
    Context -->|"[4]<br/>read by"| RuleC
    %% Link 4: RuleA -> Combined
    RuleA -->|"[5]<br/>same rule object,<br/>sub-rule here"| Combined
    %% Link 5: RuleB -> Combined
    RuleB -->|"[6]<br/>same rule object,<br/>sub-rule here"| Combined
    %% Link 6: RuleC -> Combined
    RuleC -->|"[7]<br/>same rule object,<br/>sub-rule here"| Combined
    %% Link 7: Combined -> Gate
    Combined -->|"[8]<br/>evaluate() — stops<br/>at the first failure"| Gate
    %% Link 8: RuleA -> RunAll
    RuleA -->|"[9]<br/>and registered<br/>directly, here"| RunAll
    %% Link 9: RuleB -> RunAll
    RuleB -->|"[10]<br/>and registered<br/>directly, here"| RunAll
    %% Link 10: RuleC -> RunAll
    RuleC -->|"[11]<br/>and registered<br/>directly, here"| RunAll
    %% Link 11: RunAll -> Breakdown
    RunAll -->|"[12]<br/>every condition,<br/>never truncated"| Breakdown

    style Config fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000
    style Context fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style RuleA fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style RuleB fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style RuleC fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Combined fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style RunAll fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000
    style Gate fill:#74C0FC,stroke:#1971C2,stroke-width:2px,color:#000
    style Breakdown fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: the current campaign's numbers are merged into the context, not baked into code
    %% 1-3: each fixed rule reads whatever it needs off that one context
    %% 4-6: the same three rule objects are handed to the AndRule as sub-rules...
    %% 7: ...whose own evaluate() is the fast path, stopping at the first failure
    %% 8-10: ...and separately registered with the engine, unwrapped...
    %% 11: ...so run_all() reports every one of them, regardless of how many failed
    linkStyle 0 stroke:#7EDB8F,stroke-width:2px
    linkStyle 1 stroke:#E0E0E0,stroke-width:2px
    linkStyle 2 stroke:#E0E0E0,stroke-width:2px
    linkStyle 3 stroke:#E0E0E0,stroke-width:2px
    linkStyle 4 stroke:#C9B3FF,stroke-width:2px
    linkStyle 5 stroke:#C9B3FF,stroke-width:2px
    linkStyle 6 stroke:#C9B3FF,stroke-width:2px
    linkStyle 7 stroke:#8ECFFF,stroke-width:3px
    linkStyle 8 stroke:#FFCB7A,stroke-width:2px
    linkStyle 9 stroke:#FFCB7A,stroke-width:2px
    linkStyle 10 stroke:#FFCB7A,stroke-width:2px
    linkStyle 11 stroke:#8CE99A,stroke-width:3px
```

> **Why the `AndRule` alone isn't the full answer, and registering it
> with the engine doesn't fix that either**:
> 1. **A single `passed=False` isn't useful to a support agent** — "why
>    didn't the discount apply?" needs to know *which* condition failed,
>    not just that one did.
> 2. **`AndRule.evaluate()`'s own `RuleResult.data` carries the
>    sub-results up to and including the first failure — and stops
>    there.** A cart failing both the minimum *and* the region never
>    reveals the second failure; `data` holds one entry, not two.
> 3. **Handing the `AndRule` to `RulesEngine` and calling `run_all()`
>    does not change this.** `run_all()` evaluates every *registered*
>    rule unconditionally — but if the only registered rule is the
>    `AndRule` itself, there is still only one rule to run, and its own
>    internal short-circuiting is untouched. The truncation moves, it
>    doesn't disappear.
> 4. **The fix is to register the three leaf rules with the engine
>    directly — not the `AndRule`.** `run_all()` over
>    `[minimum_rule, region_rule, first_purchase_rule]` evaluates all
>    three unconditionally, because none of them is short-circuiting
>    machinery. The `AndRule` still exists, built from the exact same
>    three objects, for whichever call site wants the fast boolean gate
>    instead.

## The code

```python
from verdict import AndRule, FunctionRule, RuleResult, RulesEngine


async def cart_meets_minimum(context: dict) -> RuleResult:
    total, minimum = context["cart_total"], context["promo_minimum"]
    return RuleResult(
        rule_name="cart_meets_minimum",
        passed=total >= minimum,
        detail=f"cart_total={total}, needs >= {minimum}",
    )


async def is_eligible_region(context: dict) -> RuleResult:
    region, eligible = context["region"], context["eligible_regions"]
    return RuleResult(
        rule_name="is_eligible_region",
        passed=region in eligible,
        detail=f"region={region!r} not in {sorted(eligible)}",
    )


async def is_first_purchase(context: dict) -> RuleResult:
    return RuleResult(rule_name="is_first_purchase", passed=context["is_first_purchase"])


# Built once. The AndRule and the engine below both hold these same three
# objects — nothing is declared twice, and nothing here needs to know in
# advance which of the two call sites below will use it.
minimum_rule = FunctionRule("cart_meets_minimum", cart_meets_minimum)
region_rule = FunctionRule("is_eligible_region", is_eligible_region)
first_purchase_rule = FunctionRule("is_first_purchase", is_first_purchase)

qualifies_for_promo = AndRule("qualifies_for_promo", [minimum_rule, region_rule, first_purchase_rule])
engine = RulesEngine([minimum_rule, region_rule, first_purchase_rule])


async def check_cart_fast(cart: dict, campaign: dict) -> bool:
    """The checkout gate: the cheapest possible yes/no, stopping at the
    first failing condition. Nothing past this point needs to know
    *which* condition failed — only whether the discount applies."""
    context = {**cart, **campaign}
    return (await qualifies_for_promo.evaluate(context)).passed


async def why_not(cart: dict, campaign: dict) -> RulesEngine:
    """The support-facing view: every condition, always — even if more
    than one failed at once. Never the fast path's job, so it doesn't
    share the fast path's short-circuiting.

    `campaign` (e.g. `{"promo_minimum": 100, "eligible_regions": {"US", "CA"}}`)
    is whatever the checkout code already reads the active campaign's
    configured values from — nothing here cares where it came from.
    """
    context = {**cart, **campaign}
    return await engine.run_all(context)
    # result.passed is true only if every condition passed; result.results
    # is one entry per condition, always all three, regardless of how many
    # failed — this is what a "why not?" screen actually needs.
```

Swapping the campaign's minimum from `100` to `75`, or adding `"MX"` to
`eligible_regions`, is now a data change passed into either function — no
edit to `cart_meets_minimum`/`is_eligible_region`/`is_first_purchase`,
the `AndRule`, or the engine. See
[`6_data-driven-rule-sets.md`](6_data-driven-rule-sets.md) for the
further step this sample deliberately stops short of: building the
*rules themselves* (not just their threshold values) from stored
configuration, for when the *kinds* of check need to change without a
deploy too, not just their numbers.

## Related

- [`3_shipping-fee-waiver.md`](3_shipping-fee-waiver.md) — the `OrRule`
  mirror image of this same idea (any one qualifying path, not all).
- [`6_data-driven-rule-sets.md`](6_data-driven-rule-sets.md) — building
  the rules themselves from stored config, not just their values.
