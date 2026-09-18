# Python — agent notes

What is specific to the Python SDK, and to writing Python that uses it.
The engine's own general guarantees are in `SKILL.md`, not repeated here.

## Install and import

```bash
pip install verdict-rules
```

```python
from verdict import AndRule, FunctionRule, OrRule, Rule, RuleResult, RulesEngine, RunResult
```

> Note: The distribution is **`verdict-rules`**; the import is **`verdict`**.

## The API, in one screen

```python
Rule[TContext]             # Protocol: name, group, async evaluate(context: TContext) -> RuleResult
                           # dict and typed (dataclass, TypedDict) contexts are equally first-class
FunctionRule(name, predicate, group=None) # TContext inferred from the predicate's own annotation
AndRule(name, rules, group=None)          # passes only if every sub-rule passes
OrRule(name, rules, group=None)           # passes as soon as one does

engine = RulesEngine(rules)
await engine.run_all(context)             # every rule, never short-circuits
await engine.run_named(name, context)     # one rule; raises KeyError if absent
await engine.run_group(group, context)    # one group;  raises KeyError if absent
await engine.try_run_named(name, context) # -> RuleResult | None
await engine.try_run_group(group, context)# -> RunResult  | None
engine.rule_names, engine.group_names     # tuples of what exists

RuleResult(rule_name, passed, detail="", data=None)   # frozen dataclasses, construct directly
RunResult(passed, results)
```

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.run_all()` |
| One named rule/group; absence would be a bug | The strict lookup — raises |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
