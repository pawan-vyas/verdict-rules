/**
 * A small, zero-dependency, async-native rule-evaluation engine.
 *
 * `Rule<TContext>`/`RulesEngine<TContext>` are generic over the context they
 * read from, with no default type parameter — dict-context is
 * `FunctionRule<Context>`/`RulesEngine<Context>`.
 *
 * ```ts
 * import { AndRule, FunctionRule, type Context } from "verdict-rules";
 *
 * const overEighteen = new FunctionRule<Context>("over_18", async (ctx) => ({
 *   ruleName: "over_18",
 *   passed: (ctx.age as number) >= 18,
 * }));
 *
 * const verdict = await new AndRule<Context>("eligible", [overEighteen]).evaluate({ age: 21 });
 * console.log(verdict.passed); // true
 * ```
 */

export { RulesEngine } from "./engine.js";
export { UnknownLookupError } from "./errors.js";
export type { Context, RuleResult, RunResult } from "./result.js";
export { AndRule, FunctionRule, OrRule } from "./rule.js";
export type { Rule, RulePredicate } from "./rule.js";
