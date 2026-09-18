import type { RuleResult } from "./result.js";

/**
 * The contract every rule satisfies.
 *
 * A plain `interface` — structural typing: any object of the right shape
 * is a `Rule`. No `implements` clause, no base class, no registration.
 *
 * `Rule<TContext>` is generic over the context it reads from, with no
 * default type parameter. Dict-context is `Rule<Context>`, written out
 * every time.
 *
 * ```ts
 * const overEighteen: Rule<Context> = {
 *   name: "over_18",
 *   async evaluate(ctx) {
 *     return { ruleName: "over_18", passed: (ctx.age as number) >= 18 };
 *   },
 * };
 * ```
 */
export interface Rule<TContext> {
  /**
   * Unique identifier for this rule, used for engine lookups and to attribute
   * a result back to its source.
   */
  readonly name: string;

  /** Optional group label. Rules sharing one can be run together. */
  readonly group?: string | undefined;

  /** Evaluate this rule against `context`. */
  evaluate(context: TContext): Promise<RuleResult>;
}

/** Signature of the predicate {@link FunctionRule} wraps. */
export type RulePredicate<TContext> = (context: TContext) => Promise<RuleResult>;

/**
 * Wraps a plain async predicate as a {@link Rule}.
 *
 * `TContext` is inferred from the wrapped predicate's own parameter type.
 */
export class FunctionRule<TContext> implements Rule<TContext> {
  readonly name: string;
  readonly group: string | undefined;
  readonly #predicate: RulePredicate<TContext>;

  constructor(name: string, predicate: RulePredicate<TContext>, group?: string) {
    this.name = name;
    this.group = group;
    this.#predicate = predicate;
  }

  /** Runs the wrapped predicate and returns whatever it returns, unchanged. */
  evaluate(context: TContext): Promise<RuleResult> {
    return this.#predicate(context);
  }
}

/**
 * Composite that passes only if every sub-rule passes.
 *
 * Short-circuits on the first failing sub-rule.
 *
 * An empty list passes vacuously.
 *
 * Every sub-rule must share the exact same `TContext`.
 */
export class AndRule<TContext> implements Rule<TContext> {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule<TContext>[];

  constructor(name: string, rules: readonly Rule<TContext>[], group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
  }

  async evaluate(context: TContext): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    // Sequential, not Promise.all.
    for (const rule of this.#rules) {
      const result = await rule.evaluate(context);
      subResults.push(result);
      if (!result.passed) {
        const detail = result.detail
          ? `'${rule.name}' failed: ${result.detail}`
          : `'${rule.name}' failed`;
        return { ruleName: this.name, passed: false, detail, data: subResults };
      }
    }
    return { ruleName: this.name, passed: true, data: subResults };
  }
}

/**
 * Composite that passes as soon as any sub-rule passes.
 *
 * Short-circuits on the first passing sub-rule.
 *
 * An empty list fails vacuously. The same same-`TContext` requirement
 * across sub-rules applies here too.
 */
export class OrRule<TContext> implements Rule<TContext> {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule<TContext>[];

  constructor(name: string, rules: readonly Rule<TContext>[], group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
  }

  async evaluate(context: TContext): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    // Sequential, not Promise.all.
    for (const rule of this.#rules) {
      const result = await rule.evaluate(context);
      subResults.push(result);
      if (result.passed) {
        return { ruleName: this.name, passed: true, data: subResults };
      }
    }
    return {
      ruleName: this.name,
      passed: false,
      detail: "no sub-rule passed",
      data: subResults,
    };
  }
}
