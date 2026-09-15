<!-- Title: Extending — Keep Your Own Domain Out Of Verdict (JS/TS) -->
# Keep your own domain out of verdict: JS/TS

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete JS/TS code, illustrating a
> rate-limiting adapter (one of the two illustrations the spec names),
> and — since the point of the boundary is that `verdict-rules` itself
> is replaceable behind it — a second implementation of the identical
> contract that doesn't use `verdict-rules` at all.

```ts
import { AndRule, FunctionRule, type Context, type RuleResult } from "verdict-rules";

/** This adapter's own domain type -- verdict-rules never sees it
 * directly, only hands it back as RuleResult.data's opaque payload. */
interface RateLimitStatus {
  window: string;
  used: number;
  quota: number;
}

/** The contract every call site depends on. Nothing here mentions
 * verdict-rules -- satisfying this structurally is enough, exactly the
 * way a Rule itself works. */
interface RateLimiter {
  check(context: Context, windows: Record<string, number>): Promise<RateLimitStatus[]>;
}

/** The only place in this codebase that imports from verdict-rules. */
class VerdictRateLimiter implements RateLimiter {
  async check(context: Context, windows: Record<string, number>): Promise<RateLimitStatus[]> {
    const ruleFor = (window: string, quota: number) =>
      new FunctionRule(`${window}_under_quota`, async (ctx: Context): Promise<RuleResult> => {
        const used = ctx[`${window}_used`] as number;
        const status: RateLimitStatus = { window, used, quota };
        return {
          ruleName: `${window}_under_quota`,
          passed: used < quota,
          data: status,
        };
      });

    const combined = new AndRule(
      "rate_limits",
      Object.entries(windows).map(([w, q]) => ruleFor(w, q)),
    );
    const result = await combined.evaluate(context);
    return (result.data as RuleResult[]).map((r) => r.data as RateLimitStatus);
  }
}

/** A hand-rolled replacement for VerdictRateLimiter -- same contract,
 * zero verdict-rules. Adding this is a new class; nothing above changed
 * to make room for it. */
class SimpleRateLimiter implements RateLimiter {
  async check(context: Context, windows: Record<string, number>): Promise<RateLimitStatus[]> {
    return Object.entries(windows).map(([window, quota]) => ({
      window,
      used: context[`${window}_used`] as number,
      quota,
    }));
  }
}
```

The composition root — the one place that decides which implementation
is actually running — is a single line:

```ts
// Before: wired to the verdict-rules-backed implementation.
let rateLimiter: RateLimiter = new VerdictRateLimiter();

// After: swapped for the hand-rolled one. One line, here, changes.
rateLimiter = new SimpleRateLimiter();

// Every call site in the codebase, unaffected either way:
const statuses = await rateLimiter.check(
  { minute_used: 3, hour_used: 40 },
  { minute: 5, hour: 100 },
);
console.log(statuses);
// [ { window: 'minute', used: 3, quota: 5 }, { window: 'hour', used: 40, quota: 100 } ]
```

That last line is the actual proof: it's identical before and after the
swap. Nothing that calls `rateLimiter.check(...)` knows or cares which
class it's holding, because `RateLimiter` is a structural `interface` —
the same property that lets a plain object satisfy `Rule` with no base
class is what lets `SimpleRateLimiter` satisfy this adapter's own
contract with no relationship to `VerdictRateLimiter` at all.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
