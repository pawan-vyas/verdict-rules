import 'result.dart';

/// The context passed to every rule when no more specific type is declared.
typedef Context = Map<String, Object?>;

/// The contract every rule satisfies, generic over the context it reads from.
///
/// Declared `abstract interface class`: consumers implement it, they never
/// extend it.
abstract interface class Rule<TContext> {
  /// Unique identifier for this rule, used for engine lookups and to attribute
  /// a [RuleResult] back to its source.
  String get name;

  /// Optional group label. Rules sharing one can be run together.
  String? get group;

  /// Evaluate this rule against [context].
  Future<RuleResult> evaluate(TContext context);
}

/// What a [RulePredicate] reports back to the [FunctionRule] wrapping it.
///
/// Deliberately carries no name -- a predicate has no legitimate reason to
/// restate a name that is already fixed, once, on the [FunctionRule]
/// wrapping it; nothing else ever attributes a [RuleResult] to the wrong
/// rule. Carries no `group` either, and never will: if [RuleResult] ever
/// grows a `group` field, it stays exclusively sourced from
/// [FunctionRule.group], the same way `name` already is, rather than
/// reopening a path for a predicate to diverge on it.
class PredicateOutcome {
  /// Whether the predicate's own condition was satisfied.
  final bool passed;

  /// Optional human-readable explanation. Empty when there is nothing
  /// beyond the boolean.
  final String detail;

  /// Optional payload; opaque to this package.
  final Object? data;

  const PredicateOutcome(this.passed, {this.detail = '', this.data});

  @override
  String toString() => detail.isEmpty
      ? (passed ? 'PASS' : 'FAIL')
      : '${passed ? 'PASS' : 'FAIL'} ($detail)';
}

/// Signature of the predicate [FunctionRule] wraps.
///
/// Reports a [PredicateOutcome] -- never a [RuleResult] directly. The
/// predicate has no way to construct a [RuleResult] itself, which is what
/// keeps `FunctionRule.name` the single source of truth for the result's
/// own `ruleName`; see [PredicateOutcome] for why.
typedef RulePredicate<TContext> = Future<PredicateOutcome> Function(
    TContext context);

/// Wraps a plain async predicate as a [Rule].
///
/// `TContext` is inferred from the wrapped predicate's own parameter type.
class FunctionRule<TContext> implements Rule<TContext> {
  @override
  final String name;

  @override
  final String? group;

  final RulePredicate<TContext> _predicate;

  FunctionRule(this.name, RulePredicate<TContext> predicate, {this.group})
      : _predicate = predicate;

  /// Runs the wrapped predicate and builds this rule's own result.
  ///
  /// No longer a straight pass-through -- the predicate only reports a
  /// [PredicateOutcome], so this is the one place that owns [name], built
  /// fresh from it on every call.
  @override
  Future<RuleResult> evaluate(TContext context) async {
    final outcome = await _predicate(context);
    return RuleResult(
      ruleName: name,
      passed: outcome.passed,
      detail: outcome.detail,
      data: outcome.data,
    );
  }

  @override
  String toString() =>
      group == null ? 'FunctionRule "$name"' : 'FunctionRule "$name" ($group)';
}

/// Decides, after one more sub-rule has been evaluated, whether a
/// [SequentialEvaluator] should stop.
///
/// Invoked once per step -- once per sub-rule evaluated -- never as a
/// one-shot classifier of the whole run. Not parameterized by any context
/// type: it only ever reads [RuleResult]/counts, never the evaluation
/// context itself.
///
/// Returns `true`/`false` to stop now with that verdict, or `null` to keep
/// evaluating the next sub-rule.
typedef StepDecider = bool? Function(
  RuleResult latest,
  List<RuleResult> soFar,
  int total,
);

/// Evaluates a list of sub-rules sequentially against one context, letting
/// a [StepDecider] choose when to stop.
///
/// Composable -- hold one as a field and delegate to it, the same way
/// [FunctionRule] holds a predicate. This is the one true sequential,
/// [RuleResult.subResults]-correct implementation in this package;
/// [ShortCircuitEvaluator] is built on top of it rather than reimplementing
/// it, and a custom composite should do the same rather than hand-rolling
/// its own loop.
class SequentialEvaluator<TContext> {
  final StepDecider _decider;
  final bool _vacuousResult;

  /// - [decider]: called once per sub-rule evaluated, see [StepDecider].
  /// - [vacuousResult]: what to report when `rules` is empty, or when
  ///   [decider] never resolves to anything but `null` even after every
  ///   sub-rule has been evaluated. Independent of [decider] -- not every
  ///   decider's own exhaustion behavior generalizes to the zero-iteration
  ///   case (a fixed threshold like "at least N" does not derive this from
  ///   any single fact the decider closes over).
  SequentialEvaluator(
      {required StepDecider decider, required bool vacuousResult})
      : _decider = decider,
        _vacuousResult = vacuousResult;

  /// Evaluates every rule in [rules], in order, against [context], stopping
  /// as soon as this evaluator's own [StepDecider] returns a non-null
  /// verdict.
  ///
  /// Returns a [RuleResult] named [name], whose [RuleResult.subResults] is
  /// exactly the sub-results actually produced -- every one of them when
  /// [rules] is exhausted without an early stop, or every one up to and
  /// including the sub-result that triggered an early stop.
  Future<RuleResult> evaluate(
    String name,
    List<Rule<TContext>> rules,
    TContext context,
  ) async {
    // Checked before the loop, not derived from a post-loop fallback
    // indexing the last evaluated result -- an empty list has no last
    // element, so this must be an independent branch, not a special case
    // of the one below.
    if (rules.isEmpty) {
      return RuleResult(ruleName: name, passed: _vacuousResult);
    }

    final soFar = <RuleResult>[];
    for (final rule in rules) {
      final latest = await rule.evaluate(context);
      soFar.add(latest);
      final early = _decider(latest, soFar, rules.length);
      if (early != null) {
        // Generic default, correct for a custom decider with no simpler
        // shortcut available: decided with items still unevaluated -> just
        // the one that flipped it; decided only once everything was seen
        // -> all of them. Provably wrong for ShortCircuitEvaluator
        // specifically, which overrides this below using the one extra
        // fact (stopOn) a fully generic decider has no access to.
        final decidedBy = soFar.length == rules.length
            ? List<RuleResult>.unmodifiable(soFar)
            : <RuleResult>[latest];
        return RuleResult(
          ruleName: name,
          passed: early,
          subResults: List.unmodifiable(soFar),
          decidedBy: decidedBy,
        );
      }
    }
    final decided = _decider(soFar.last, soFar, rules.length);
    return RuleResult(
      ruleName: name,
      passed: decided ?? _vacuousResult,
      subResults: List.unmodifiable(soFar),
      decidedBy: List.unmodifiable(soFar),
    );
  }

  @override
  String toString() => 'SequentialEvaluator<$TContext>';
}

/// Stops at the first sub-rule whose own [RuleResult.passed] equals
/// [stopOn] -- the shape [AndRule]/[OrRule] both are.
///
/// Wraps [SequentialEvaluator] internally; a caller never needs to know
/// that type exists unless it needs more than this covers (a
/// running-history-dependent decision, which this type's own decider
/// cannot express -- compose [SequentialEvaluator] directly for that
/// instead).
class ShortCircuitEvaluator<TContext> {
  final bool _stopOn;
  final SequentialEvaluator<TContext> _inner;

  /// [stopOn] is the [RuleResult.passed] value that ends evaluation early.
  /// The empty-list result (`!stopOn`) is not a second, independently-set
  /// argument -- it is the same fact the exhaustion case already computes,
  /// so stating it twice could only ever agree or silently contradict it.
  ShortCircuitEvaluator({required bool stopOn})
      : _stopOn = stopOn,
        _inner = SequentialEvaluator<TContext>(
          decider: (latest, soFar, total) {
            if (latest.passed == stopOn) return stopOn;
            return soFar.length == total ? !stopOn : null;
          },
          vacuousResult: !stopOn,
        );

  /// See [SequentialEvaluator.evaluate] -- same result, except [decidedBy]
  /// is recomputed here rather than trusting [SequentialEvaluator]'s own
  /// generic rule.
  ///
  /// That generic rule can't distinguish "found the trigger, which
  /// happened to be the last item evaluated" from "genuinely exhausted
  /// every item without ever finding it" using count alone -- an
  /// [AndRule] failing on its *last* sub-rule has
  /// `subResults.length == total` exactly like a genuine full pass does.
  /// [_stopOn] is the one extra fact that tells them apart.
  Future<RuleResult> evaluate(
    String name,
    List<Rule<TContext>> rules,
    TContext context,
  ) async {
    final result = await _inner.evaluate(name, rules, context);
    final last = result.subResults.isNotEmpty ? result.subResults.last : null;
    final decidedBy =
        last != null && last.passed == _stopOn ? [last] : result.subResults;
    return RuleResult(
      ruleName: result.ruleName,
      passed: result.passed,
      detail: result.detail,
      data: result.data,
      subResults: result.subResults,
      decidedBy: decidedBy,
    );
  }

  @override
  String toString() => 'ShortCircuitEvaluator<$TContext> (stopOn: $_stopOn)';
}

/// Composite that passes only if every sub-rule passes.
///
/// Short-circuits on the first failing sub-rule. An empty list passes
/// vacuously. Composes a single [ShortCircuitEvaluator] rather than
/// implementing evaluation itself -- see that type and [SequentialEvaluator]
/// for the one place short-circuiting/[RuleResult.subResults]/vacuous-truth
/// are actually implemented.
///
/// Every sub-rule must be a [Rule] of the exact same `TContext`.
class AndRule<TContext> implements Rule<TContext> {
  /// What an empty [AndRule] evaluates to -- pinned, not wired into
  /// construction ([ShortCircuitEvaluator] derives this internally from
  /// `stopOn`).
  static const bool vacuousResult = true;

  @override
  final String name;

  @override
  final String? group;

  final List<Rule<TContext>> _rules;

  /// The one true implementation this composite forwards to.
  ///
  /// An instance field, not a shared static one: Dart does not allow a
  /// generic class's static member to reference that class's own type
  /// parameter (unlike C#'s per-closed-type statics, or Python's
  /// erased-generics class attribute), so each [AndRule] instance builds
  /// its own. Harmless -- [ShortCircuitEvaluator] is stateless beyond the
  /// `decider` it closes over, so this costs one extra allocation per
  /// [AndRule] constructed, not a behavior difference.
  final ShortCircuitEvaluator<TContext> _evaluator =
      ShortCircuitEvaluator<TContext>(stopOn: false);

  AndRule(this.name, List<Rule<TContext>> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(TContext context) =>
      _evaluator.evaluate(name, _rules, context);

  @override
  String toString() {
    final groupSuffix = group == null ? '' : ' ($group)';
    return 'AndRule "$name"$groupSuffix — ${_rules.length} sub-rule(s)';
  }
}

/// Composite that passes as soon as any sub-rule passes.
///
/// Short-circuits on the first passing sub-rule. An empty list fails
/// vacuously. The same same-`TContext` requirement across sub-rules applies
/// here too. Composes a single [ShortCircuitEvaluator], the same way
/// [AndRule] does with the opposite `stopOn`.
class OrRule<TContext> implements Rule<TContext> {
  /// What an empty [OrRule] evaluates to -- pinned, not wired into
  /// construction. See [AndRule.vacuousResult].
  static const bool vacuousResult = false;

  @override
  final String name;

  @override
  final String? group;

  final List<Rule<TContext>> _rules;

  /// See [AndRule._evaluator] for why this is an instance field, not a
  /// shared static one.
  final ShortCircuitEvaluator<TContext> _evaluator =
      ShortCircuitEvaluator<TContext>(stopOn: true);

  OrRule(this.name, List<Rule<TContext>> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(TContext context) =>
      _evaluator.evaluate(name, _rules, context);

  @override
  String toString() {
    final groupSuffix = group == null ? '' : ' ($group)';
    return 'OrRule "$name"$groupSuffix — ${_rules.length} sub-rule(s)';
  }
}

/// Composite that passes exactly when its one wrapped rule fails.
///
/// No [SequentialEvaluator]/[ShortCircuitEvaluator] composed in -- one
/// child, no sequence to iterate, so that machinery would be indirection
/// for nothing it uses.
class NotRule<TContext> implements Rule<TContext> {
  @override
  final String name;

  @override
  final String? group;

  final Rule<TContext> _rule;

  NotRule(this.name, Rule<TContext> rule, {this.group}) : _rule = rule;

  /// Evaluates the wrapped rule and inverts its verdict.
  ///
  /// `subResults` is the one-element `[inner]`, truthfully -- never
  /// flattened away. An empty `subResults` has to mean *only* "this is a
  /// leaf," never also "this is a composite hiding its own structure" --
  /// that guarantee is what makes [RuleResult.leaves]/
  /// [RuleResult.failingLeaves] safe to call on any [RuleResult] at all, so
  /// [NotRule] doesn't get to special-case it away just because a failed
  /// [NotRule]'s own `failingLeaves` can otherwise read as misleadingly
  /// empty (the cause is a pass, not a failure). `decidedBy` is `[inner]`
  /// unconditionally, in both directions -- correct either way, since
  /// "inner passed" is genuinely why a failing [NotRule] failed, not an
  /// inconsistency.
  @override
  Future<RuleResult> evaluate(TContext context) async {
    final inner = await _rule.evaluate(context);
    return RuleResult(
      ruleName: name,
      passed: !inner.passed,
      subResults: [inner],
      decidedBy: [inner],
    );
  }

  @override
  String toString() =>
      group == null ? 'NotRule "$name"' : 'NotRule "$name" ($group)';
}
