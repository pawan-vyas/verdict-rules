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

  /// Positions within [subResults] of the children that explain *this*
  /// result's own verdict. Read [decidedBy] instead unless building a
  /// result by hand.
  ///
  /// Positions rather than the child results themselves, so that the stored
  /// object graph is a genuine tree. Holding the same children under two
  /// fields makes it a DAG, and every tree-shaped walk -- [toJson] feeding
  /// `jsonEncode`, a structured logger -- expands a shared node once per
  /// path, so serialized size doubles per nesting level.
  final List<int> decidedByIndices;

  /// Not a `const` constructor: copying the collections requires a call,
  /// which a const initializer cannot make, and a const result would
  /// otherwise have to alias the caller's list. Use [RuleResult.leaf] when
  /// there are no children to copy and a compile-time constant is wanted.
  RuleResult({
    required this.ruleName,
    required this.passed,
    this.detail = '',
    this.data,
    List<RuleResult> subResults = const [],
    List<int> decidedByIndices = const [],
  })  : subResults = List.unmodifiable(subResults),
        decidedByIndices = List.unmodifiable(decidedByIndices) {
    // An index naming a child this result does not have is the one way this
    // shape can be wrong, so it is rejected here rather than left to throw
    // a RangeError from whichever caller reads [decidedBy] first. The
    // objects form admitted no equivalent check: nothing stopped it naming
    // a result that was never a child of this one.
    final outOfRange = [
      for (final i in this.decidedByIndices)
        if (i < 0 || i >= this.subResults.length) i
    ];
    if (outOfRange.isNotEmpty) {
      throw ArgumentError.value(
        outOfRange,
        'decidedByIndices',
        'out of range for ${this.subResults.length} subResults '
            "on rule '$ruleName'",
      );
    }
  }

  /// A leaf result, as a compile-time constant.
  ///
  /// The general constructor cannot be `const`, because copying the caller's
  /// collections requires a call. A leaf has no children to copy, so the two
  /// collections are fixed empty and there is nothing left that would need
  /// one -- which also means there is no index that could be out of range,
  /// so this constructor needs no body and can therefore be `const` at all.
  ///
  /// Useful for a fixed result or a test fixture, and for the places Dart
  /// requires a constant expression rather than merely allowing one.
  const RuleResult.leaf({
    required this.ruleName,
    required this.passed,
    this.detail = '',
    this.data,
  })  : subResults = const [],
        decidedByIndices = const [];

  /// Which of [subResults] explain *this* result's own verdict -- a
  /// one-level, non-recursive fact, fixed by whatever built this result
  /// (the same principle [PredicateOutcome] already follows: the producer
  /// records the fact because only the producer knows it unambiguously).
  /// Empty for a leaf or a vacuous composite.
  ///
  /// This is **not** the same question [failingLeaves] answers.
  /// [failingLeaves] recurses to the terminal failures; [decidedBy] stops
  /// after one level and says nothing about pass/fail polarity -- a
  /// passing [AndRule] has every sub-result in [decidedBy], not none. Do
  /// not chain `result.decidedBy[0].decidedBy[0]...` expecting it to land
  /// on the same thing [failingLeaves] would: it breaks for a failed
  /// `NotRule`, whose deciding child actually *passed* -- correct for
  /// "what explains this verdict," wrong for "what failed."
  List<RuleResult> get decidedBy =>
      [for (final i in decidedByIndices) subResults[i]];

  /// Every leaf result reachable from this one, in evaluation order --
  /// this result itself when it has no sub-results.
  ///
  /// **Each leaf's own [passed] is its own outcome, not a contribution to
  /// this result's verdict.** A `NotRule` above a leaf inverts it, and an
  /// `OrRule` can pass despite a failed branch, so rendering a checklist
  /// straight from [leaves] can show every item green on a result that
  /// failed. Use [failingLeaves] to explain a verdict; use this to
  /// enumerate what ran.
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

  /// This result as a JSON-encodable map, ready for `jsonEncode`.
  ///
  /// The stored fields only -- [leaves], [failingLeaves] and [decidedBy]
  /// are derived from them at any time, and including [leaves] would place
  /// a leaf result inside itself, which `jsonEncode` cannot represent.
  /// [data] is passed through untouched: it is opaque to this package,
  /// which makes encoding it the caller's own responsibility.
  Map<String, dynamic> toJson() => {
        'ruleName': ruleName,
        'passed': passed,
        'detail': detail,
        'data': data,
        'subResults': [for (final sub in subResults) sub.toJson()],
        'decidedByIndices': decidedByIndices,
      };

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

  /// Every failing leaf across every rule this run evaluated.
  ///
  /// A forwarder to each result's own [RuleResult.failingLeaves], **not** a
  /// filter over [leaves]. Filtering disagrees with the per-result answer in
  /// both directions, because a result's verdict is not a function of its
  /// leaves' verdicts:
  ///
  /// - A failed `NotRule` wraps a child that *passed*, so it is its own
  ///   failing leaf. Filtering finds a passing leaf and reports no failure on
  ///   a failed run.
  /// - A passed `OrRule` can hold a failed branch it recovered from.
  ///   Filtering reports that branch as a failure on a passing run.
  List<RuleResult> get failingLeaves =>
      [for (final r in results) ...r.failingLeaves];

  /// This run as a JSON-encodable map, ready for `jsonEncode`. See
  /// [RuleResult.toJson].
  Map<String, dynamic> toJson() => {
        'passed': passed,
        'results': [for (final r in results) r.toJson()],
      };

  @override
  String toString() => 'RunResult(passed: $passed, results: ${results.length})';
}
