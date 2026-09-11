import { UnknownLookupError } from "./errors.js";
import type { Context, RuleResult, RunResult } from "./result.js";
import type { Rule } from "./rule.js";

/**
 * Holds a set of rules and answers questions about them.
 *
 * Distinct from a composite: a composite returns one verdict and stops early,
 * whereas the engine's run modes are diagnostic and never short-circuit. They
 * exist to produce a full picture, not the fastest path to one boolean.
 */
export class RulesEngine {
  readonly #rules: readonly Rule[];
  readonly #byName: Map<string, Rule>;
  readonly #byGroup: Map<string, Rule[]>;

  constructor(rules: readonly Rule[]) {
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

  /**
   * Every rule name registered here, in registration order.
   *
   * Exactly the names {@link runNamed} accepts, so a caller who cannot know in
   * advance whether a rule exists can check rather than catch.
   */
  get ruleNames(): readonly string[] {
    return [...this.#byName.keys()];
  }

  /**
   * Every group label carried by at least one rule, in first-seen order.
   *
   * Exactly the labels {@link runGroup} accepts. A group is present only
   * because some rule declared it.
   */
  get groupNames(): readonly string[] {
    return [...this.#byGroup.keys()];
  }

  /** Evaluate every registered rule. Never short-circuits. */
  async runAll(context: Context): Promise<RunResult> {
    const results: RuleResult[] = [];
    for (const rule of this.#rules) {
      results.push(await rule.evaluate(context));
    }
    return { passed: results.every((r) => r.passed), results };
  }

  /**
   * Evaluate exactly one rule, looked up by name.
   *
   * @throws {UnknownLookupError} if no rule has this name.
   */
  async runNamed(name: string, context: Context): Promise<RuleResult> {
    const rule = this.#byName.get(name);
    if (rule === undefined) {
      throw new UnknownLookupError("rule", name);
    }
    return rule.evaluate(context);
  }

  /**
   * Evaluate every rule sharing a group label. Never short-circuits.
   *
   * @throws {UnknownLookupError} if no rule carries this label.
   *
   * An unknown group throws rather than returning a vacuous pass, matching
   * {@link runNamed}. A group exists only because some rule declared it, so an
   * empty-but-real group is not representable and a lookup matching nothing
   * can only be a typo or a stale name. Returning a pass there would mean a
   * misspelled group silently approves.
   *
   * This is the one place the package is strict. Emptiness — a set you were
   * handed that happened to be empty — still folds to its identity; absence is
   * an error. Use {@link groupNames} to check first if a group may
   * legitimately be absent.
   */
  async runGroup(group: string, context: Context): Promise<RunResult> {
    const rules = this.#byGroup.get(group);
    if (rules === undefined || rules.length === 0) {
      throw new UnknownLookupError("group", group);
    }
    const results: RuleResult[] = [];
    for (const rule of rules) {
      results.push(await rule.evaluate(context));
    }
    return { passed: results.every((r) => r.passed), results };
  }
}
