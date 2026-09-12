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

## Use it from a browser, with no build step

Published as ESM, CommonJS, and a plain global bundle, so a vanilla page works
without tooling. **Always pinned, always with an integrity hash:**

```html
<script
  src="https://cdn.jsdelivr.net/npm/verdict-rules@0.0.1/dist/verdict-rules.global.min.js"
  integrity="sha384-x/fYRkMxsgqXSrvhdixpsWsPx6LusOb0BpsMQ4sSljVqMQnB/GrTaLxiRsTyWsIq"
  crossorigin="anonymous"></script>
<script>
  const rule = new VerdictRules.FunctionRule("ok", async () => ({
    ruleName: "ok",
    passed: true,
  }));
  new VerdictRules.AndRule("all", [rule]).evaluate({}).then((v) => console.log(v.passed));
</script>
```

Neither half of that is optional, and both failures are silent:

- **Unpinned** (`/verdict-rules/` with no `@version`) means the next release
  reaches every page using it, with nobody editing anything.
- **No `integrity`** means the browser executes whatever the CDN returns, with
  full access to the page — cookies, DOM, network. A `<script src>` from a CDN
  is the one distribution path where a third party sits inside your runtime
  trust boundary. The hash makes a compromised CDN able to break the page but
  never to inject into it.

The exact tag for any version is generated, not written by hand:

```sh
npm run sri            # ready-to-paste tags for every CDN
npm run sri:verify     # assert each CDN serves exactly the published bytes
```

It is also checked: `npm test` fails if any `<script>` tag in this
documentation is unpinned or missing its hash.

Or as a module, with no bundler — no SRI, because these transform on the fly
and so have no stable bytes to hash:

```html
<script type="module">
  import { AndRule } from "https://cdn.jsdelivr.net/npm/verdict-rules@0.0.1/+esm";
</script>
```

`require("verdict-rules")` works too. The global build targets ES2019, so it
runs in browsers that never learned private class fields.

Nothing is published to a CDN separately: unpkg, jsDelivr and esm.sh all mirror
npm automatically, so every one of them serves this package the moment it is on
npm. The same integrity hash is valid on all of them, because they all serve the
tarball's bytes unchanged.

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

Python raises `KeyError` here, C# `KeyNotFoundException`, Dart `ArgumentError`.
JavaScript has no built-in equivalent, and a bare `Error` would leave callers
matching on message text — which breaks the moment a message is reworded. So
the package exports its own.

Reaching for it in a `catch` usually means the check belongs earlier:
`ruleNames` and `groupNames` let you ask before calling.

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
