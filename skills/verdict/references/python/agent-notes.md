# Python — agent notes

What is specific to the Python SDK, and to writing Python that uses it.
The engine's own general guarantees are in `SKILL.md`, not repeated here.

## Install and import

```bash
pip install verdict-rules
```

```python
from verdict import (
    AndRule, FunctionRule, NotRule, OrRule, PredicateOutcome,
    Rule, RuleResult, RulesEngine, RunResult,
)
```

> Note: The distribution is **`verdict-rules`**; the import is **`verdict`**.

## The API, in one screen

```python
Rule[TContext]             # Protocol: name, group, async evaluate(context: TContext) -> RuleResult
                           # dict and typed (dataclass, TypedDict) contexts are equally first-class
FunctionRule(name, predicate, group=None) # TContext inferred from the predicate's own annotation
AndRule(name, rules, group=None)          # passes only if every sub-rule passes
OrRule(name, rules, group=None)           # passes as soon as one does
NotRule(name, rule, group=None)           # passes exactly when the one wrapped rule fails

engine = RulesEngine(rules)
await engine.run_all(context)             # every rule, never short-circuits
await engine.run_named(name, context)     # one rule; raises KeyError if absent
await engine.run_group(group, context)    # one group;  raises KeyError if absent
await engine.try_run_named(name, context) # -> RuleResult | None
await engine.try_run_group(group, context)# -> RunResult  | None
engine.rule_names, engine.group_names     # tuples of what exists

PredicateOutcome(passed, detail="", data=None)   # what a predicate returns; it has no name field
RuleResult(rule_name, passed, detail="", data=None, sub_results=(), decided_by_indices=())
RunResult(passed, results)                       # all three are frozen dataclasses
```

A predicate reports a `PredicateOutcome`; the `FunctionRule` wrapping it
owns the name and builds the `RuleResult`:

```python
async def over_18(context: dict) -> PredicateOutcome:
    age = context["age"]
    return PredicateOutcome(passed=age >= 18, detail=f"age {age}")

rule = FunctionRule("over_18", over_18)
```

## Reading a result

```python
result.sub_results     # this result's own children, exactly what it evaluated
result.decided_by      # which of those explain this result's own verdict
result.leaves          # every leaf reachable from here, flattened
result.failing_leaves  # the leaves explaining a failure
```

`RunResult` exposes `leaves`/`failing_leaves` too, flattened across every
rule the run evaluated.

**Only `sub_results` and `decided_by_indices` are stored.** The other three
are `@property` accessors computed on access, so a result is a finite tree
and `json.dumps(dataclasses.asdict(result))` works. Build one by passing
positions, not children:

```python
RuleResult(rule_name="pair", passed=False, sub_results=(a, b), decided_by_indices=(1,))
```

An index naming a child the result does not have raises `IndexError` at
construction.

Key an audit trail on a leaf's own `rule_name`, never a composite's:

```python
verdict = await graduates.evaluate(student)
if not verdict.passed:
    log.warning("refused by %s", verdict.failing_leaves[0].rule_name)
```

Composites short-circuit, so these hold only what was evaluated: a failed
`AndRule` has exactly one failing leaf, and a passing `OrRule` has none
even when an earlier branch failed on the way to that pass.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.run_all()` |
| One named rule/group; absence would be a bug | The strict lookup — raises |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
