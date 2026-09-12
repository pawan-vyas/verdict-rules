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
   * Evaluate one rule by name, or return `undefined` if no such rule exists.
   *
   * This is the primitive; {@link runNamed} is a two-line assertion on top of
   * it. The distinction matters when absence is an expected, legitimate state
   * rather than a mistake — a rule set that varies per tenant, an optional
   * group behind a feature flag, a name carried in configuration that a given
   * deployment has not adopted yet.
   *
   * In those cases the caller decides what absence means, because the engine
   * cannot: for one consumer a missing rule means "nothing to enforce, pass",
   * for another "skip this and do not count it", for a third "the
   * configuration is wrong, fail loudly". A single library default would be
   * right for one of them and wrong for the rest.
   *
   * `undefined` means *absent*, never *failed* — a rule that exists and fails
   * still returns a {@link RuleResult} with `passed: false`.
   */
  async tryRunNamed(
    name: string,
    context: Context,
  ): Promise<RuleResult | undefined> {
    const rule = this.#byName.get(name);
    if (rule === undefined) return undefined;
    return rule.evaluate(context);
  }

  /**
   * Evaluate exactly one rule, looked up by name.
   *
   * The strict form, and the one to reach for by default: if a name is not
   * expected to be absent, an absent name is a bug worth hearing about
   * immediately. Use {@link tryRunNamed} when absence is a state your own
   * domain has an answer for.
   *
   * @throws {UnknownLookupError} if no rule has this name.
   */
  async runNamed(name: string, context: Context): Promise<RuleResult> {
    const result = await this.tryRunNamed(name, context);
    if (result === undefined) {
      throw new UnknownLookupError("rule", name);
    }
    return result;
  }

  /**
   * Evaluate a group, or return `undefined` if no such group exists.
   *
   * This is the primitive; {@link runGroup} is a two-line assertion on top of
   * it. See {@link tryRunNamed} for when reaching for it is right — the short
   * version is that the engine cannot know whether an absent group means "no
   * constraint applies here" or "the configuration is broken", and only the
   * caller can.
   *
   * `undefined` means *absent*, never *vacuously passed*. That distinction is
   * the whole point: a group exists only because some rule declared it, so an
   * empty-but-real group is not representable, and a lookup matching nothing
   * can only be a typo or a stale name. Returning a passing
   * {@link RunResult} here would mean a misspelled group silently approves.
   */
  async tryRunGroup(
    group: string,
    context: Context,
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
   * The strict form, and the one to reach for by default. Use
   * {@link tryRunGroup} when absence is a state your own domain has an answer
   * for.
   *
   * @throws {UnknownLookupError} if no rule carries this label.
   *
   * This is the one place the package is strict. Emptiness — a set you were
   * handed that happened to be empty — still folds to its identity; absence is
   * an error. Use {@link groupNames} to enumerate, or {@link tryRunGroup} to
   * ask in a single call.
   */
  async runGroup(group: string, context: Context): Promise<RunResult> {
    const result = await this.tryRunGroup(group, context);
    if (result === undefined) {
      throw new UnknownLookupError("group", group);
    }
    return result;
  }
}
