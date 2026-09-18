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

## The API, in one screen

```ts
interface Rule<TContext> {                // structural — any object of this shape is a Rule
  readonly name: string;
  readonly group?: string;
  evaluate(context: TContext): Promise<RuleResult>;
}
type RulePredicate<TContext> = (context: TContext) => Promise<RuleResult>;
type Context = Record<string, unknown>;   // never `any`

class UnknownLookupError extends Error {  // the only custom error this SDK defines
  readonly kind: "rule" | "group";        // which lookup missed
  readonly key: string;                   // the name/label that matched nothing
}

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
custom interface). There is no default type parameter, though — where a
predicate's own type can't be inferred, the dict-context spelling is
`Rule<Context>`/`FunctionRule<Context>`, written out.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
