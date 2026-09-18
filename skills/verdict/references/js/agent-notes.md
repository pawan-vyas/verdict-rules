# JavaScript/TypeScript — agent notes

What is specific to the JS/TS SDK, and to writing JS/TS that uses it.
The engine's own general guarantees are in `SKILL.md`, not repeated here.

## Install and import

```bash
npm install verdict-rules
```

```ts
import {
  AndRule,
  FunctionRule,
  OrRule,
  RulesEngine,
  UnknownLookupError,
} from "verdict-rules";
import type { Context, Rule, RulePredicate, RuleResult, RunResult } from "verdict-rules";
```

The package name and the import specifier are the same: **`verdict-rules`**,
unlike Python's split (`pip install verdict-rules` → `import verdict`).
ESM only — `require("verdict-rules")` resolves via the `exports` map's
`require` condition to a CJS build, but there is no separate package to
install for it.

## The API, in one screen

```ts
interface Rule<TContext> {                // structural — any object of this shape is a Rule
  readonly name: string;
  readonly group?: string;
  evaluate(context: TContext): Promise<RuleResult>;
}
type RulePredicate<TContext> = (context: TContext) => Promise<RuleResult>;
type Context = Record<string, unknown>;   // never `any`

new FunctionRule(name, predicate, group?)
new AndRule(name, rules, group?)          // passes only if every sub-rule passes
new OrRule(name, rules, group?)           // passes as soon as one does

const engine = new RulesEngine(rules);
await engine.runAll(context);             // every rule, never short-circuits
await engine.runNamed(name, context);     // one rule; throws UnknownLookupError if absent
await engine.runGroup(group, context);    // one group;  throws UnknownLookupError if absent
await engine.tryRunNamed(name, context);  // -> RuleResult | undefined
await engine.tryRunGroup(group, context); // -> RunResult  | undefined
engine.ruleNames, engine.groupNames       // readonly string[] of what exists
```

`RuleResult` and `RunResult` are plain readonly interfaces, not classes —
construct them as object literals: `{ ruleName, passed, detail?, data? }`
and `{ passed, results }`.

**A typed, non-dict context** reaches for the exact same
`FunctionRule`/`AndRule`/`OrRule`/`RulesEngine` — `TContext` is a type
parameter on each, not a separate name, and is inferred from the
predicate's own parameter type at the call site
(`new FunctionRule("x", predicate)` needs no explicit type argument as
long as `predicate` is typed, whether that type is `Context` or a
custom interface). There is no default type parameter, though — the
dict-context spelling still has to be written out somewhere a
predicate's own type can't be inferred from context; see the mistake
below. Every sub-rule inside one `AndRule<TContext>`/`OrRule<TContext>`
must share the exact same `TContext`.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |

## Testing what matters

`docs/testing/` in the source repository is the full checklist (see
`references/REPOSITORY-MAP.md`). The parts that are easy to skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing or an
  oracle/differential approach (an independent, deliberately simpler
  reference implementation checked against many random configurations)
  instead of adding fixtures one at a time as bugs are found.
