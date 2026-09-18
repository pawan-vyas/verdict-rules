import type { RuleResult } from "./result.js";

/**
 * The contract every rule satisfies.
 *
 * A plain `interface`, which in TypeScript means **structural** typing: any
 * object of the right shape *is* a `Rule`. No `implements` clause, no base
 * class, no registration.
 *
 * `Rule<TContext>` is generic over the context it reads from, with no
 * default type parameter — deliberately, unlike a `Rule<TContext = Context>`
 * shorthand that was considered and rejected. A default would make
 * "deliberately chose dict-context" and "forgot to type this" look
 * identical in source, which is a real inconsistency in a codebase this
 * strict elsewhere (`strict: true`, `noUncheckedIndexedAccess: true`).
 * Dict-context is `Rule<Context>`, written out every time — no more
 * ceremony than any other explicit type argument.
 *
 * ```ts
 * const overEighteen: Rule<Context> = {
 *   name: "over_18",
 *   async evaluate(ctx) {
 *     return { ruleName: "over_18", passed: (ctx.age as number) >= 18 };
 *   },
 * };            // already a Rule<Context> — nothing declared
 * ```
 *
 * This matches Python's `Protocol[TContext]` exactly. Dart and C# have
 * nominal typing and require an explicit `implements`, so the extension
 * story genuinely differs between the SDKs rather than only its syntax.
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
 * The shape most rules should be: no new class, no ceremony. `TContext` is
 * inferred from the wrapped predicate's own parameter type — a predicate
 * typed as `(ctx: OrderContext) => Promise<RuleResult>` needs no explicit
 * type argument at the `new FunctionRule(...)` call site.
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
 * Short-circuits on the first failing sub-rule: later sub-rules are never
 * evaluated once one has failed, so a caller can rely on this never doing more
 * work — or having more side effects — than the minimum needed to reach a
 * verdict.
 *
 * An empty list **passes** vacuously: nothing to fail on, and the identity of
 * the fold it performs. The opposite polarity to {@link OrRule}, which is
 * deliberate and easy to get backwards.
 *
 * Every sub-rule must share the exact same `TContext` — the type checker
 * enforces this once construction names a type argument. Reusing one rule
 * across two differently-shaped contexts goes through an explicit
 * projecting adapter (see docs/extending/reusing-a-rule-across-contexts/)
 * rather than loosening this constraint.
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
 * {@link AndRule}, and the asymmetry is the point. The same same-`TContext`
 * requirement across sub-rules applies here too; see {@link AndRule}.
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
