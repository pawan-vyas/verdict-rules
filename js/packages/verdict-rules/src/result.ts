/**
 * Result types returned by rule evaluation.
 *
 * Deliberately plain, immutable data — a rule reports what happened and
 * nothing more. Any domain-specific payload a caller wants to carry alongside
 * the pass/fail outcome rides in {@link RuleResult.data}, which this package
 * treats as fully opaque: verdict never inspects or depends on its shape, and
 * that is what keeps the engine reusable across unrelated domains.
 */

/** The context passed to every rule. Opaque to verdict itself. */
export type Context = Record<string, unknown>;

/** Outcome of evaluating a single rule. */
export interface RuleResult {
  /**
   * Name of the rule this result came from, matching that rule's own `name`,
   * so a caller walking a {@link RunResult} can attribute each outcome back to
   * the rule that produced it.
   */
  readonly ruleName: string;

  /** Whether the rule's condition was satisfied. */
  readonly passed: boolean;

  /**
   * Optional human-readable explanation — usually why a rule failed. Empty
   * when there is nothing worth saying beyond the boolean.
   */
  readonly detail?: string;

  /**
   * Optional, fully opaque payload. Verdict never reads it.
   *
   * For composites this holds the sub-results gathered so far — only the ones
   * that actually ran, never padded out to the full list, and never flattened
   * into the parent's own level.
   */
  readonly data?: unknown;
}

/** Aggregate outcome of running a whole set of rules through the engine. */
export interface RunResult {
  /** True only if every rule in {@link RunResult.results} passed. */
  readonly passed: boolean;

  /**
   * One {@link RuleResult} per rule evaluated, in evaluation order.
   *
   * A short-circuited composite still contributes exactly one entry here for
   * itself; its own sub-results are nested inside its `data` rather than
   * flattened into this list.
   */
  readonly results: readonly RuleResult[];
}
