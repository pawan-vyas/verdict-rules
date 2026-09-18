<!-- Title: Extending — Isolating A Flaky Predicate (JS/TS) -->
# Isolating a flaky predicate: JS/TS

> The concept and the reasoning are in [`README.md`](README.md) — read
> that first. This page is the concrete JS/TS code.

```ts
import { FunctionRule, type RulePredicate, type RuleResult } from "verdict-rules";

interface PromoContext {
  promoCode: string;
  simulateTimeout?: boolean;
}

/** Turn a predicate's own exception into a failing RuleResult,
 * instead of letting it propagate out of the run that contains it. */
function defensive<TContext>(name: string, predicate: RulePredicate<TContext>): FunctionRule<TContext> {
  const wrapped = async (context: TContext): Promise<RuleResult> => {
    try {
      return await predicate(context);
    } catch (exc) {
      return { ruleName: name, passed: false, detail: (exc as Error).message };
    }
  };
  return new FunctionRule(name, wrapped);
}

/** Stands in for a real network call that can time out. */
async function checkPromoCodeAgainstExternalService(context: PromoContext): Promise<RuleResult> {
  if (context.simulateTimeout) {
    throw new Error("promo-validation service did not respond");
  }
  return { ruleName: "promo_code_valid", passed: context.promoCode === "SAVE10" };
}

const rule = defensive("promo_code_valid", checkPromoCodeAgainstExternalService);
```

```ts
await rule.evaluate({ promoCode: "SAVE10" });
// { ruleName: 'promo_code_valid', passed: true }

await rule.evaluate({ promoCode: "SAVE10", simulateTimeout: true });
// { ruleName: 'promo_code_valid', passed: false,
//   detail: 'promo-validation service did not respond' }
```

The same timeout against the **unwrapped** predicate propagates instead
of returning a result — this is what every other rule shares a
`runAll`/`runGroup` with, unless it's wrapped too:

```ts
const unwrapped = new FunctionRule("promo_code_valid", checkPromoCodeAgainstExternalService);
await unwrapped.evaluate({ promoCode: "SAVE10", simulateTimeout: true });
// throws Error: promo-validation service did not respond
```

Now a timeout in the wrapped check reports as `passed: false, detail: "..."`
— one entry in `RunResult.results`, same as any other failing rule — and
every other rule in that `runAll`/`runGroup` still runs and still
reports.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
