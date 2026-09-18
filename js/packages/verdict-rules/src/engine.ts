import { UnknownLookupError } from "./errors.js";
import type { RuleResult, RunResult } from "./result.js";
import type { Rule } from "./rule.js";

/**
 * Holds a set of rules and answers questions about them.
 *
 * The engine's run modes never short-circuit; a composite reaches one
 * verdict and stops.
 *
 * Generic over `TContext`, the same way {@link Rule} is, with no default
 * type parameter — dict-context is `RulesEngine<Context>`.
 */
export class RulesEngine<TContext> {
  readonly #rules: readonly Rule<TContext>[];
  readonly #byName: Map<string, Rule<TContext>>;
  readonly #byGroup: Map<string, Rule<TContext>[]>;

  constructor(rules: readonly Rule<TContext>[]) {
    this.#rules = [...rules];
    this.#byName = new Map(rules.map((r) => [r.name, r]));
    this.#byGroup = new Map();
    for (const rule of rules) {
      if (rule.group === undefined || rule.group === "") continue;
      const existing = this.#byGroup.get(rule.group);
      if (existing === undefined) this.#byGroup.set(rule.group, [rule]);
      else existing.push(rule);
    }
  }

  /** Every rule name registered here, in registration order. */
  get ruleNames(): readonly string[] {
    return [...this.#byName.keys()];
  }

  /** Every group label carried by at least one rule, in first-seen order. */
  get groupNames(): readonly string[] {
    return [...this.#byGroup.keys()];
  }

  /** Evaluate every registered rule. Never short-circuits. */
  async runAll(context: TContext): Promise<RunResult> {
    const results: RuleResult[] = [];
    for (const rule of this.#rules) {
      results.push(await rule.evaluate(context));
    }
    return { passed: results.every((r) => r.passed), results };
  }

  /**
   * Evaluate one rule by name, or return `undefined` if no such rule exists.
   *
   * `undefined` means absent, never failed.
   */
  async tryRunNamed(
    name: string,
    context: TContext,
  ): Promise<RuleResult | undefined> {
    const rule = this.#byName.get(name);
    if (rule === undefined) return undefined;
    return rule.evaluate(context);
  }

  /**
   * Evaluate exactly one rule, looked up by name.
   *
   * @throws {UnknownLookupError} if no rule has this name.
   */
  async runNamed(name: string, context: TContext): Promise<RuleResult> {
    const result = await this.tryRunNamed(name, context);
    if (result === undefined) {
      throw new UnknownLookupError("rule", name);
    }
    return result;
  }

  /**
   * Evaluate a group, or return `undefined` if no such group exists.
   *
   * `undefined` means absent, never vacuously passed.
   */
  async tryRunGroup(
    group: string,
    context: TContext,
  ): Promise<RunResult | undefined> {
    const rules = this.#byGroup.get(group);
    if (rules === undefined || rules.length === 0) return undefined;
    const results: RuleResult[] = [];
    for (const rule of rules) {
      results.push(await rule.evaluate(context));
    }
    return { passed: results.every((r) => r.passed), results };
  }

  /**
   * Evaluate every rule sharing a group label. Never short-circuits.
   *
   * @throws {UnknownLookupError} if no rule carries this label.
   */
  async runGroup(group: string, context: TContext): Promise<RunResult> {
    const result = await this.tryRunGroup(group, context);
    if (result === undefined) {
      throw new UnknownLookupError("group", group);
    }
    return result;
  }
}
