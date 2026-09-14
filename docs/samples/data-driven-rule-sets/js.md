<!-- Title: Sample — Data-Driven Rule Sets (JS/TS) -->
# Sample: Data-Driven Rule Sets

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation compiles every configured condition
straight into an `if`/`else if` ladder in application code:

```ts
async function matchesConditionGrant(category: string | undefined, groups: string[]): Promise<boolean> {
  if (category === "Manager" && groups.includes("Headquarters")) {
    return true;
  }
  if (category === "Analyst" && (groups.includes("Support") || groups.includes("Headquarters"))) {
    return true;
  }
  return false;
}
```

This is, functionally, a two-row configuration table hand-transcribed
into source code — every new condition an admin wants is a developer
task, a PR, and a deploy. See the spec for the rest of what this shape
gets wrong.

## The `verdict-rules` way

```ts
import { AndRule, OrRule, type Rule, type Context, type RuleResult } from "verdict-rules";

/** One stored row describing a single, independently-editable rule. */
interface ConfiguredRow {
  id: number;
  field: string;
  operator: "gte" | "eq" | "in";
  value: unknown;
}

/** Turn one stored row into a Rule -- the only place that knows how. */
function ruleForRow(row: ConfiguredRow): Rule {
  return {
    name: `row:${row.id}`,
    async evaluate(context: Context): Promise<RuleResult> {
      const actual = context[row.field];
      const checks: Record<ConfiguredRow["operator"], () => boolean> = {
        gte: () => actual !== undefined && actual !== null && (actual as number) >= (row.value as number),
        eq: () => actual === row.value,
        in: () => (row.value as Set<unknown>).has(actual),
      };
      const passed = checks[row.operator]();
      return { ruleName: `row:${row.id}`, passed };
    },
  };
}

/** Stand-in for a real DB read -- the only I/O in this whole pattern.
 * A real implementation queries storage instead of returning a literal. */
async function fetchCurrentRows(): Promise<ConfiguredRow[]> {
  return [
    { id: 1, field: "category", operator: "eq", value: "Manager" },
    { id: 2, field: "region", operator: "in", value: new Set(["US", "CA", "UK"]) },
  ];
}

/**
 * Build the current rule set fresh and evaluate it -- nothing retained between calls.
 *
 * @param combine "all" (AndRule -- every row must pass; empty config passes) or
 *   "any" (OrRule -- at least one row must pass; empty config fails).
 *   Which default is correct is a domain decision, made explicitly here,
 *   not inferred -- see the spec's "reading the diagram" note on this.
 */
async function evaluateAgainstCurrentConfig(
  context: Context,
  combine: "all" | "any",
): Promise<boolean> {
  const rows = await fetchCurrentRows();
  const rules = rows.map(ruleForRow);
  const combined: Rule = combine === "all" ? new AndRule("combined", rules) : new OrRule("combined", rules);
  const result = await combined.evaluate(context);
  return result.passed;
}
```

Against the two rows above — a category grant and a region grant, both
required:

```ts
await evaluateAgainstCurrentConfig({ category: "Manager", region: "US" }, "all");
// true -- matches both rows

await evaluateAgainstCurrentConfig({ category: "Manager", region: "DE" }, "all");
// false -- row 2 fails; the DE region isn't in the configured set

await evaluateAgainstCurrentConfig({ category: "Manager", region: "DE" }, "any");
// true -- combine="any" only needs one row to pass
```

Editing row 2's `value` to add `"DE"` — a data change, in whatever
storage `fetchCurrentRows` reads from — changes the second call's
result with no edit to this function, `ruleForRow`, or the combinator.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/data-driven-rule-construction/`](../../extending/data-driven-rule-construction/README.md) —
  the general scenario this sample is a fuller version of, including how
  two unrelated domains share one engine without coupling to each
  other.
- [`dynamic-discounts/js.md`](../dynamic-discounts/js.md) — a smaller, single-`AndRule`
  instance of the same idea.
