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

/// Signature of the predicate [FunctionRule] wraps.
typedef RulePredicate<TContext> = Future<RuleResult> Function(TContext context);

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

  /// Runs the wrapped predicate and returns whatever it returns, unchanged.
  @override
  Future<RuleResult> evaluate(TContext context) => _predicate(context);
}

/// Composite that passes only if every sub-rule passes.
///
/// Short-circuits on the first failing sub-rule. An empty list passes
/// vacuously.
///
/// Every sub-rule must be a [Rule] of the exact same `TContext`.
class AndRule<TContext> implements Rule<TContext> {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule<TContext>> _rules;

  AndRule(this.name, List<Rule<TContext>> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(TContext context) async {
    final subResults = <RuleResult>[];
    // Sequential, not Future.wait.
    for (final rule in _rules) {
      final result = await rule.evaluate(context);
      subResults.add(result);
      if (!result.passed) {
        final reason = result.detail.isEmpty
            ? "'${rule.name}' failed"
            : "'${rule.name}' failed: ${result.detail}";
        return RuleResult(
          ruleName: name,
          passed: false,
          detail: reason,
          data: subResults,
        );
      }
    }
    return RuleResult(ruleName: name, passed: true, data: subResults);
  }
}

/// Composite that passes as soon as any sub-rule passes.
///
/// Short-circuits on the first passing sub-rule. An empty list fails
/// vacuously. The same same-`TContext` requirement across sub-rules
/// applies here too.
class OrRule<TContext> implements Rule<TContext> {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule<TContext>> _rules;

  OrRule(this.name, List<Rule<TContext>> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(TContext context) async {
    final subResults = <RuleResult>[];
    // Sequential, not Future.wait.
    for (final rule in _rules) {
      final result = await rule.evaluate(context);
      subResults.add(result);
      if (result.passed) {
        return RuleResult(ruleName: name, passed: true, data: subResults);
      }
    }
    return RuleResult(
      ruleName: name,
      passed: false,
      detail: 'no sub-rule passed',
      data: subResults,
    );
  }
}
