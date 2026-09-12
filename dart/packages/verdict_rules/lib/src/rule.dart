import 'result.dart';

/// The contract every rule satisfies.
///
/// Declared `abstract interface class` rather than `abstract class`
/// deliberately: consumers implement it, they never extend it. Allowing
/// extension would expose the fragile base class problem, where an internal
/// call to another method on `this` can land in a consumer's override. The
/// `interface` modifier forbids that while leaving `implements Rule` open to
/// anyone.
///
/// Dart *does* have structural typing — for function types. Any function
/// matching [RulePredicate] is a rule through [FunctionRule], with nothing
/// declared and no type to name; a tear-off works directly, as
/// `FunctionRule('quorum', hasQuorum)`. That is the same shape-is-enough
/// property Python's `Protocol` gives, applied to functions rather than to
/// whole objects.
///
/// What Dart lacks is structural typing for a *multi-member* interface. An
/// object that happens to carry `name`, `group` and `evaluate` is not thereby
/// a [Rule] — a rule shape owning its own name and group must say
/// `implements Rule`, where Python and TypeScript would accept it as-is. That
/// narrow difference is why [FunctionRule] carries more weight here: it is the
/// escape hatch back to shape-based rules, and most rules should use it.
abstract interface class Rule {
  /// Unique identifier for this rule, used for engine lookups and to attribute
  /// a [RuleResult] back to its source.
  String get name;

  /// Optional group label. Rules sharing one can be run together.
  String? get group;

  /// Evaluate this rule against [context].
  Future<RuleResult> evaluate(Map<String, Object?> context);
}

/// Signature of the predicate [FunctionRule] wraps.
typedef RulePredicate = Future<RuleResult> Function(
    Map<String, Object?> context);

/// Wraps a plain async predicate as a [Rule].
///
/// The shape most rules should be: no new class, no ceremony.
class FunctionRule implements Rule {
  @override
  final String name;

  @override
  final String? group;

  final RulePredicate _predicate;

  FunctionRule(this.name, RulePredicate predicate, {this.group})
      : _predicate = predicate;

  /// Runs the wrapped predicate and returns whatever it returns, unchanged.
  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) =>
      _predicate(context);
}

/// Composite that passes only if every sub-rule passes.
///
/// Short-circuits on the first failing sub-rule. Later sub-rules are never
/// evaluated once one has failed, so a caller can rely on this never doing
/// more work — or having more side effects — than the minimum needed to reach
/// a verdict.
///
/// An empty list **passes** vacuously: nothing to fail on, and the identity of
/// the fold it performs. That is the opposite polarity to [OrRule], which is
/// deliberate and easy to get backwards.
class AndRule implements Rule {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule> _rules;

  AndRule(this.name, List<Rule> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async {
    final subResults = <RuleResult>[];
    // A plain sequential loop, never Future.wait: short-circuiting only means
    // something if later work never *starts*, and any concurrent scheduling
    // would already have begun every sub-rule before the first result returns.
    // The returned boolean is identical either way, so this breaks silently.
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
/// Short-circuits on the first passing sub-rule.
///
/// An empty list **fails** vacuously: nothing to pass on. The opposite of
/// [AndRule], and the asymmetry is the point.
class OrRule implements Rule {
  @override
  final String name;

  @override
  final String? group;

  final List<Rule> _rules;

  OrRule(this.name, List<Rule> rules, {this.group}) : _rules = rules;

  @override
  Future<RuleResult> evaluate(Map<String, Object?> context) async {
    final subResults = <RuleResult>[];
    // Sequential, for the same reason as AndRule.
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
