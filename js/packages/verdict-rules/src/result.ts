/** Result types returned by rule evaluation. */

/** The context passed to every rule. Opaque to verdict itself. */
export type Context = Record<string, unknown>;

/** Outcome of evaluating a single rule. */
export interface RuleResult {
  /** Name of the rule this result came from, matching that rule's own `name`. */
  readonly ruleName: string;

  /** Whether the rule's condition was satisfied. */
  readonly passed: boolean;

  /**
   * Optional human-readable explanation — usually why a rule failed. Empty
   * when there is nothing to say beyond the boolean.
   */
  readonly detail?: string;

  /**
   * Optional payload a caller can attach; opaque to this package. For
   * composites this holds the sub-results gathered so far.
   */
  readonly data?: unknown;
}

/** Aggregate outcome of running a whole set of rules through the engine. */
export interface RunResult {
  /** True only if every rule in {@link RunResult.results} passed. */
  readonly passed: boolean;

  /**
   * One {@link RuleResult} per rule evaluated, in evaluation order. A
   * composite's own sub-results are nested inside its `data`.
   */
  readonly results: readonly RuleResult[];
}
