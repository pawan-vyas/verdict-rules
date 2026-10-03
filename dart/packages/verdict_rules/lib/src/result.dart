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

  /// Optional payload; genuinely opaque to this package -- never read or
  /// written by verdict itself. A composite rule's own children live in
  /// [subResults] instead, never here.
  final Object? data;

  /// This result's own children, in evaluation order. Empty for a leaf
  /// result -- absence of any sub-result *is* the leaf signal, the same
  /// idiom `Rule.group` already uses for "none" on this type's sibling
  /// types. A composite rule (`AndRule`, `OrRule`, `NotRule`, or a custom
  /// composite built the same way) populates this with exactly the
  /// sub-results it actually evaluated -- never padded, never flattened.
  final List<RuleResult> subResults;

  /// Which of [subResults] explain *this* result's own verdict -- a
  /// one-level, non-recursive fact, populated once by whatever built this
  /// result (the same principle [PredicateOutcome] already follows: the
  /// producer records the fact because only the producer knows it
  /// unambiguously). Empty for a leaf or a vacuous composite.
  ///
  /// This is **not** the same question [failingLeaves] answers.
  /// [failingLeaves] recurses to the terminal failures; [decidedBy] stops
  /// after one level and says nothing about pass/fail polarity -- a
  /// passing [AndRule] has every sub-result in [decidedBy], not none. Do
  /// not chain `result.decidedBy[0].decidedBy[0]...` expecting it to land
  /// on the same thing [failingLeaves] would: it breaks for a failed
  /// `NotRule`, whose [decidedBy] is its *passing* inner child -- correct
  /// for "what explains this verdict," wrong for "what failed."
  final List<RuleResult> decidedBy;

  /// Not a `const` constructor: copying the collections requires a call,
  /// which a const initializer cannot make. Const construction is given up
  /// deliberately -- a const result would have to alias the caller's list.
  RuleResult({
    required this.ruleName,
    required this.passed,
    this.detail = '',
    this.data,
    List<RuleResult> subResults = const [],
    List<RuleResult> decidedBy = const [],
  })  : subResults = List.unmodifiable(subResults),
        decidedBy = List.unmodifiable(decidedBy);

  /// Every leaf result reachable from this one, in evaluation order --
  /// this result itself when it has no sub-results.
  List<RuleResult> get leaves => subResults.isEmpty
      ? [this]
      : [for (final sub in subResults) ...sub.leaves];

  /// Every failing leaf that contributed to this result's own failure.
  ///
  /// Empty when this result passed -- even when an earlier, short-circuited
  /// branch on the way to that pass itself failed. A failed result with no
  /// failing children is itself the leaf (this is what lets a failed
  /// `NotRule` -- whose single child actually passed -- report correctly
  /// here, rather than misleadingly reporting no failing leaves at all on a
  /// failed result). This is an independent recursion, not a filter over
  /// [leaves].
  List<RuleResult> get failingLeaves {
    if (passed) return const [];
    final childFailures = [
      for (final sub in subResults) ...sub.failingLeaves,
    ];
    return childFailures.isEmpty ? [this] : childFailures;
  }

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
  /// A composite's own sub-results live in that rule's own
  /// [RuleResult.subResults], not flattened into this list -- see [leaves]
  /// for the flattened view across every rule this run evaluated.
  final List<RuleResult> results;

  RunResult({required this.passed, List<RuleResult> results = const []})
      : results = List.unmodifiable(results);

  /// Every leaf across every rule this run evaluated, flattened, in
  /// evaluation order. One-line forwarder over each result's own
  /// [RuleResult.leaves].
  List<RuleResult> get leaves => [for (final r in results) ...r.leaves];

  /// Every leaf in [leaves] that failed.
  List<RuleResult> get failingLeaves => [
        for (final l in leaves)
          if (!l.passed) l
      ];

  @override
  String toString() => 'RunResult(passed: $passed, results: ${results.length})';
}
