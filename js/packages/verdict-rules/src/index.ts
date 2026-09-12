/**
 * A small, zero-dependency, async-native rule-evaluation engine.
 *
 * Compose independently-changing conditions into one explainable pass/fail
 * verdict. Evaluation is sequential and never concurrent, which is what makes
 * short-circuiting a real contract rather than a best-effort optimisation.
 *
 * ```ts
 * import { AndRule, FunctionRule } from "verdict-rules";
 *
 * const overEighteen = new FunctionRule("over_18", async (ctx) => ({
 *   ruleName: "over_18",
 *   passed: (ctx.age as number) >= 18,
 * }));
 *
 * const verdict = await new AndRule("eligible", [overEighteen]).evaluate({ age: 21 });
 * console.log(verdict.passed); // true
 * ```
 */

export { RulesEngine } from "./engine.js";
export { UnknownLookupError } from "./errors.js";
export type { Context, RuleResult, RunResult } from "./result.js";
export { AndRule, FunctionRule, OrRule } from "./rule.js";
export type { Rule, RulePredicate } from "./rule.js";
