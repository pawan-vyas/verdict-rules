# Python — agent notes

Short by design. Everything about *what verdict is* lives in
`references/docs/`, which is the repository's own documentation rather
than a summary that could drift from it. This file carries only what is
specific to the Python SDK, and to writing Python that uses it.

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

## Mistakes that show up in generated Python specifically

- **`asyncio.gather` in a composite.** It returns the same boolean and
  destroys the short-circuit guarantee. Sub-rules are evaluated in a
  plain `for` loop with `await`, one at a time.
- **`result or default`.** Python's `or` fires on any *falsy* value, not
  only `None`. It happens to work here because these dataclasses are
  always truthy, but that is a property of this library rather than of
  the pattern. Write `result.passed if result is not None else default`.
- **A predicate returning a bare `bool`.** `FunctionRule`'s predicate
  must return a `RuleResult`, not `True`/`False`.
- **`rule_name` set to something other than the rule's own `name`.** A
  caller walking a `RunResult` attributes outcomes by that field.
- **Reaching for `try_run_*` to avoid thinking about absence.** A
  `KeyError` in development is a typo found in seconds; the same typo
  behind a default is a rule set that silently stopped being enforced.
  Use the strict form unless the caller's own domain has an answer for
  absence.
- **A `Rule` implementation importing anything from `verdict`.** It does
  not need to — `Rule` is a `Protocol`, so the right shape is enough.

## Testing what matters

[`references/docs/testing/`](../../../../docs/testing/README.md) (fetch it) is the full checklist,
with [`references/docs/testing/python.md`](../../../../docs/testing/python.md)
naming which test proves which contract. The parts that are easy to
skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- Give each **vacuous-truth polarity** its own test. They are asymmetric.
- Test both halves of a lookup: the strict form raising **and** the
  `try_` form returning `None`.
- If code uses a fallback, test the **present-but-failing** case — not
  just the absent one. That is the direction where a bug is silent.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing (e.g.
  Hypothesis) or an oracle/differential approach (an independent,
  deliberately simpler reference implementation checked against many
  random configurations) instead of adding fixtures one at a time as
  bugs are found. `python/examples/graduation_verdict/` is a real,
  shipped 500-case instance of the oracle/differential shape.

## Fetching the deeper documents

```bash
VERSION=$(python -c "import importlib.metadata as m; print(m.version('verdict-rules'))")
scripts/fetch-docs.sh python "$VERSION"
```

See [`commands/verdict-fetch-docs.md`](../../commands/verdict-fetch-docs.md).
