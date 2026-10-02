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
   *
   * `AndRule`/`OrRule`/`NotRule` never set this — composing
   * {@link ShortCircuitEvaluator}/{@link SequentialEvaluator} leaves no
   * channel richer than a boolean to build a descriptive string from. Read
   * {@link RuleResult.failingLeaves} (or `AndRule.failed`/`OrRule.passed`/
   * `NotRule.negated`) for a composite's own reason instead.
   */
  readonly detail?: string;

  /**
   * Optional payload a caller can attach; genuinely opaque to this
   * package — never read or written by verdict itself. A composite's own
   * children live in {@link RuleResult.subResults} instead, never here.
   */
  readonly data?: unknown;

  /**
   * This result's own children, in evaluation order. Empty for a leaf
   * result — absence of any sub-result *is* the leaf signal, the same
   * idiom `Rule.group`'s `string | undefined` already uses for "none" on
   * a sibling type. A composite rule (`AndRule`, `OrRule`, `NotRule`, or a
   * custom composite built on {@link SequentialEvaluator}) populates this
   * with exactly the sub-results it actually evaluated — never padded,
   * never flattened.
   */
  readonly subResults: readonly RuleResult[];

  /**
   * Every leaf result reachable from this one, in evaluation order — this
   * result itself when it has no {@link RuleResult.subResults}.
   */
  readonly leaves: readonly RuleResult[];

  /**
   * Every failing leaf that contributed to this result's own failure.
   *
   * An independent recursion, **not** a filter over {@link RuleResult.leaves}:
   * empty when this result passed — even when an earlier, short-circuited
   * branch on the way to that pass itself failed. A failed result with no
   * failing children is itself the leaf — this is what lets a failed
   * `NotRule` (whose single child actually passed) report correctly here,
   * rather than misleadingly reporting no failing leaves at all on a
   * failed result.
   */
  readonly failingLeaves: readonly RuleResult[];
}

/** This result's own leaves, computed purely from `subResults`/`passed`. */
function computeLeaves(result: RuleResult): readonly RuleResult[] {
  return result.subResults.length === 0 ? [result] : result.subResults.flatMap(computeLeaves);
}

/** This result's own failing leaves, computed purely from `subResults`/`passed`. */
function computeFailingLeaves(result: RuleResult): readonly RuleResult[] {
  if (result.passed) return [];
  const childFailures = result.subResults.flatMap(computeFailingLeaves);
  return childFailures.length === 0 ? [result] : childFailures;
}

/** Optional fields a {@link buildRuleResult} caller can set. */
export interface RuleResultInit {
  // `| undefined` alongside `?:`, not just `?:` alone -- under this
  // package's own `exactOptionalPropertyTypes`, a caller forwarding a
  // `PredicateOutcome`'s own optional `detail`/`data` (itself possibly
  // `undefined`) needs to be allowed to pass that `undefined` through
  // explicitly, not just omit the key entirely.
  readonly detail?: string | undefined;
  readonly data?: unknown;
  readonly subResults?: readonly RuleResult[] | undefined;
}

/**
 * Builds a {@link RuleResult}, deriving `leaves`/`failingLeaves` from
 * `subResults` so every library-constructed result gets them for free.
 *
 * Internal to this package — every shipped `Rule` (`FunctionRule`,
 * `AndRule`, `OrRule`, `NotRule`, and anything composing
 * `SequentialEvaluator`/`ShortCircuitEvaluator`) goes through this rather
 * than building a `RuleResult` object literal by hand, the same way every
 * language's own `RuleResult` constructor/dataclass computes these two
 * properties from its own `SubResults`.
 */
export function buildRuleResult(
  ruleName: string,
  passed: boolean,
  init: RuleResultInit = {},
): RuleResult {
  const result = {
    ruleName,
    passed,
    detail: init.detail ?? "",
    data: init.data,
    subResults: init.subResults ?? [],
    leaves: [] as readonly RuleResult[],
    failingLeaves: [] as readonly RuleResult[],
  };
  // Reassigned below, not supplied up front -- computeLeaves/computeFailingLeaves
  // only ever read `.passed`/`.subResults`, both already final above, so this
  // embeds a genuine self-reference (the final object, not a transient draft)
  // in the leaf case, and `assert.deepEqual(leaf.leaves, [leaf])`-shaped
  // assertions hold by identity, not by coincidental structural equality.
  result.leaves = computeLeaves(result);
  result.failingLeaves = computeFailingLeaves(result);
  return Object.freeze(result);
}

/** Aggregate outcome of running a whole set of rules through the engine. */
export interface RunResult {
  /** True only if every rule in {@link RunResult.results} passed. */
  readonly passed: boolean;

  /**
   * One {@link RuleResult} per rule evaluated, in evaluation order. A
   * composite's own sub-results live in that rule's own
   * {@link RuleResult.subResults}, not flattened into this list — see
   * {@link RunResult.leaves} for the flattened view across every rule this
   * run evaluated.
   */
  readonly results: readonly RuleResult[];

  /**
   * Every leaf across every rule this run evaluated, flattened, in
   * evaluation order. A one-line forwarder to each result's own
   * {@link RuleResult.leaves}.
   */
  readonly leaves: readonly RuleResult[];

  /**
   * Every failing leaf across every rule this run evaluated.
   *
   * Unlike {@link RuleResult.failingLeaves}, this *is* a plain filter over
   * {@link RunResult.leaves} — `runAll`/`runGroup` never short-circuit, so
   * every top-level result's own verdict is already final; there is no
   * earlier short-circuited branch here to misrepresent.
   */
  readonly failingLeaves: readonly RuleResult[];
}

/**
 * Builds a {@link RunResult} from the per-rule results `RulesEngine`
 * gathered, deriving `passed`/`leaves`/`failingLeaves`.
 *
 * Internal to this package.
 */
export function buildRunResult(results: readonly RuleResult[]): RunResult {
  const leaves = results.flatMap(computeLeaves);
  return {
    passed: results.every((r) => r.passed),
    results,
    leaves,
    failingLeaves: leaves.filter((l) => !l.passed),
  };
}
