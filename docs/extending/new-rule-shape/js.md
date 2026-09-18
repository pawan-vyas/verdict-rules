<!-- Title: Extending — A Genuinely New Rule Shape (JS/TS) -->
# A genuinely new rule shape: JS/TS

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import { FunctionRule, type Rule, type RuleResult } from "verdict-rules";

/**
 * Passes if at least `minimum` of the given sub-rules pass.
 *
 * Not part of verdict itself -- a consumer-defined combinator, exactly
 * as free to exist as AndRule/OrRule are, with no changes needed on
 * verdict's side to support it.
 */
class ThresholdRule<TContext> implements Rule<TContext> {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule<TContext>[];
  readonly #minimum: number;

  constructor(name: string, rules: readonly Rule<TContext>[], minimum: number, group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
    this.#minimum = minimum;
  }

  async evaluate(context: TContext): Promise<RuleResult> {
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

`ThresholdRule<TContext>` can now be handed to a `RulesEngine`, nested
inside an `AndRule`, or hold an `AndRule` as one of its own sub-rules —
every existing piece of this package already knows how to run it,
because nothing anywhere checks `instanceof FunctionRule` or similar;
the `Rule` interface's structural shape is the only contract that
matters.

The same case the spec's own diagram shows — 2 of 3 needed, the third
sub-rule fails:

```ts
const atLeastTwo = new ThresholdRule<Record<string, unknown>>(
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

`ThresholdRule<TContext>` binds every direct sub-rule to the same
`TContext` — but a sub-rule can be a
[`ProjectingRule`](../reusing-a-rule-across-contexts/js.md), which
itself satisfies `Rule<TContext>` while its wrapped rule reads a
narrower, different type internally. The combinator stays bound to one
context; what its sub-rules actually read does not have to match:

```ts
const verifiedRule = new FunctionRule("is_verified_user", isVerifiedUser); // reads UserFlag
const checkoutVerified = new ProjectingRule<OrderContext, UserFlag>(
  verifiedRule,
  (ctx) => ({ isVerified: ctx.isVerified }),
);

const qualifies = new ThresholdRule<OrderContext>(
  "qualifies",
  [
    checkoutVerified, // reads UserFlag internally, via the projection
    new FunctionRule("has_promo_code", hasPromoCode), // reads OrderContext directly
  ],
  2,
);
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/graduation-requirement-verdict/`](../../samples/graduation-requirement-verdict/README.md) —
  `AtLeastNRule`, the design this exact pattern would back, once this
  SDK has its own tested instance.
- [`../reusing-a-rule-across-contexts/js.md`](../reusing-a-rule-across-contexts/js.md) —
  `ProjectingRule` itself, used above to mix a sub-rule reading a
  narrower context into a `ThresholdRule<TContext>` bound to a wider
  one.
