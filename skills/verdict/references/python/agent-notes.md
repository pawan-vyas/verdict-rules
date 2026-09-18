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

The distribution is **`verdict-rules`**; the import is **`verdict`**.
The plain name was taken on PyPI, so the two differ — the same split as
`beautifulsoup4` → `bs4`. Add `verdict-rules` to the project's own
dependency list before writing code against it; an import working in one
file is not evidence it is declared.

## The API, in one screen

```python
Rule[TContext]             # Protocol: name, group, async evaluate(context: TContext) -> RuleResult
FunctionRule(name, predicate, group=None)
AndRule(name, rules, group=None)          # passes only if every sub-rule passes
OrRule(name, rules, group=None)           # passes as soon as one does

engine = RulesEngine(rules)
await engine.run_all(context)             # every rule, never short-circuits
await engine.run_named(name, context)     # one rule; raises KeyError if absent
await engine.run_group(group, context)    # one group;  raises KeyError if absent
await engine.try_run_named(name, context) # -> RuleResult | None
await engine.try_run_group(group, context)# -> RunResult  | None
engine.rule_names, engine.group_names     # tuples of what exists
```

`Rule` is generic over its context (`TContext`), inferred from a
predicate's own annotation — `FunctionRule("x", predicate)` needs no
explicit type argument as long as `predicate` is typed. A plain `dict`
context (`Rule[dict[str, Any]]`) is exactly as first-class as a typed
one (a dataclass, a `TypedDict`); default to whichever the rule set
naturally needs, never assume the typed form is "more correct." Every
sub-rule inside one `AndRule`/`OrRule` must share the same `TContext`.

`RuleResult(rule_name, passed, detail="", data=None)` and
`RunResult(passed, results)` are frozen dataclasses.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.run_all()` |
| One named rule/group; absence would be a bug | The strict lookup — raises |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |

## Testing what matters

`docs/testing/` in the source repository is the full checklist (see
`references/REPOSITORY-MAP.md`). The parts that are easy to skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing (e.g.
  Hypothesis) or an oracle/differential approach (an independent,
  deliberately simpler reference implementation checked against many
  random configurations) instead of adding fixtures one at a time as
  bugs are found. `python/examples/graduation_verdict/` is a real,
  shipped 500-case instance of the oracle/differential shape.
