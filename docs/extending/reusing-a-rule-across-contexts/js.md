<!-- Title: Extending — Reusing a Typed Rule Across Contexts (JS/TS) -->
# Reusing a typed rule across contexts: JS/TS

> The concept, the diagram, and the trade-off are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

```ts
import type { Rule, RuleResult } from "verdict-rules";

/**
 * Adapts a Rule<TInner> to run inside a composite built on TOuter.
 *
 * Not part of verdict-rules itself -- a consumer-defined adapter, exactly
 * as free to exist as a new rule shape is, with no changes needed on
 * verdict-rules' side to support it.
 */
export class ProjectingRule<TOuter, TInner> implements Rule<TOuter> {
  readonly name: string;
  readonly group: string | undefined;

  constructor(
    private readonly inner: Rule<TInner>,
    private readonly project: (outer: TOuter) => TInner,
  ) {
    this.name = inner.name;
    this.group = inner.group;
  }

  evaluate(context: TOuter): Promise<RuleResult> {
    return this.inner.evaluate(this.project(context));
  }
}
```

Written once against its own narrow context, the same rule now
projects into two unrelated composites:

```ts
import { AndRule, FunctionRule } from "verdict-rules";

interface UserFlag {
  isVerified: boolean;
}

const isVerifiedUser = async (ctx: UserFlag) => ({
  ruleName: "is_verified_user",
  passed: ctx.isVerified,
});

interface OrderContext {
  total: number;
  isVerified: boolean;
}

interface SignupContext {
  email: string;
  isVerified: boolean;
}

const verifiedRule = new FunctionRule("is_verified_user", isVerifiedUser);

const checkoutVerified = new ProjectingRule<OrderContext, UserFlag>(
  verifiedRule,
  (ctx) => ({ isVerified: ctx.isVerified }),
);
const signupVerified = new ProjectingRule<SignupContext, UserFlag>(
  verifiedRule,
  (ctx) => ({ isVerified: ctx.isVerified }),
);

const checkout = new AndRule<OrderContext>("eligible", [checkoutVerified]);
const signup = new AndRule<SignupContext>("eligible", [signupVerified]);

const result = await checkout.evaluate({ total: 75, isVerified: true });
result.passed; // true -- delegated straight through to isVerifiedUser
```

`ProjectingRule` satisfies `Rule<TOuter>` structurally, the same way
every other rule in this package does — nothing about `AndRule` or
`RulesEngine` needed to change to accept it.

## Related

- [`README.md`](README.md) — the language-agnostic spec this page
  implements.
- [`../new-rule-shape/js.md`](../new-rule-shape/js.md) —
  the same pattern (a consumer-defined type satisfying `Rule`
  structurally) applied to combination logic instead of context
  adaptation.
