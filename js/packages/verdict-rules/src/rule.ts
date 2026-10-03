import { RuleResult } from "./result.js";

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
 *     return new RuleResult("over_18", (ctx.age as number) >= 18);
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

/**
 * What a predicate reports back to the {@link FunctionRule} wrapping it.
 *
 * A predicate used to construct its own {@link RuleResult} directly,
 * including its own `ruleName` — completely decoupled from whatever name
 * the `FunctionRule` wrapping it was constructed with, and nothing kept the
 * two in sync. The fix is structural, not a runtime convention: a predicate
 * returns this instead, and only {@link FunctionRule.evaluate} ever builds
 * the final `RuleResult`, from the one name already fixed at construction.
 *
 * Deliberately has **no** `ruleName` field — restating a name that's
 * already fixed on the wrapping `FunctionRule` is never legitimate, not
 * just inconvenient, so the capability doesn't exist. **No `group` field
 * either, permanently** — `RuleResult` doesn't carry a `group` today; if
 * that ever changes, `group` stays exclusively sourced from
 * `FunctionRule.group`, the same way `name` already is, never from the
 * predicate.
 */
export interface PredicateOutcome {
  /** Whether the predicate's condition was satisfied. */
  readonly passed: boolean;

  /** Optional human-readable explanation. Empty when there's nothing to say. */
  readonly detail?: string;

  /** Optional payload a caller can attach; opaque to this package. */
  readonly data?: unknown;
}

/** Signature of the predicate {@link FunctionRule} wraps. */
export type RulePredicate<TContext> = (context: TContext) => Promise<PredicateOutcome>;

/**
 * Guards the one externally-authored boundary this package has: a
 * predicate's own return value. `RulePredicate`'s type annotation is only
 * ever checked by a type checker, never enforced at runtime, and a stale
 * predicate still built against the pre-`PredicateOutcome` calling
 * convention (returning a `RuleResult` directly) duck-types close enough —
 * both shapes carry `passed`/`detail`/`data` — to otherwise silently
 * "work" by accident instead of surfacing the mismatch. Rejects that shape
 * explicitly by checking for `ruleName`, the one field a `RuleResult` always
 * carries and a `PredicateOutcome` never does.
 */
function assertPredicateOutcome(outcome: unknown, ruleName: string): PredicateOutcome {
  if (
    typeof outcome !== "object" ||
    outcome === null ||
    typeof (outcome as { passed?: unknown }).passed !== "boolean" ||
    "ruleName" in outcome
  ) {
    throw new TypeError(
      `FunctionRule "${ruleName}": predicate must return a PredicateOutcome ` +
        `({ passed, detail?, data? }), not a RuleResult or any other shape`,
    );
  }
  return outcome as PredicateOutcome;
}

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

  /**
   * Runs the wrapped predicate and builds the {@link RuleResult} from
   * whatever it reports — this is the one place that owns `name`, never
   * the predicate itself.
   */
  async evaluate(context: TContext): Promise<RuleResult> {
    const outcome = assertPredicateOutcome(await this.#predicate(context), this.name);
    return new RuleResult(this.name, outcome.passed, { detail: outcome.detail, data: outcome.data });
  }

  /** @returns A one-line summary -- the name, and the group when set. */
  toString(): string {
    return `FunctionRule "${this.name}"` + (this.group ? ` (${this.group})` : "");
  }

  /** So `console.log`/the Node REPL show the same summary as {@link toString}. */
  [Symbol.for("nodejs.util.inspect.custom")](): string {
    return this.toString();
  }
}

/**
 * Signature of the decision a {@link SequentialEvaluator} delegates to:
 * given the latest sub-result, every sub-result gathered so far (including
 * the latest), and the total number of sub-rules, return `true`/`false` to
 * stop evaluating right now with that verdict, or `undefined` to keep
 * going. Invoked once per step (per sub-rule evaluated), not a one-shot
 * classifier of the whole run.
 *
 * Not parameterized by `TContext` — it only ever touches `RuleResult`/
 * counts, never the evaluation context itself; {@link SequentialEvaluator.evaluate}
 * carries its own `TContext` type parameter instead.
 */
export type StepDecider = (
  latest: RuleResult,
  soFar: readonly RuleResult[],
  total: number,
) => boolean | undefined;

/**
 * Evaluates a list of sub-rules sequentially against one context, letting
 * `decider` choose when to stop. Composable -- hold one as a field and
 * delegate to it, the same way {@link FunctionRule} holds a predicate.
 *
 * No base class anywhere in this package's composite support: every
 * dependency here is a visible, local constructor argument, and this piece
 * is unit-testable in isolation, independent of `AndRule`/`OrRule`/any
 * specific composite existing at all.
 */
export class SequentialEvaluator {
  readonly #decider: StepDecider;
  readonly #vacuousResult: boolean;

  /**
   * @param decider - Called once per sub-rule evaluated — see {@link StepDecider}.
   * @param vacuousResult - What to report when `rules` is empty, or when
   *   `decider` never resolves to anything but `undefined` even after
   *   every sub-rule has been evaluated.
   */
  constructor(decider: StepDecider, vacuousResult: boolean) {
    this.#decider = decider;
    this.#vacuousResult = vacuousResult;
  }

  /**
   * Evaluate `rules` in order against `context`, stopping when `decider`
   * says to.
   *
   * @param name - The name the resulting `RuleResult` carries — the
   *   composite's own name, not any sub-rule's.
   * @param rules - Sub-rules evaluated in order until `decider` decides, or
   *   the list is exhausted.
   */
  async evaluate<TContext>(
    name: string,
    rules: readonly Rule<TContext>[],
    context: TContext,
  ): Promise<RuleResult> {
    // Checked before the loop, not derived from a post-loop fallback
    // indexing the last evaluated result -- an empty list has no last
    // element, so this must be an independent branch, not a special case
    // of the one below.
    if (rules.length === 0) {
      return new RuleResult(name, this.#vacuousResult, { subResults: [] });
    }

    const soFar: RuleResult[] = [];
    for (const rule of rules) {
      const latest = await rule.evaluate(context);
      soFar.push(latest);
      const early = this.#decider(latest, soFar, rules.length);
      if (early !== undefined) {
        // Generic default, correct for a custom decider with no simpler
        // shortcut available: decided with items still unevaluated ->
        // just the one sub-result that flipped it; decided only once
        // every item has been seen -> all of them. Provably wrong for
        // ShortCircuitEvaluator specifically, which overrides this below
        // using the one extra fact (its own stopOn) a fully generic
        // decider doesn't have access to -- see ShortCircuitEvaluator.evaluate.
        const decidedByIndices =
          soFar.length === rules.length ? soFar.map((_, index) => index) : [soFar.length - 1];
        return new RuleResult(name, early, { subResults: soFar, decidedByIndices });
      }
    }
    // Non-null: `rules.length === 0` already returned above, so the loop
    // ran at least once and `soFar` is never empty here.
    const lastEvaluated = soFar[soFar.length - 1]!;
    const final = this.#decider(lastEvaluated, soFar, rules.length);
    return new RuleResult(name, final ?? this.#vacuousResult, {
      subResults: soFar,
      decidedByIndices: soFar.map((_, index) => index),
    });
  }
}

/**
 * Stops at the first sub-rule whose own `passed` equals `stopOn` -- the
 * shape {@link AndRule} and {@link OrRule} both are. Wraps
 * {@link SequentialEvaluator} internally; callers never need to know that
 * type exists unless they need more than this covers.
 */
export class ShortCircuitEvaluator {
  readonly #inner: SequentialEvaluator;
  readonly #stopOn: boolean;

  /**
   * @param stopOn - The sub-result `passed` value that ends evaluation
   *   immediately with that same verdict -- `false` for `AndRule` (stop on
   *   the first failure), `true` for `OrRule` (stop on the first pass).
   *
   * Only `stopOn` -- unlike `SequentialEvaluator`, where `vacuousResult` is
   * a genuinely independent fact, here the exhaustion case already
   * resolves to `!stopOn`, and an empty list is just that same exhaustion
   * with zero iterations; a second, independently-set argument could only
   * ever restate that fact correctly or contradict it, never add real
   * information.
   */
  constructor(stopOn: boolean) {
    this.#stopOn = stopOn;
    this.#inner = new SequentialEvaluator(
      (latest, soFar, total) => {
        if (latest.passed === stopOn) return stopOn;
        if (soFar.length === total) return !stopOn;
        return undefined;
      },
      !stopOn,
    );
  }

  /**
   * See {@link SequentialEvaluator.evaluate} -- same result, except
   * `decidedBy` is recomputed here rather than trusting
   * {@link SequentialEvaluator}'s own generic rule.
   *
   * That generic rule can't distinguish "found the trigger, which
   * happened to be the last item evaluated" from "genuinely exhausted
   * every item without ever finding it" using count alone -- an `AndRule`
   * failing on its *last* sub-rule has `subResults.length === total`
   * exactly like a genuine full pass does. `stopOn` is the one extra fact
   * that tells them apart: the trigger was found if and only if the last
   * evaluated sub-result's own `passed` equals `stopOn`, regardless of
   * where in the list it landed.
   */
  async evaluate<TContext>(
    name: string,
    rules: readonly Rule<TContext>[],
    context: TContext,
  ): Promise<RuleResult> {
    const result = await this.#inner.evaluate(name, rules, context);
    const total = result.subResults.length;
    const last = total > 0 ? result.subResults[total - 1] : undefined;
    const triggered = last !== undefined && last.passed === this.#stopOn;
    const decidedByIndices = triggered ? [total - 1] : result.subResults.map((_, index) => index);
    // Constructed, not spread: a spread of a `RuleResult` produces a plain
    // object, which is neither frozen nor a `RuleResult`.
    return new RuleResult(result.ruleName, result.passed, {
      detail: result.detail,
      data: result.data,
      subResults: result.subResults,
      decidedByIndices,
    });
  }
}

// One shared instance each -- TContext is erased at runtime, and
// ShortCircuitEvaluator/SequentialEvaluator's own `evaluate` is generic per
// call, not per instance, so every AndRule/OrRule regardless of its own
// TContext safely shares these two. (C#'s reference sketch gives each
// concrete `AndRule<TContext>` instantiation its own `private static
// readonly` evaluator field -- TypeScript's generics are erased, and a
// `static` class member can never reference the class's own type
// parameter, so that per-instantiation-static trick has no TS equivalent;
// a single module-level instance is both the closest match and, since
// nothing about evaluation actually depends on TContext, strictly cheaper.)
const AND_EVALUATOR = new ShortCircuitEvaluator(false);
const OR_EVALUATOR = new ShortCircuitEvaluator(true);

/**
 * Composite that passes only if every sub-rule passes.
 *
 * Short-circuits on the first failing sub-rule, by composing a single,
 * shared {@link ShortCircuitEvaluator} -- construction syntax, `toString()`,
 * and type identity are unchanged from before this class held an evaluator
 * instead of a hand-rolled loop.
 *
 * An empty list passes vacuously.
 *
 * Every sub-rule must share the exact same `TContext`.
 */
export class AndRule<TContext> implements Rule<TContext> {
  /** Pinned fact: `new AndRule("x", []).evaluate(...)` always passes. */
  static readonly VACUOUS_RESULT = true;

  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule<TContext>[];

  constructor(name: string, rules: readonly Rule<TContext>[], group?: string) {
    this.name = name;
    this.group = group;
    // Copied, not aliased: a caller retaining the array it passed could
    // otherwise change this composite's sub-rules, and its verdict,
    // after construction.
    this.#rules = [...rules];
  }

  evaluate(context: TContext): Promise<RuleResult> {
    return AND_EVALUATOR.evaluate(this.name, this.#rules, context);
  }

  /** @returns A one-line summary -- the name, group, and sub-rule count. */
  toString(): string {
    return (
      `AndRule "${this.name}"` +
      (this.group ? ` (${this.group})` : "") +
      ` — ${this.#rules.length} sub-rule(s)`
    );
  }

  /** So `console.log`/the Node REPL show the same summary as {@link toString}. */
  [Symbol.for("nodejs.util.inspect.custom")](): string {
    return this.toString();
  }
}

/**
 * Composite that passes as soon as any sub-rule passes.
 *
 * Short-circuits on the first passing sub-rule, by composing a single,
 * shared {@link ShortCircuitEvaluator}.
 *
 * An empty list fails vacuously. The same same-`TContext` requirement
 * across sub-rules applies here too.
 */
export class OrRule<TContext> implements Rule<TContext> {
  /** Pinned fact: `new OrRule("x", []).evaluate(...)` always fails. */
  static readonly VACUOUS_RESULT = false;

  readonly name: string;
  readonly group: string | undefined;
  readonly #rules: readonly Rule<TContext>[];

  constructor(name: string, rules: readonly Rule<TContext>[], group?: string) {
    this.name = name;
    this.group = group;
    // Copied, not aliased: a caller retaining the array it passed could
    // otherwise change this composite's sub-rules, and its verdict,
    // after construction.
    this.#rules = [...rules];
  }

  evaluate(context: TContext): Promise<RuleResult> {
    return OR_EVALUATOR.evaluate(this.name, this.#rules, context);
  }

  /** @returns A one-line summary -- the name, group, and sub-rule count. */
  toString(): string {
    return (
      `OrRule "${this.name}"` +
      (this.group ? ` (${this.group})` : "") +
      ` — ${this.#rules.length} sub-rule(s)`
    );
  }

  /** So `console.log`/the Node REPL show the same summary as {@link toString}. */
  [Symbol.for("nodejs.util.inspect.custom")](): string {
    return this.toString();
  }
}

/**
 * Composite that passes exactly when its one wrapped rule fails.
 *
 * No `SequentialEvaluator`/`ShortCircuitEvaluator` composed in -- one
 * child, no sequence to iterate, so that machinery would be indirection for
 * nothing it uses.
 */
export class NotRule<TContext> implements Rule<TContext> {
  readonly name: string;
  readonly group: string | undefined;
  readonly #rule: Rule<TContext>;

  constructor(name: string, rule: Rule<TContext>, group?: string) {
    this.name = name;
    this.group = group;
    this.#rule = rule;
  }

  /**
   * Evaluate the wrapped rule and invert its verdict.
   *
   * `subResults` is the one-element `[inner]`, truthfully -- never
   * flattened away. An empty `subResults` has to mean *only* "this is a
   * leaf," never also "this is a composite hiding its own structure" --
   * that guarantee is what makes `leaves`/`failingLeaves` safe to call on
   * any `RuleResult` at all, so `NotRule` doesn't get to special-case it
   * away just because a failed `NotRule`'s own `failingLeaves` can
   * otherwise read as misleadingly empty (the cause is a pass, not a
   * failure). `decidedBy` is the one inner result unconditionally, in both
   * directions -- correct either way, since "inner passed" is genuinely
   * why a failing `NotRule` failed, not an inconsistency.
   */
  async evaluate(context: TContext): Promise<RuleResult> {
    const inner = await this.#rule.evaluate(context);
    return new RuleResult(this.name, !inner.passed, { subResults: [inner], decidedByIndices: [0] });
  }

  /** @returns A one-line summary -- the name, and the group when set. */
  toString(): string {
    return `NotRule "${this.name}"` + (this.group ? ` (${this.group})` : "");
  }

  /** So `console.log`/the Node REPL show the same summary as {@link toString}. */
  [Symbol.for("nodejs.util.inspect.custom")](): string {
    return this.toString();
  }
}
