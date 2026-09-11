# verdict-rules

> A small, zero-dependency, async-native rule-evaluation engine for
> JavaScript and TypeScript. Compose independently-changing conditions into one
> explainable pass/fail verdict.

The JS/TS SDK of [verdict](https://github.com/pawan-vyas/verdict-rules), which
exists in more than one language with identical execution-model guarantees.

**This is `0.0.1` — correct, but minimal.** The type set and its guarantees are
complete and tested. The worked example, the shared cross-language fixture, and
the full documentation set arrive before `0.1.0`.

## Install

```sh
npm install verdict-rules
```

## Use

```ts
import { AndRule, FunctionRule, type Context } from "verdict-rules";

const atLeast = (name: string, field: string, floor: number) =>
  new FunctionRule(name, async (ctx: Context) => {
    const value = ctx[field] as number;
    return { ruleName: name, passed: value >= floor, detail: `${value} vs ${floor}` };
  });

const eligible = new AndRule("eligible", [
  atLeast("age_ok", "age", 18),
  atLeast("score_ok", "score", 60),
]);

const verdict = await eligible.evaluate({ age: 21, score: 55 });
console.log(verdict.passed); // false
console.log(verdict.detail); // 'score_ok' failed: 55 vs 60
```

## If it has the shape, it is a rule

`Rule` is a plain `interface`, and TypeScript's typing is **structural** — so
any object of the right shape already *is* a `Rule`. No `implements`, no base
class, no registration:

```ts
const overEighteen = {
  name: "over_18",
  async evaluate(ctx: Context) {
    return { ruleName: "over_18", passed: (ctx.age as number) >= 18 };
  },
};

await new AndRule("eligible", [overEighteen]).evaluate({ age: 21 });
```

This matches Python's `Protocol` exactly. The Dart and C# SDKs have nominal
typing and require an explicit `implements`, so the extension story genuinely
differs between them rather than only the syntax.

Most rules need no object literal either: `FunctionRule` wraps a plain async
predicate.

## What it guarantees

- **Sequential evaluation, never concurrent.** Composites use a plain `for`
  loop with `await`, never `Promise.all`. Short-circuiting only means something
  if later work never *starts* — and because the returned boolean is identical
  either way, getting this wrong is silent.
- **Vacuous truth has a polarity.** `AndRule([])` passes, `OrRule([])` fails.
  Deliberately asymmetric.
- **Emptiness is not absence.** An empty composite folds to its identity; an
  unknown rule name or group **throws**. A group exists only because some rule
  declared it, so a lookup matching nothing can only be a mistake — and a
  misspelled group silently approving is the worst failure an eligibility check
  can have. Use `ruleNames` / `groupNames` to check rather than catch.
- **`RuleResult.data` is opaque** — only what actually ran, never padded, never
  flattened.
- **Zero runtime dependencies.**

## Licence

MIT.
