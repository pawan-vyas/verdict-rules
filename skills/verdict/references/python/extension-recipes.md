# Extension Recipes

Because `Rule` is a structural `Protocol`, everything below is possible
with **zero registration, zero base classes, and zero changes to
verdict itself**. You never ask the package's permission to add a new
kind of rule or a new way of building them.

## Recipe 1 — wrap a predicate you already have

The common case, and the shape most real rules take:

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

## Recipe 2 — a genuinely new rule shape

`AndRule`/`OrRule` cover "every sub-rule must pass" and "at least one
must pass." A requirement that doesn't fit either — "at least N of
these M must pass," a weighted score threshold — is a new class
implemented entirely in **your own** code:

```python
from verdict import Rule, RuleResult

class AtLeastNRule:
    """Passes if at least `minimum` of the given sub-rules pass.

    Evaluates every sub-rule unconditionally — a threshold count can't
    be decided early the way a plain and/or can, so there's no
    short-circuit here. That's a property of this rule shape, not
    something verdict imposes.
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

This can now be handed to a `RulesEngine`, nested inside an `AndRule`,
or hold an `AndRule` as one of its own sub-rules — nothing checks
`isinstance(x, FunctionRule)` anywhere; the `Rule` Protocol
(`@runtime_checkable`) is the only contract that matters. Before adding
a shape like this, check `when-to-extend-the-core.md` — most ideas in
this category don't need to become a formally reusable pattern; a
one-off class in the consumer's own code, exactly like this, is usually
the right amount of engineering.

## Recipe 3 — keep your own domain out of verdict, in one adapter module

**The single most important pattern.** Build one module that
translates your domain's own vocabulary into `Rule` objects and back
out of `RuleResult.data`, and never let that vocabulary leak into a
`Rule` implementation used elsewhere, or scatter `from verdict import`
across multiple call sites:

```python
# rate_limit_adapter.py — the ONLY file that imports verdict for this feature
from verdict import AndRule, FunctionRule, RuleResult

def _rule_for_window(window) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        new_total = await window.record(window.amount)
        status = RateLimitStatus(  # your own domain type
            allowed=new_total <= window.limit,
            remaining=max(0, window.limit - new_total),
        )
        return RuleResult(rule_name=window.name, passed=status.allowed, data=status)
    return FunctionRule(window.name, predicate)

async def evaluate_rate_limit(windows: list) -> "RateLimitStatus":
    combined = await AndRule("windows", [_rule_for_window(w) for w in windows]).evaluate({})
    statuses = [r.data for r in combined.data]  # unpack your own type back out
    return statuses[-1] if not combined.passed else min(statuses, key=lambda s: s.remaining)
```

Everything *above* this adapter talks in `RateLimitStatus`, never in
`Rule`/`RuleResult`. This is what lets the identical engine serve a
second, completely unrelated feature (say, access-control conditions)
in the same codebase via a second adapter module, with zero coupling
between the two and zero changes to verdict itself — the concrete proof
this pattern scales past one domain per codebase.

## Recipe 4 — build rule sets from stored configuration at runtime

Because rules are plain objects, build them from whatever configuration
a caller already has — rows from a database, a config service — rather
than one hand-written `FunctionRule` per case:

```python
from verdict import AndRule, FunctionRule, RuleResult

def make_rule(rule_config: dict) -> FunctionRule:
    async def predicate(context: dict) -> RuleResult:
        actual = context.get(rule_config["field"])
        return RuleResult(rule_name=rule_config["name"], passed=actual == rule_config["expected"])
    return FunctionRule(rule_config["name"], predicate)

configured_rules = [make_rule(cfg) for cfg in load_rule_configs()]
combined_rule = AndRule("combined", configured_rules)
```

An empty `load_rule_configs()` produces an empty `AndRule`, which
vacuously passes — "nothing configured" and "nothing to enforce" fall
out of the same rule, no special-casing at the call site. Nothing stops
the *shape* each row becomes from varying too (an `AndRule` for one row
type, an `OrRule` for another, a plain `FunctionRule` for a third) —
dispatch on whatever discriminant the config rows carry, in one factory
function, the same way `Recipe 3`'s adapter is the one place that
matters for domain vocabulary.

## Recipe 5 — nest composites arbitrarily

`AndRule`/`OrRule` satisfy `Rule` themselves, so they hold each other as
sub-rules to any depth, with no special-casing anywhere:

```python
from verdict import AndRule, OrRule

# is_active_account, is_premium_member, has_promo_code, meets_spend_threshold
# are already-built Rule instances from elsewhere in your own code.
qualifies = AndRule("qualifies", [
    is_active_account,
    OrRule("has_a_valid_reason", [is_premium_member, has_promo_code, meets_spend_threshold]),
])
```

`qualifies` reads exactly like a plain rule to anything holding it — a
`RulesEngine`, another `AndRule`, or a direct `await
qualifies.evaluate(context)` call — since a composite rule is
structurally indistinguishable from a plain one from the outside.

## What you never need to do

- Register a new rule shape anywhere.
- Subclass anything — `Rule` is a `Protocol`, not an ABC.
- Import verdict from more than one adapter module per domain (Recipe
  3) — if you find yourself doing that, consolidate.
- Change anything in verdict's own source for any of the above — if a
  recipe seems to require that, see `when-to-extend-the-core.md` before
  assuming it does.
