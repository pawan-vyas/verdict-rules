# Core Concepts

The five names, one complete example, and the execution-model
guarantees that make short-circuiting actually mean something.

## The types

- **`Rule`** — a structural `Protocol`. Anything with a `name: str`, a
  `group: str | None`, and an `async evaluate(context: dict) ->
  RuleResult` method satisfies it — no inheritance, no registration, no
  base class. This is what makes every recipe in
  `extension-recipes.md` free: a custom rule shape is just a plain
  object with the right members.
- **`FunctionRule(name, predicate, group=None)`** — wraps a plain async
  predicate as a `Rule`. The common case: most rules are "run this
  function against the context and see what it says."
- **`AndRule(name, rules, group=None)`** — passes only if every
  sub-rule passes. Stops at the first failure — later sub-rules are
  never evaluated once one has already failed. `AndRule([])` **passes**
  vacuously (nothing to fail on).
- **`OrRule(name, rules, group=None)`** — passes if any sub-rule
  passes. Stops at the first pass. `OrRule([])` **fails** vacuously
  (nothing to pass on) — the opposite polarity from `AndRule`, and easy
  to get backwards.
- **`RulesEngine(rules)`** — holds a rule collection, offers three ways
  to run it:
  - `run_all(context)` — every rule, unconditionally. Never
    short-circuits; the point is a full diagnostic picture, not the
    fastest path to one boolean.
  - `run_named(name, context)` — exactly one rule, looked up by name.
    Raises `KeyError` if no rule has that name.
  - `run_group(group, context)` — every rule sharing a group label.
    Also never short-circuits. Raises `KeyError` if no rule carries
    that label, exactly like `run_named`: a group exists only because
    some rule declared it, so a lookup matching nothing is a typo or a
    stale name, never a legitimately empty group.
  - `rule_names` / `group_names` — read-only tuples of what is
    registered, so a caller who cannot know in advance whether a name
    exists can check rather than catch.
- **`RuleResult(rule_name, passed, detail="", data=None)`** — the
  outcome of evaluating one rule. `data` is a fully opaque slot for a
  caller's own domain object (a computed status, a sub-result list) to
  ride through evaluation — nothing in verdict reads or depends on its
  shape.
- **`RunResult(passed, results)`** — the aggregate outcome of
  `run_all`/`run_group`: `passed` is `True` only if every entry in
  `results` passed; `results` holds one `RuleResult` per rule that was
  evaluated, in order. A short-circuited composite rule still
  contributes exactly **one** entry to `results` — its own sub-rules'
  results live nested inside that one entry's own `data`, never
  flattened into the outer list.

## One complete example

```python
import asyncio
from verdict import AndRule, FunctionRule, RuleResult, RulesEngine


async def under_daily_limit(context: dict) -> RuleResult:
    spent_today = context["spent_today"]
    limit = context["daily_limit"]
    return RuleResult(
        rule_name="under_daily_limit",
        passed=spent_today < limit,
        detail=f"{spent_today} of {limit}",
    )


async def account_in_good_standing(context: dict) -> RuleResult:
    return RuleResult(
        rule_name="account_in_good_standing",
        passed=context["account_status"] == "active",
    )


async def main() -> None:
    can_proceed = AndRule(
        "can_proceed",
        [
            FunctionRule("under_daily_limit", under_daily_limit),
            FunctionRule("account_in_good_standing", account_in_good_standing),
        ],
    )

    engine = RulesEngine([can_proceed])
    result = await engine.run_named(
        "can_proceed",
        {"spent_today": 42, "daily_limit": 100, "account_status": "active"},
    )
    print(result.passed)  # True


asyncio.run(main())
```

Walking through it: `run_named` looks `"can_proceed"` up by name and
calls its `evaluate()`. `AndRule` evaluates `under_daily_limit` first
(passes), then `account_in_good_standing` (passes) — if the first had
failed, the second would never run at all. The top-level `RuleResult`
the caller sees has `data` holding both sub-results nested inside it,
not flattened into anything the caller has to unpack for this simple
case.

## Execution model: sequential, never concurrent

`AndRule`/`OrRule` evaluate their sub-rules one at a time, with a plain
loop and `await` — never `asyncio.gather` or any other concurrent
scheduling. This is deliberate, not an oversight, and it's why
short-circuiting is a real, reliable contract rather than a best-effort
optimization:

- **Short-circuiting only means something if later work never starts.**
  Concurrent evaluation would already have kicked off every sub-rule's
  coroutine before the first result comes back — a rule whose predicate
  has a real side effect (a DB write, an external call) would still
  fire even though its outcome could no longer change the composite's
  own result.
- **Evaluation order is part of the contract.** A caller ordering rules
  cheapest-first can rely on that ordering being honored exactly, never
  raced. This is why an `OrRule`'s sub-rule order is a real design
  decision (cheapest/most-likely-to-pass check first), not cosmetic.

`RulesEngine.run_all()`/`run_group()` are different on purpose: they
evaluate every rule unconditionally, with no short-circuiting at all,
because their job is a full diagnostic picture, not a fast verdict.

## Composing rules dynamically

Because rules are just plain objects, they're straightforward to build
up at runtime from configuration rather than one hand-written
`FunctionRule` per call site:

```python
checks = [make_rule(cfg) for cfg in load_configured_checks(metric)]
verdict = await AndRule("combined", checks).evaluate(context)
```

A metric with no configured checks produces an empty `AndRule`, which
vacuously passes — "nothing configured" and "nothing to enforce" fall
out of the same rule, with no special-casing needed at the call site.
See `extension-recipes.md`'s Recipe 4 for the fuller version of this
pattern.
