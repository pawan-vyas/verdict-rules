import 'result.dart';

/// The context passed to every rule when no more specific type is declared.
/// Opaque to verdict_rules itself, and exactly as first-class as any typed
/// `TContext` -- never a fallback for the untyped. Written out explicitly at
/// every `Rule<Context>` declaration, the same way `Rule<OrderContext>` names
/// its own type.
typedef Context = Map<String, Object?>;

/// The contract every rule satisfies, generic over the context it reads from.
///
/// Declared `abstract interface class` rather than `abstract class`
/// deliberately: consumers implement it, they never extend it. Allowing
/// extension would expose the fragile base class problem, where an internal
/// call to another method on `this` can land in a consumer's override. The
/// `interface` modifier forbids that while leaving `implements Rule<TContext>`
/// open to anyone.
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
/// `implements Rule<TContext>`, where Python and TypeScript would accept it
/// as-is. That narrow difference is why [FunctionRule] carries more weight
/// here: it is the escape hatch back to shape-based rules, and most rules
/// should use it.
///
/// `TContext` is this package's one real breaking migration from its
/// pre-generic form: an existing `implements Rule` declaration becomes
/// `implements Rule<Context>` (dict-context) or `implements Rule<TContext>`
/// (a typed context) explicitly. Constructor call sites
/// (`FunctionRule(...)`, `AndRule(...)`) are unaffected — `TContext` is
/// inferred there via ordinary Dart type inference, the same as before
/// generics existed.
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
/// The shape most rules should be: no new class, no ceremony. `TContext` is
/// inferred from the wrapped predicate's own parameter type, so
/// `FunctionRule('x', predicate)` rarely needs an explicit type argument at
/// the call site as long as `predicate` itself is typed.
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
/// Short-circuits on the first failing sub-rule. Later sub-rules are never
/// evaluated once one has failed, so a caller can rely on this never doing
/// more work — or having more side effects — than the minimum needed to reach
/// a verdict.
///
/// An empty list **passes** vacuously: nothing to fail on, and the identity of
/// the fold it performs. That is the opposite polarity to [OrRule], which is
/// deliberate and easy to get backwards.
///
/// Every sub-rule must be a [Rule] of the exact same `TContext` — the
/// analyzer enforces this once construction names a type argument. Reusing
/// one rule across two differently-shaped contexts goes through an explicit
/// projecting adapter (see docs/extending/reusing-a-rule-across-contexts/)
/// rather than loosening this constraint.
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
/// [AndRule], and the asymmetry is the point. The same same-`TContext`
/// requirement across sub-rules applies here too; see [AndRule]'s own docs.
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
