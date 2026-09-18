<!-- Title: Extending — Deciding What A Missing Rule Set Means (JS/TS) -->
# Deciding what a missing rule set means: JS/TS

> The concept, the table, and the diagram are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { FunctionRule, RulesEngine, type RuleResult } from "verdict-rules";

interface UserContext {
  betaTester?: boolean;
}

async function isBetaTester(context: UserContext): Promise<RuleResult> {
  return { ruleName: "is_beta_tester", passed: context.betaTester ?? false };
}

const engine = new RulesEngine<UserContext>([new FunctionRule("is_beta_tester", isBetaTester, "beta_checks")]);

const result = await engine.tryRunGroup("beta_checks", { betaTester: true });
// { passed: true, results: [ { ruleName: 'is_beta_tester', passed: true } ] }

await engine.tryRunGroup("no_such_group", {});
// undefined -- the group was never registered
```

The four situations from the spec, as four different ways to consume
that same `undefined`:

```ts
// 1. Absence means "no constraint applies"
const result1 = await engine.tryRunGroup(group, context);
const allowed1 = result1 !== undefined ? result1.passed : true;

// 2. Absence means "the configuration is wrong"
const result2 = await engine.tryRunGroup(group, context);
const allowed2 = result2 !== undefined ? result2.passed : false;

// 3. Absence means "skip it" -- contributes nothing either way
const checks = [await engine.tryRunGroup(group, context)].filter((r) => r !== undefined);

// 4. Absence is genuinely unexpected -- say so immediately
const result4 = await engine.runGroup(group, context); // throws UnknownLookupError
```

`tryRunGroup` is the primitive `runGroup` is built on, not the other way
around:

```ts
async runGroup(group: string, context: Context): Promise<RunResult> {
  const result = await this.tryRunGroup(group, context);
  if (result === undefined) {
    throw new UnknownLookupError("group", group);
  }
  return result;
}
```

There is one lookup path. The strict form is a two-line assertion on top
of the lenient one, rather than a second implementation that could drift
from it.

If you only need to enumerate what exists, `engine.ruleNames` and
`engine.groupNames` report exactly the lookups that will not throw.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
