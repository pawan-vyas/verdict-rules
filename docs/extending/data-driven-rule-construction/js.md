<!-- Title: Extending — Building Rule Sets From Stored Configuration (JS/TS) -->
# Building rule sets from stored configuration: JS/TS

> The concept and the testing implication are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { AndRule, FunctionRule, type Context, type PredicateOutcome } from "verdict-rules";

interface RuleConfig {
  name: string;
  field: string;
  expected: unknown;
}

function makeRule(ruleConfig: RuleConfig): FunctionRule<Context> {
  return new FunctionRule(ruleConfig.name, async (context: Context): Promise<PredicateOutcome> => {
    const actual = context[ruleConfig.field];
    return { passed: actual === ruleConfig.expected };
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
// passed: true

const refused = await combinedRule.evaluate({ role: "manager", office: "Remote" });
refused.failingLeaves[0]?.ruleName;
// 'in_headquarters' -- the config-driven name, carried through
```

The rule name comes from the config once, at the `FunctionRule` call —
the predicate no longer repeats it, so a config whose name changes cannot
leave a result labelled with the old one.

An empty `loadRuleConfigs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
