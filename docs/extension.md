<!-- Title: Verdict Extension Guide -->
# Verdict — Extension Guide

> For people building *on top of* Verdict from their own codebase — a new
> rule shape, a new domain, a new way of assembling rules — without
> changing anything under the package's own source itself (today, that's
> Python's `python/src/verdict/`; the same recipes apply unchanged in any
> future language this package ships for, against that language's own
> source tree). If the change you're making genuinely belongs inside this
> package, see [`maintenance.md`](maintenance.md) instead.

The short version: because [`Rule`](architecture.md#type-structure) is a
structural `Protocol`, not an abstract base class, everything below is
possible with **zero registration, zero imports beyond the ones you
already need, and zero subclassing**. You never ask this package's
permission to add a new kind of rule.

## Recipe 1 — wrap a predicate you already have

The common case, and the one `verdict` itself expects to cover most
needs: any async function that inspects a `dict` and decides pass/fail
is a rule the moment it's wrapped in `FunctionRule`.

```python
from verdict import FunctionRule, RuleResult

async def cart_meets_minimum(context: dict) -> RuleResult:
    total = context["cart_total"]
    minimum = context["minimum_for_offer"]
    return RuleResult(
        rule_name="cart_meets_minimum",
        passed=total >= minimum,
        detail=f"{total} vs minimum {minimum}",
    )

rule = FunctionRule("cart_meets_minimum", cart_meets_minimum)
```

No new class, no new file needed for this shape of rule — it's what
the large majority of rules should end up being.

## Recipe 2 — a genuinely new rule shape

`AndRule`/`OrRule` cover "every sub-rule must pass" and "at least one
must pass." A requirement that doesn't fit either — "at least N of these
M must pass," a weighted score threshold, anything with its own
combination logic — is a new class implemented entirely in **your own**
code, satisfying `Rule` structurally:

```python
from verdict import Rule, RuleResult


class ThresholdRule:
    """Passes if at least `minimum` of the given sub-rules pass.

    Not part of verdict itself — a consumer-defined combinator, exactly
    as free to exist as AndRule/OrRule are, with no changes needed on
    verdict's side to support it.
    """

    def __init__(self, name: str, rules: list[Rule], minimum: int, group: str | None = None) -> None:
        self.name = name
        self.group = group
        self._rules = rules
        self._minimum = minimum

    async def evaluate(self, context: dict) -> RuleResult:
        sub_results = [await rule.evaluate(context) for rule in self._rules]
        passed_count = sum(1 for r in sub_results if r.passed)
        return RuleResult(
            rule_name=self.name,
            passed=passed_count >= self._minimum,
            detail=f"{passed_count} of {len(self._rules)} passed, needed {self._minimum}",
            data=sub_results,
        )
```

`ThresholdRule` can now be handed to a `RulesEngine`, nested inside an
`AndRule`, or hold an `AndRule` as one of its own sub-rules — every
existing piece of this package already knows how to run it, because
nothing anywhere checks `isinstance(x, FunctionRule)` or similar; the
`Rule` `Protocol` (`@runtime_checkable`) is the only contract that
matters.

```mermaid
graph TB
    Ctx[/"📥 context"/]
    R1["✅ rule 1"]
    R2["✅ rule 2"]
    R3["❌ rule 3"]
    Tally{"🔀 ThresholdRule<br/>count >= minimum?"}
    Pass("✅ passed=True<br/>2 of 3, needed 2")

    %% Link 0: Ctx -> R1
    Ctx -->|"[1]<br/>evaluated"| R1
    %% Link 1: Ctx -> R2
    Ctx -->|"[2]<br/>evaluated"| R2
    %% Link 2: Ctx -> R3
    Ctx -->|"[3]<br/>evaluated too — no short-circuit"| R3
    %% Link 3: R1 -> Tally
    R1 -->|"[4]<br/>counted"| Tally
    %% Link 4: R2 -> Tally
    R2 -->|"[5]<br/>counted"| Tally
    %% Link 5: R3 -> Tally
    R3 -->|"[6]<br/>counted"| Tally
    %% Link 6: Tally -> Pass
    Tally -->|"[7]<br/>2 >= 2"| Pass

    style Ctx fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style R1 fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style R2 fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style R3 fill:#B47EFF,stroke:#9654E8,stroke-width:2px,color:#000
    style Tally fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style Pass fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0-2: every sub-rule is evaluated, including rule 3 which fails —
    %%      unlike AndRule/OrRule, there's no early exit to skip it
    %% 3-5: each sub-rule's own pass/fail is tallied, not short-circuited on
    %% 6: the tally, not any single sub-rule, decides the final verdict
    linkStyle 0 stroke:#E0E0E0,stroke-width:2px
    linkStyle 1 stroke:#E0E0E0,stroke-width:2px
    linkStyle 2 stroke:#E0E0E0,stroke-width:2px
    linkStyle 3 stroke:#C9B3FF,stroke-width:2px
    linkStyle 4 stroke:#C9B3FF,stroke-width:2px
    linkStyle 5 stroke:#C9B3FF,stroke-width:2px
    linkStyle 6 stroke:#C9B3FF,stroke-width:3px
```

> **The One Real Trade-Off Here**: this shape evaluates every sub-rule
> unconditionally rather than short-circuiting — a threshold count can't
> be decided early the way a plain `and`/`or` can, so rule 3 above still
> runs even though it ends up failing. That's a legitimate property of
> *this* rule shape to have, not something `verdict` imposes on it —
> contrast with `AndRule`/`OrRule`'s own diagrams in
> [`architecture.md`](architecture.md#execution-model-sequential-not-concurrent),
> where stopping early is the entire point.

## Recipe 3 — keep your own domain out of `verdict`, in one adapter module

The single most important extension pattern: build **one** module
that translates your domain's own vocabulary into `Rule` objects and
back out of `RuleResult.data`, and never let that vocabulary leak into
verdict itself or scatter across multiple call sites.

- **A rate-limiting adapter** — the *only* place your codebase's
  rate-limiting logic imports `verdict` directly. It builds one
  `FunctionRule` per configured rate-limit window, combines them under
  an `AndRule`, and packages its own rate-limit status object as each
  rule's opaque `RuleResult.data` — everything above this one adapter
  talks in its own rate-limit vocabulary, never in `Rule`/`RuleResult`.
- **A second, independent adapter in that same codebase** — for an
  entirely unrelated domain (category/group-based access control),
  reusing the identical engine with **zero changes to `verdict`
  itself**. This is how the pattern scales past one domain per
  codebase: two adapters, same package, no coupling between them.

```mermaid
graph TB
    subgraph YourCode["📦 Your codebase"]
        direction TB
        Domain["⚙️ Your domain logic<br/>(discounts, grants, whatever)"]
        Adapter[["🔌 One adapter module"]]
    end
    Verdict[("📦 verdict<br/>Rule / Engine / Result")]

    %% Link 0: Domain -> Adapter
    Domain -->|"[1]<br/>your own vocabulary"| Adapter
    %% Link 1: Adapter -> Verdict
    Adapter -->|"[2]<br/>Rule / RuleResult only"| Verdict
    %% Link 2: Verdict -> Adapter
    Verdict -->|"[3]<br/>RuleResult.data<br/>(opaque payload)"| Adapter
    %% Link 3: Adapter -> Domain
    Adapter -->|"[4]<br/>your own types, unpacked"| Domain

    style Domain fill:#FFB84D,stroke:#E69500,stroke-width:2px,color:#000
    style Adapter fill:#FFB84D,stroke:#E69500,stroke-width:3px,color:#000
    style Verdict fill:#96E6B3,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: domain logic hands its own facts to the adapter
    %% 1: the adapter builds Rule objects, the only place verdict is imported
    %% 2: verdict hands back RuleResult, whose .data is an opaque payload
    %% 3: the adapter unpacks that payload back into your own domain types
    linkStyle 0 stroke:#FFCB7A,stroke-width:2px
    linkStyle 1 stroke:#FFCB7A,stroke-width:3px
    linkStyle 2 stroke:#7EDB8F,stroke-width:3px
    linkStyle 3 stroke:#FFCB7A,stroke-width:2px
```

> **The Adapter Boundary**:
> 1. **Your domain logic never imports `verdict` directly** — it hands
>    its own facts to one adapter module in your own codebase.
> 2. **The adapter is the only place `verdict` gets imported** — it
>    builds `Rule`/`FunctionRule`/`AndRule`/`OrRule` objects and calls
>    into the engine, using nothing but verdict's own vocabulary.
> 3. **`RuleResult.data` is the payload channel back out** — verdict
>    never reads or constrains its shape, so the adapter can stash
>    whatever domain object it wants there and unpack it on the way back
>    to your own domain logic.
> 4. **The same shape repeats per domain, without interacting** — a
>    rate-limiting adapter and an access-control adapter in one
>    codebase are two independent instances of this diagram, sharing
>    the engine and nothing else.

Why this matters: the moment domain vocabulary (a rate-limit window, a
grant row, a discount code) leaks into a `Rule` implementation that isn't
confined to one adapter module, `verdict` stops being reusable for the
*next* domain in the same codebase — the whole reason a second consumer
could be added here with no engine-side changes at all.

## Recipe 4 — build rule sets from stored configuration at runtime

Because rules are plain objects, nothing stops building them from
whatever configuration a caller already has — a list of dicts, DB rows,
a settings file — rather than hand-writing one `FunctionRule` per case
at import time:

```python
from verdict import AndRule, FunctionRule, RuleResult


def make_rule(rule_config: dict) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        actual = context.get(rule_config["field"])
        passed = actual == rule_config["expected"]
        return RuleResult(rule_name=rule_config["name"], passed=passed)
    return FunctionRule(rule_config["name"], predicate)


configured_rules = [make_rule(cfg) for cfg in load_rule_configs()]
combined_rule = AndRule("combined", configured_rules)
```

An empty `load_rule_configs()` produces an empty `AndRule`, which
vacuously passes — "nothing configured" and "nothing to enforce" fall
out of the same rule, no special-casing needed at the call site. See
[`python/docs/samples/6_data-driven-rule-sets.md`](../python/docs/samples/6_data-driven-rule-sets.md)
for a fuller worked version of this, worked through for both
rate-limit windows and access-control conditions.

## Recipe 5 — nest composites arbitrarily

Because `AndRule`/`OrRule` satisfy `Rule` themselves, they can hold each
other as sub-rules to any depth, with no special-casing anywhere:

```python
from verdict import AndRule, OrRule

# is_active_account, is_premium_member, has_promo_code, and meets_spend_threshold
# are all already-built Rule instances (e.g. FunctionRule) from elsewhere in your
# own code — nesting doesn't care what built them.
qualifies = AndRule("qualifies", [
    is_active_account,
    OrRule("has_a_valid_reason", [is_premium_member, has_promo_code, meets_spend_threshold]),
])
```

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
> That's the whole payoff of `Rule` being a structural `Protocol`
> rather than a fixed type hierarchy — see
> [`architecture.md`](architecture.md#type-structure).

`qualifies` reads exactly like a plain rule to anything holding it — a
`RulesEngine`, another `AndRule`, or a direct `await qualifies.evaluate(context)`
call — since a composite rule is structurally indistinguishable from a
plain one from the outside.

## What you never need to do

- Register a new rule shape anywhere in this package.
- Subclass anything — `Rule` is a `Protocol`, not an `ABC`.
- Import `verdict` from more than one adapter module per domain (Recipe
  3) — if you find yourself doing that, that's the signal to consolidate.
- Change anything under a language's own package source for any of the
  recipes above — if a recipe seems to require that, it likely belongs
  in [`maintenance.md`](maintenance.md) instead, as a change to the
  package itself rather than an extension of it.

## Related docs

- [`architecture.md`](architecture.md) — why `Rule` being a `Protocol`
  is what makes all of the above free.
- [`maintenance.md`](maintenance.md) — changing this package itself.
- [`testing.md`](testing.md) — testing verdict itself, if a recipe here
  turns out to need a change on that side after all.
- [`python/docs/samples/`](../python/docs/samples/1_README.md) — full
  worked examples using these recipes end-to-end.
