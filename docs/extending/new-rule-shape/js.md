<!-- Title: Extending — A Genuinely New Rule Shape (JS/TS) -->
# A genuinely new rule shape: JS/TS

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { FunctionRule, type Rule, type Context, type RuleResult } from "verdict-rules";

/**
 * Passes if at least `minimum` of the given sub-rules pass.
 *
 * Not part of verdict itself -- a consumer-defined combinator, exactly
 * as free to exist as AndRule/OrRule are, with no changes needed on
 * verdict's side to support it.
 */
class ThresholdRule implements Rule {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule[];
  readonly #minimum: number;

  constructor(name: string, rules: readonly Rule[], minimum: number, group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
    this.#minimum = minimum;
  }

  async evaluate(context: Context): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    for (const rule of this.#rules) {
      subResults.push(await rule.evaluate(context));
    }
    const passedCount = subResults.filter((r) => r.passed).length;
    return {
      ruleName: this.name,
      passed: passedCount >= this.#minimum,
      detail: `${passedCount} of ${this.#rules.length} passed, needed ${this.#minimum}`,
      data: subResults,
    };
  }
}
```

`ThresholdRule` can now be handed to a `RulesEngine`, nested inside an
`AndRule`, or hold an `AndRule` as one of its own sub-rules — every
existing piece of this package already knows how to run it, because
nothing anywhere checks `instanceof FunctionRule` or similar; the
`Rule` interface's structural shape is the only contract that matters.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```ts
const atLeastTwo = new ThresholdRule(
  "at_least_two",
  [
    new FunctionRule("rule_1", async () => ({ ruleName: "rule_1", passed: true })),
    new FunctionRule("rule_2", async () => ({ ruleName: "rule_2", passed: true })),
    new FunctionRule("rule_3", async () => ({ ruleName: "rule_3", passed: false })),
  ],
  2,
);

const result = await atLeastTwo.evaluate({});
console.log(result.passed, result.detail);
// true, "2 of 3 passed, needed 2"
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/`](../../samples/graduation-requirement-verdict/README.md) —
  `AtLeastNRule`, the design this exact pattern would back, once this
  SDK has its own tested instance.
