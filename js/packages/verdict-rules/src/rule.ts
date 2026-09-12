import type { Context, RuleResult } from "./result.js";

/**
 * The contract every rule satisfies.
 *
 * A plain `interface`, which in TypeScript means **structural** typing: any
 * object of the right shape *is* a `Rule`. No `implements` clause, no base
 * class, no registration.
 *
 * ```ts
 * const overEighteen = {
 *   name: "over_18",
 *   async evaluate(ctx: Context) {
 *     return { ruleName: "over_18", passed: (ctx.age as number) >= 18 };
 *   },
 * };            // already a Rule — nothing declared
 * ```
 *
 * This matches Python's `Protocol` exactly. Dart and C# have nominal typing
 * and require an explicit `implements`, so the extension story genuinely
 * differs between the SDKs rather than only its syntax.
 */
export interface Rule {
  /**
   * Unique identifier for this rule, used for engine lookups and to attribute
   * a result back to its source.
   */
  readonly name: string;

  /** Optional group label. Rules sharing one can be run together. */
  readonly group?: string | undefined;

  /** Evaluate this rule against `context`. */
  evaluate(context: Context): Promise<RuleResult>;
}

/** Signature of the predicate {@link FunctionRule} wraps. */
export type RulePredicate = (context: Context) => Promise<RuleResult>;

/**
 * Wraps a plain async predicate as a {@link Rule}.
 *
 * The shape most rules should be: no new class, no ceremony.
 */
export class FunctionRule implements Rule {
  readonly name: string;
  readonly group: string | undefined;
  readonly #predicate: RulePredicate;

  constructor(name: string, predicate: RulePredicate, group?: string) {
    this.name = name;
    this.group = group;
    this.#predicate = predicate;
  }

  /** Runs the wrapped predicate and returns whatever it returns, unchanged. */
  evaluate(context: Context): Promise<RuleResult> {
    return this.#predicate(context);
  }
}

/**
 * Composite that passes only if every sub-rule passes.
 *
 * Short-circuits on the first failing sub-rule: later sub-rules are never
 * evaluated once one has failed, so a caller can rely on this never doing more
 * work — or having more side effects — than the minimum needed to reach a
 * verdict.
 *
 * An empty list **passes** vacuously: nothing to fail on, and the identity of
 * the fold it performs. The opposite polarity to {@link OrRule}, which is
 * deliberate and easy to get backwards.
 */
export class AndRule implements Rule {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule[];

  constructor(name: string, rules: readonly Rule[], group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
  }

  async evaluate(context: Context): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    // A plain sequential loop, never Promise.all: short-circuiting only means
    // something if later work never *starts*, and concurrent scheduling would
    // already have kicked off every sub-rule before the first result returns.
    // The returned boolean is identical either way, so this breaks silently.
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
 * An empty list **fails** vacuously: nothing to pass on. The opposite of
 * {@link AndRule}, and the asymmetry is the point.
 */
export class OrRule implements Rule {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule[];

  constructor(name: string, rules: readonly Rule[], group?: string) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
  }

  async evaluate(context: Context): Promise<RuleResult> {
    const subResults: RuleResult[] = [];
    // Sequential, for the same reason as AndRule.
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
