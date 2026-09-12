/// Result types returned by rule evaluation.
///
/// Deliberately plain, immutable data — a [Rule] reports what happened and
/// nothing more. Any domain-specific payload a caller wants to carry alongside
/// the pass/fail outcome rides in [RuleResult.data], which this package treats
/// as fully opaque: verdict never inspects or depends on its shape, and that is
/// what keeps the engine reusable across unrelated domains.
library;

/// Outcome of evaluating a single rule.
class RuleResult {
  /// Name of the rule this result came from, matching that rule's own `name`,
  /// so a caller walking a [RunResult] can attribute each outcome back to the
  /// rule that produced it.
  final String ruleName;

  /// Whether the rule's condition was satisfied.
  final bool passed;

  /// Optional human-readable explanation — usually why a rule failed. Empty
  /// when there is nothing worth saying beyond the boolean.
  final String detail;

  /// Optional, fully opaque payload. Verdict never reads it.
  ///
  /// For composites this holds the sub-results gathered so far — only the ones
  /// that actually ran, never padded out to the full list, and never flattened
  /// into the parent's own level.
  final Object? data;

  const RuleResult({
    required this.ruleName,
    required this.passed,
    this.detail = '',
    this.data,
  });

  @override
  String toString() =>
      'RuleResult(ruleName: $ruleName, passed: $passed, detail: $detail)';
}

/// Aggregate outcome of running a whole set of rules through the engine.
class RunResult {
  /// True only if every rule in [results] passed.
  final bool passed;

  /// One [RuleResult] per rule evaluated, in evaluation order.
  ///
  /// A short-circuited composite still contributes exactly one entry here for
  /// itself; its own sub-results are nested inside its [RuleResult.data]
  /// rather than flattened into this list.
  final List<RuleResult> results;

  const RunResult({required this.passed, this.results = const []});

  @override
  String toString() =>
      'RunResult(passed: $passed, results: ${results.length})';
}
