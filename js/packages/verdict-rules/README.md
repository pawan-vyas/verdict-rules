# Verdict — JS/TS

> The JS/TS implementation of Verdict — a small, zero-dependency,
> async-native rule-evaluation engine.

## Install

```sh
npm install verdict-rules
```

```ts
import { AndRule, FunctionRule, RulesEngine } from "verdict-rules";
```

## A first rule

```ts
import { AndRule, FunctionRule, RulesEngine, type Context } from "verdict-rules";

const atLeast = (name: string, field: string, floor: number) =>
  new FunctionRule(name, async (ctx: Context) => {
    const value = ctx[field] as number;
    return { ruleName: name, passed: value >= floor, detail: `${value} vs ${floor}` };
  });

const eligible = new AndRule("eligible", [
  atLeast("age_ok", "age", 18),
  atLeast("score_ok", "score", 60),
]);

const engine = new RulesEngine([eligible]);
const verdict = await engine.runNamed("eligible", { age: 21, score: 55 });
console.log(verdict.passed); // false
console.log(verdict.detail); // 'score_ok' failed: 55 vs 60
```

## If it has the shape, it is a rule

`Rule` is a plain `interface`, and TypeScript's typing is **structural** — so
any object of the right shape already *is* a `Rule`. No `implements`, no base
class, no registration:

```ts
const isBusinessHours = {
  name: "is_business_hours",
  async evaluate(ctx: Context) {
    return { ruleName: "is_business_hours", passed: (ctx.hour as number) >= 9 && (ctx.hour as number) < 17 };
  },
};

await new AndRule("open", [isBusinessHours]).evaluate({ hour: 21 });
```

Most rules need no object literal either: `FunctionRule` wraps a plain async
predicate.

## Use it from a browser, with no build step

Published as ESM, CommonJS, and a plain global bundle, so a vanilla page works
without tooling.

As a module, with no bundler — no `integrity` needed here, since these
transform on the fly and so have no stable bytes to hash:

```html
<script type="module">
  import { AndRule } from "https://cdn.jsdelivr.net/npm/verdict-rules@latest/+esm";
</script>
```

`require("verdict-rules")` works too. The global build targets ES2019, so it
runs in browsers that never learned private class fields.

As a plain `<script>` tag, **pin the exact version and its integrity hash —
never `@latest` here.** Unpinned means the next release reaches every page
using it with nobody editing anything; no `integrity` means the browser
executes whatever the CDN returns, with full access to the page's cookies,
DOM, and network — a `<script src>` from a CDN is the one distribution path
where a third party sits inside your runtime trust boundary, and the hash is
what stops a compromised CDN from injecting into the page rather than only
being able to break it.

> [jsdelivr's own package page](https://www.jsdelivr.com/package/npm/verdict-rules)
> generates the exact `<script>` tag, with the correct hash, for whichever
> version you pick — copy it from there. A static example here would only be
> correct for whichever version existed when this page was written, and
> silently wrong for every release after it.

```html
<script
  src="https://cdn.jsdelivr.net/npm/verdict-rules@X.Y.Z/dist/verdict-rules.global.js"
  integrity="sha384-<the hash jsdelivr's package page shows for that version>"
  crossorigin="anonymous"></script>
<script>
  const rule = new VerdictRules.FunctionRule("ok", async () => ({
    ruleName: "ok",
    passed: true,
  }));
  new VerdictRules.AndRule("all", [rule]).evaluate({}).then((v) => console.log(v.passed));
</script>
```

Also available via [jsdelivr](https://www.jsdelivr.com/package/npm/verdict-rules),
[unpkg](https://unpkg.com/verdict-rules/), and [esm.sh](https://esm.sh/verdict-rules)
— same package, same integrity guarantee.

## Unknown lookups throw a typed error

```ts
import { UnknownLookupError } from "verdict-rules";

try {
  await engine.runGroup("cor", ctx);      // a typo
} catch (err) {
  if (err instanceof UnknownLookupError && err.kind === "group") {
    // err.key === "cor"
  }
}
```

JavaScript has no built-in equivalent to a typed lookup-miss error, and a bare
`Error` would leave callers matching on message text — which breaks the moment
a message is reworded. So the package exports its own.

Reaching for it in a `catch` usually means the check belongs earlier —
`tryRunNamed` and `tryRunGroup` ask in a single call, returning `undefined`
instead of throwing:

```ts
const result = await engine.tryRunGroup("beta_checks", ctx);
const allowed = result?.passed ?? true;   // absent means "no constraint here"
```

`undefined` means **absent, never failed** — a rule that exists and fails still
returns a `RuleResult` with `passed: false`. The engine does not pick a
fallback because it cannot: absence means "no constraint applies" to one
consumer and "the configuration is broken" to another. Prefer the throwing
forms by default; reach for these when your own domain has an answer for
absence.

**The fallback only applies to absence.** A group that exists always reports
its real verdict, so `?? true` does not mean "sometimes true" — a failing group
is still a failure whatever default you choose. If you test code using this,
the case worth covering is a *present, failing* group rather than the absent
one everybody thinks of first.

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

## Where to go next

| Doc | For |
| --- | --- |
| [`docs/quickstart.md`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/js/packages/verdict-rules/docs/quickstart.md) | The quickstart — core concepts and a full worked example |
| [`docs/architecture/`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/docs/architecture/README.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extending/`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/docs/extending/README.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance/`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/docs/maintenance/README.md) | Changing this package itself |
| [`docs/testing/`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/docs/testing/README.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/samples/`](https://github.com/pawan-vyas/verdict-rules/blob/js-v0.3.1/docs/samples/README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
