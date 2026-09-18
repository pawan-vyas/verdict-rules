# JavaScript/TypeScript — agent notes

Short by design. Everything about *what verdict is* lives in
`references/docs/`, which is the repository's own documentation rather
than a summary that could drift from it. This file carries only what is
specific to the JS/TS SDK, and to writing JS/TS that uses it.

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

## Mistakes that show up in generated JS/TS specifically

- **`Promise.all` in a composite.** It starts every sub-rule's coroutine
  before the first result returns and destroys the short-circuit
  guarantee. Sub-rules are evaluated in a plain `for…of` loop with
  `await`, one at a time.
- **`result || default`.** JS's `||` fires on any *falsy* value, not
  only `undefined` — `0`, `""`, `false` all trigger it. Write
  `result?.passed ?? default`, or an explicit
  `result !== undefined ? result.passed : default`. Unlike Python's
  equivalent footgun, this one is not saved by the result types
  happening to be truthy — `passed: false` is a real, common value here.
- **A predicate returning a bare `boolean`.** `FunctionRule`'s predicate
  must return a `RuleResult`, not `true`/`false`.
- **Catching `UnknownLookupError` where a rule shape should be reached
  for instead.** Checking `engine.ruleNames.includes(name)` (or
  `groupNames`) before calling is usually clearer than a `try`/`catch`
  around the strict form, and is exactly what those two getters exist
  for — see `runNamed`'s own doc comment.
- **A `Rule` implementation importing anything from `verdict-rules`.**
  It does not need to — `Rule` is a structural `interface`, so the right
  shape (a `name` and an `evaluate` method) is enough. This is the same
  property Python's `Protocol` gives; unlike C#/Dart, no `implements`
  clause is required either.
- **`ruleName` set to something other than the rule's own `name`.** A
  caller walking a `RunResult` attributes outcomes by that field.
- **Writing bare `Rule` or `FunctionRule` with no type argument and
  expecting dict-context.** There is no default type parameter,
  deliberately — `Rule<Context>`/`FunctionRule<Context>` is the
  dict-context spelling, written out every time. An untyped `Rule`
  alone is a type error, not a silent `Rule<Context>`.
- **Mixing sub-rules of different `TContext`s inside one
  `AndRule`/`OrRule`.** `tsc` rejects this once a type argument is
  named — reuse a rule across two shapes via an explicit projecting
  adapter (`docs/extending/reusing-a-rule-across-contexts/js.md`)
  instead of loosening the composite's own type.

## Testing what matters

[`references/docs/testing/`](../../../../docs/testing/README.md) (fetch it) is the full checklist. The parts
that are easy to skip:

- Prove short-circuiting with a **call log**, not the final boolean. A
  composite that evaluates everything still returns the right answer.
- Give each **vacuous-truth polarity** its own test. They are asymmetric.
- Test both halves of a lookup: the strict form throwing **and** the
  `try` form returning `undefined`.
- If code uses a fallback, test the **present-but-failing** case — not
  just the absent one. That is the direction where a bug is silent.
- For a rule set **built from stored/config data at runtime** rather
  than hand-written, hand-picked fixtures stop scaling as the
  configuration space grows — reach for property-based testing or an
  oracle/differential approach (an independent, deliberately simpler
  reference implementation checked against many random configurations)
  instead of adding fixtures one at a time as bugs are found.

## Fetching the deeper documents

```bash
scripts/fetch-docs.sh js
```

See [`commands/verdict-fetch-docs.md`](../../commands/verdict-fetch-docs.md).
