<!-- Title: Extending — Building Rule Sets From Stored Configuration (JS/TS) -->
# Building rule sets from stored configuration: JS/TS

> The concept and the testing implication are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { AndRule, FunctionRule, type Context, type RuleResult } from "verdict-rules";

interface RuleConfig {
  name: string;
  field: string;
  expected: unknown;
}

function makeRule(ruleConfig: RuleConfig): FunctionRule {
  return new FunctionRule(ruleConfig.name, async (context: Context): Promise<RuleResult> => {
    const actual = context[ruleConfig.field];
    const passed = actual === ruleConfig.expected;
    return { ruleName: ruleConfig.name, passed };
  });
}

/** Stands in for a real config source for this example. */
function loadRuleConfigs(): RuleConfig[] {
  return [
    { name: "is_manager", field: "role", expected: "manager" },
    { name: "in_headquarters", field: "office", expected: "HQ" },
  ];
}

const configuredRules = loadRuleConfigs().map(makeRule);
const combinedRule = new AndRule("combined", configuredRules);
```

```ts
await combinedRule.evaluate({ role: "manager", office: "HQ" });
// { ruleName: 'combined', passed: true, ... }

await combinedRule.evaluate({ role: "manager", office: "Remote" });
// { ruleName: 'combined', passed: false, ... } -- in_headquarters fails
```

An empty `loadRuleConfigs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/data-driven-rule-sets/js.md`](../../samples/data-driven-rule-sets/js.md) —
  the fuller worked version, in JS/TS.
