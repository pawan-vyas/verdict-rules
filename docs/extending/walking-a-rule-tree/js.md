<!-- Title: Extending — Walking a Rule Tree (JS/TS) -->
# Walking a rule tree: JS/TS

> The concept, the diagram, and the trap are in
> [`README.md`](README.md) — read that first. This page is the concrete
> JS/TS code.

`isCompositeRule` is a type guard, so TypeScript narrows to
`CompositeRule<TContext>` inside the branch and `subRules` is typed without a
cast. The guard is **structural**, so your own composite narrows exactly as a
built-in one does — implement `CompositeRule<TContext>` and you are done.

This matters more here than in the other SDKs: the built-in composites store
their parts in a `#private` field, so `subRules` is not a convenience over an
already-reachable field — it is the only way to read them at all.

```ts
import {
  AndRule,
  type Context,
  FunctionRule,
  isCompositeRule,
  NotRule,
  OrRule,
  type Rule,
  RuleResult,
} from "verdict-rules";

/** A consumer-defined composite that also carries its own threshold. */
class AtLeastNRule implements Rule<Context> {
  readonly name: string;
  readonly group: string | undefined;
  readonly subRules: readonly Rule<Context>[];
  readonly #minimum: number;

  constructor(name: string, rules: readonly Rule<Context>[], minimum: number) {
    this.name = name;
    this.group = undefined;
    this.subRules = [...rules];
    this.#minimum = minimum;
  }

  async evaluate(context: Context): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    for (const rule of this.subRules) {
      subResults.push(await rule.evaluate(context));
    }
    const passed = subResults.filter((r) => r.passed).length;
    return new RuleResult(this.name, passed >= this.#minimum, {
      subResults,
      decidedByIndices: subResults.flatMap((r, i) => (r.passed ? [i] : [])),
    });
  }
}

/** Every rule in the tree, parents before children, depth-first. */
function* walk(rule: Rule<Context>): Generator<Rule<Context>> {
  yield rule;
  if (isCompositeRule(rule)) {
    for (const part of rule.subRules) {
      yield* walk(part);
    }
  }
}

function leafNames(rule: Rule<Context>): string[] {
  return [...walk(rule)].filter((r) => !isCompositeRule(r)).map((r) => r.name);
}

/** Names used by two *different* rule objects -- a build problem. */
function duplicateNames(rule: Rule<Context>): string[] {
  const seen = new Map<string, Rule<Context>>();
  const duplicates = new Set<string>();
  for (const found of walk(rule)) {
    const first = seen.get(found.name);
    if (first === undefined) seen.set(found.name, found);
    else if (first !== found) duplicates.add(found.name);
  }
  return [...duplicates];
}

const leaf = (name: string, passed = true) =>
  new FunctionRule<Context>(name, async () => ({ passed }));

const tree = new AndRule<Context>("eligible", [
  leaf("verified"),
  new OrRule<Context>("either_path", [leaf("auto", false), leaf("manual")]),
  new NotRule<Context>("not_blocked", leaf("blocked", false)),
  new AtLeastNRule("two_of_three", [leaf("a"), leaf("b"), leaf("c", false)], 2),
]);

console.log(leafNames(tree));
// [ 'verified', 'auto', 'manual', 'blocked', 'a', 'b', 'c' ]

console.log(duplicateNames(tree));
// []

// The custom composite's own parts are reached, so its three leaves are
// listed. An `instanceof` chain over AndRule/OrRule/NotRule would have
// stopped at `two_of_three` and reported four leaves instead of seven.
const verdict = await tree.evaluate({});
console.log(verdict.passed, verdict.subResults.length);
// true 4
```

`AtLeastNRule` declares `implements Rule<Context>` and still narrows, because
the guard checks the shape rather than the declaration — declaring
`implements CompositeRule<Context>` instead is clearer and makes the compiler
check `subRules` for you. Both work.

## Related

- [`README.md`](README.md) — the concept and the trap this page implements.
- [`../new-rule-shape/js.md`](../new-rule-shape/js.md) — where
  `AtLeastNRule` comes from, written out in full.
