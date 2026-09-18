/// Result types returned by rule evaluation.
library;

/// Outcome of evaluating a single rule.
class RuleResult {
  /// Name of the rule this result came from, matching that rule's own `name`.
  final String ruleName;

  /// Whether the rule's condition was satisfied.
  final bool passed;

  /// Optional human-readable explanation. Empty when there is nothing
  /// beyond the boolean.
  final String detail;

  /// Optional payload; opaque to this package.
  ///
  /// For composites this holds the sub-results gathered so far.
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
  /// A composite's own sub-results are nested inside its
  /// [RuleResult.data] rather than flattened into this list.
  final List<RuleResult> results;

  const RunResult({required this.passed, this.results = const []});

  @override
  String toString() => 'RunResult(passed: $passed, results: ${results.length})';
}
