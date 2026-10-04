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
  NotRule,
  OrRule,
  RuleResult,
  RulesEngine,
  RunResult,
  UnknownLookupError,
} from "verdict-rules";
import type { Context, PredicateOutcome, Rule, RulePredicate } from "verdict-rules";
```

> `RuleResult` and `RunResult` are classes, so they are value imports --
> `new RuleResult(...)` needs the binding itself, not `import type`.

## The API, in one screen

```ts
interface Rule<TContext> {                // structural — any object of this shape is a Rule
  readonly name: string;
  readonly group?: string;
  evaluate(context: TContext): Promise<RuleResult>;
}
type RulePredicate<TContext> = (context: TContext) => Promise<PredicateOutcome>;
interface PredicateOutcome {              // what a predicate returns; it has no ruleName
  readonly passed: boolean;
  readonly detail?: string;
  readonly data?: unknown;
}
type Context = Record<string, unknown>;   // never `any`; no default type param — dict is Rule<Context>

class UnknownLookupError extends Error {  // the only custom error this SDK defines
  readonly kind: "rule" | "group";        // which lookup missed
  readonly key: string;                   // the name/label that matched nothing
}

new FunctionRule(name, predicate, group?) // TContext inferred from the predicate's own parameter type
new AndRule(name, rules, group?)          // passes only if every sub-rule passes
new OrRule(name, rules, group?)           // passes as soon as one does
new NotRule(name, rule, group?)           // passes exactly when the one wrapped rule fails

const engine = new RulesEngine(rules);
await engine.runAll(context);             // every rule, never short-circuits
await engine.runNamed(name, context);     // one rule; throws UnknownLookupError if absent
await engine.runGroup(group, context);    // one group;  throws UnknownLookupError if absent
await engine.tryRunNamed(name, context);  // -> RuleResult | undefined
await engine.tryRunGroup(group, context); // -> RunResult  | undefined
engine.ruleNames, engine.groupNames       // readonly string[] of what exists

new RuleResult(ruleName, passed, { detail?, data?, subResults?, decidedByIndices? })
new RunResult(results)                    // `passed` is derived from the results
```

A predicate reports a `PredicateOutcome`; the `FunctionRule` wrapping it
owns the name and builds the `RuleResult`:

```ts
const overEighteen = new FunctionRule<Context>("over_18", async (ctx) => ({
  passed: (ctx.age as number) >= 18,
  detail: `age ${ctx.age}`,
}));
```

## Reading a result

```ts
result.subResults     // this result's own children, exactly what it evaluated
result.decidedBy      // which of those explain this result's own verdict
result.leaves         // every leaf reachable from here, flattened
result.failingLeaves  // the leaves explaining a failure
```

`RunResult` exposes `leaves`/`failingLeaves` too, flattened across every
rule the run evaluated. Both forward to each result's own view -- in
particular `failingLeaves` is **not** a filter over `leaves`, because a
result's verdict is not a function of its leaves' verdicts (a failed
`NotRule` is its own failing leaf; a passed `OrRule` may hold a failed
branch it recovered from).

**Only `subResults` and `decidedByIndices` are stored.** The other three are
prototype getters, so they are non-enumerable and `JSON.stringify(result)`
emits a finite tree. Build one by passing positions, not children:

```ts
new RuleResult("pair", false, { subResults: [a, b], decidedByIndices: [1] });
```

An index naming a child the result does not have throws `RangeError` at
construction. Instances are `Object.freeze`d -- `readonly` alone is erased
at runtime.

Key an audit trail on a leaf's own `ruleName`, never a composite's:

```ts
const verdict = await graduates.evaluate(student);
if (!verdict.passed) {
  const refusals = verdict.failingLeaves.map((leaf) => leaf.ruleName);
  logger.warn(`refused by ${refusals.join(", ")}`);
}
```

Composites short-circuit, so these hold only what was evaluated: a passing
`OrRule` has no failing leaves even when an earlier branch failed on the
way to that pass. **A failed `AndRule` reports the failing leaves of the
one sub-rule that stopped it** -- a single leaf only when that sub-rule is
itself a leaf, and several when it is a composite that failed on more than
one of its own. Read the whole list; indexing `[0]!` names one of several
causes without saying so, and the non-null assertion hides that the list
could have been empty.

A custom composite builds its result with the constructor, never by
spreading an existing one -- a spread yields a plain object without the
`leaves`/`failingLeaves` accessors, which is not a `RuleResult`.

## Which run mode

| Need | Reach for |
| --- | --- |
| One fast pass/fail verdict | A composite's own `evaluate()` |
| Every rule's own outcome (a status page, an audit trail) | `engine.runAll()` |
| One named rule/group; absence would be a bug | The strict lookup — throws |
| One named rule/group; absence is expected, your domain decides what it means | The non-raising lookup |
