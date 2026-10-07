/** Result types returned by rule evaluation. */

/** The context passed to every rule. Opaque to verdict itself. */
export type Context = Record<string, unknown>;

/** Optional fields a {@link RuleResult} constructor call can set. */
export interface RuleResultInit {
  // `| undefined` alongside `?:`, not just `?:` alone -- under this
  // package's own `exactOptionalPropertyTypes`, a caller forwarding a
  // `PredicateOutcome`'s own optional `detail`/`data` (itself possibly
  // `undefined`) needs to be allowed to pass that `undefined` through
  // explicitly, not just omit the key entirely.
  /** See {@link RuleResult.detail}. Defaults to the empty string. */
  readonly detail?: string | undefined;

  /** See {@link RuleResult.data}. */
  readonly data?: unknown;

  /** See {@link RuleResult.subResults}. Defaults to empty, marking a leaf. */
  readonly subResults?: readonly RuleResult[] | undefined;

  /** See {@link RuleResult.decidedByIndices}. Defaults to empty. */
  readonly decidedByIndices?: readonly number[] | undefined;
}

/**
 * Outcome of evaluating a single rule.
 *
 * A real class with a public constructor, not a plain interface: a
 * structural shape would let a caller hand-build an object whose
 * `leaves`/`failingLeaves` disagree with its own `subResults`, and the
 * type checker would accept it. Here those two are derived, so there is
 * nothing to disagree with — the same guarantee every other SDK gets
 * from its own constructor/dataclass.
 */
export class RuleResult {
  /** Name of the rule this result came from, matching that rule's own `name`. */
  readonly ruleName: string;

  /** Whether the rule's condition was satisfied. */
  readonly passed: boolean;

  /**
   * Human-readable explanation — usually why a rule failed. Empty when
   * there is nothing to say beyond the boolean.
   *
   * `AndRule`/`OrRule`/`NotRule` never set this — composing
   * {@link ShortCircuitEvaluator}/{@link SequentialEvaluator} leaves no
   * channel richer than a boolean to build a descriptive string from. Read
   * {@link RuleResult.failingLeaves} (or {@link RuleResult.decidedBy}) for a
   * composite's own reason instead.
   */
  readonly detail: string;

  /**
   * Optional payload a caller can attach; genuinely opaque to this
   * package — never read or written by verdict itself. A composite's own
   * children live in {@link RuleResult.subResults} instead, never here.
   */
  readonly data: unknown;

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
   * Positions within {@link RuleResult.subResults} of the children that
   * explain this result's own verdict. Read
   * {@link RuleResult.decidedBy} instead unless building a result by hand.
   *
   * Positions rather than the child results themselves, so that the stored
   * object graph is a genuine tree. Holding the same children under two
   * fields makes it a DAG, and every tree-shaped walk — `JSON.stringify`, a
   * structured logger — expands a shared node once per path, so serialized
   * size doubles per nesting level.
   */
  readonly decidedByIndices: readonly number[];

  constructor(ruleName: string, passed: boolean, init: RuleResultInit = {}) {
    this.ruleName = ruleName;
    this.passed = passed;
    this.detail = init.detail ?? "";
    this.data = init.data;
    // Copied, not aliased: Object.freeze below seals this instance, not
    // an array the caller passed and kept. Without the copy, appending
    // the result to the very array it was built from makes it contain
    // itself, and every traversal here recurses through that.
    this.subResults = [...(init.subResults ?? [])];
    this.decidedByIndices = [...(init.decidedByIndices ?? [])];

    // An index naming a child that does not exist is the one way this shape
    // can be wrong, so it is rejected here rather than left to produce
    // `undefined` entries from whichever caller reads `decidedBy` first.
    const outOfRange = this.decidedByIndices.filter(
      (index) => !Number.isInteger(index) || index < 0 || index >= this.subResults.length,
    );
    if (outOfRange.length > 0) {
      throw new RangeError(
        `decidedByIndices [${outOfRange.join(", ")}] out of range for ` +
          `${this.subResults.length} subResults on rule "${ruleName}"`,
      );
    }

    Object.freeze(this);
  }

  /**
   * Which of {@link RuleResult.subResults} explain *this* result's own
   * verdict — fixed by whatever built this result, the same principle a
   * predicate's own `PredicateOutcome` already follows: the producer records
   * the fact because only the producer knows it unambiguously. Empty for a
   * leaf or a vacuous composite — nothing else decided it.
   *
   * **Scope, read carefully**: this is a one-level, non-recursive
   * question — "which immediate child (or children) explain this node's
   * own verdict" — not the same question {@link RuleResult.failingLeaves}
   * answers ("recursively, what are the terminal failures"). Do not chain
   * `result.decidedBy[0]!.decidedBy[0]!...` expecting it to converge on
   * the same answer as `failingLeaves` — it doesn't: a failed `NotRule`'s
   * `decidedBy` is its one *passing* inner child (correct for "why did
   * this fail"), and walking further from there drifts straight into "why
   * did the inner rule pass," a different question with no relationship
   * to the original failure.
   */
  get decidedBy(): readonly RuleResult[] {
    return this.decidedByIndices.map((index) => this.subResults[index]!);
  }

  /**
   * Every leaf result reachable from this one, in evaluation order — this
   * result itself when it has no {@link RuleResult.subResults}.
   *
   * Computed on access rather than stored, which is what keeps the object
   * graph acyclic: a leaf's own leaves list is `[this]`, so storing it
   * would put a reference to the result inside the result, and any
   * tree-shaped traversal of that structure — `JSON.stringify`, a
   * structured logger, a reflection-based mapper — would recurse until it
   * gave up.
   *
   * **Each leaf's own `passed` is its own outcome, not a contribution to
   * this result's verdict.** A `NotRule` above a leaf inverts it, and an
   * `OrRule` can pass despite a failed branch, so rendering a checklist
   * straight from `leaves` can show every item green on a result that
   * failed. Use {@link RuleResult.failingLeaves} to explain a verdict;
   * use this to enumerate what ran.
   */
  get leaves(): readonly RuleResult[] {
    return this.subResults.length === 0
      ? [this]
      : this.subResults.flatMap((sub) => sub.leaves);
  }

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
  get failingLeaves(): readonly RuleResult[] {
    if (this.passed) return [];
    const childFailures = this.subResults.flatMap((sub) => sub.failingLeaves);
    return childFailures.length === 0 ? [this] : childFailures;
  }
}

/** Aggregate outcome of running a whole set of rules through the engine. */
export class RunResult {
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

  constructor(results: readonly RuleResult[]) {
    this.passed = results.every((r) => r.passed);
    this.results = [...results];
    Object.freeze(this);
  }

  /**
   * Every leaf across every rule this run evaluated, flattened, in
   * evaluation order. A one-line forwarder to each result's own
   * {@link RuleResult.leaves}.
   */
  get leaves(): readonly RuleResult[] {
    return this.results.flatMap((r) => r.leaves);
  }

  /**
   * Every failing leaf across every rule this run evaluated.
   *
   * A forwarder to each result's own {@link RuleResult.failingLeaves}, **not**
   * a filter over {@link RunResult.leaves}. Filtering disagrees with the
   * per-result answer in both directions, because a result's verdict is not a
   * function of its leaves' verdicts:
   *
   * - A failed `NotRule` wraps a child that *passed*, so it is its own
   *   failing leaf. Filtering finds a passing leaf and reports no failure on
   *   a failed run.
   * - A passed `OrRule` can hold a failed branch it recovered from. Filtering
   *   reports that branch as a failure on a passing run.
   */
  get failingLeaves(): readonly RuleResult[] {
    return this.results.flatMap((r) => r.failingLeaves);
  }
}
