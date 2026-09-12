import 'result.dart';
import 'rule.dart';

/// Holds a set of rules and answers questions about them.
///
/// Distinct from a composite: a composite returns one verdict and stops early,
/// whereas the engine's run modes are diagnostic and never short-circuit. They
/// exist to produce a full picture, not the fastest path to one boolean.
class RulesEngine {
  final List<Rule> _rules;
  final Map<String, Rule> _byName;
  final Map<String, List<Rule>> _byGroup;

  RulesEngine(List<Rule> rules)
      : _rules = List.unmodifiable(rules),
        _byName = {for (final r in rules) r.name: r},
        _byGroup = _indexByGroup(rules);

  static Map<String, List<Rule>> _indexByGroup(List<Rule> rules) {
    final byGroup = <String, List<Rule>>{};
    for (final rule in rules) {
      final group = rule.group;
      if (group != null && group.isNotEmpty) {
        byGroup.putIfAbsent(group, () => <Rule>[]).add(rule);
      }
    }
    return byGroup;
  }

  /// Every rule name registered here, in registration order.
  ///
  /// Exactly the names [runNamed] accepts, so a caller who cannot know in
  /// advance whether a rule exists can check rather than catch.
  Iterable<String> get ruleNames => _byName.keys;

  /// Every group label carried by at least one rule, in first-seen order.
  ///
  /// Exactly the labels [runGroup] accepts. A group is present only because
  /// some rule declared it.
  Iterable<String> get groupNames => _byGroup.keys;

  /// Evaluate every registered rule. Never short-circuits.
  Future<RunResult> runAll(Map<String, Object?> context) async {
    final results = <RuleResult>[];
    for (final rule in _rules) {
      results.add(await rule.evaluate(context));
    }
    return RunResult(
      passed: results.every((r) => r.passed),
      results: results,
    );
  }

  /// Evaluate one rule by name, or return null if no such rule exists.
  ///
  /// This is the primitive; [runNamed] is a two-line assertion on top of it.
  /// The distinction matters when absence is an expected, legitimate state
  /// rather than a mistake — a rule set that varies per tenant, an optional
  /// group behind a feature flag, a name carried in configuration a given
  /// deployment has not adopted yet.
  ///
  /// In those cases the caller decides what absence means, because the engine
  /// cannot: for one consumer a missing rule means "nothing to enforce, pass",
  /// for another "skip this and do not count it", for a third "the
  /// configuration is wrong, fail loudly". A single library default would be
  /// right for one of them and wrong for the rest.
  ///
  /// Null means *absent*, never *failed* — a rule that exists and fails still
  /// returns a [RuleResult] with `passed` false.
  Future<RuleResult?> tryRunNamed(
      String name, Map<String, Object?> context) async {
    final rule = _byName[name];
    if (rule == null) return null;
    return rule.evaluate(context);
  }

  /// Evaluate exactly one rule, looked up by name.
  ///
  /// The strict form, and the one to reach for by default: if a name is not
  /// expected to be absent, an absent name is a bug worth hearing about
  /// immediately. Use [tryRunNamed] when absence is a state your own domain
  /// has an answer for.
  ///
  /// Throws [ArgumentError] if no rule has this name.
  Future<RuleResult> runNamed(
      String name, Map<String, Object?> context) async {
    final result = await tryRunNamed(name, context);
    if (result == null) {
      throw ArgumentError.value(name, 'name', 'No rule with this name');
    }
    return result;
  }

  /// Evaluate a group, or return null if no such group exists.
  ///
  /// This is the primitive; [runGroup] is a two-line assertion on top of it.
  /// See [tryRunNamed] for when reaching for it is right — the short version
  /// is that the engine cannot know whether an absent group means "no
  /// constraint applies here" or "the configuration is broken", and only the
  /// caller can.
  ///
  /// Null means *absent*, never *vacuously passed*. A group exists only
  /// because some rule declared it, so an empty-but-real group is not
  /// representable, and a lookup matching nothing can only be a typo or a
  /// stale name. Returning a passing [RunResult] here would mean a misspelled
  /// group silently approves.
  Future<RunResult?> tryRunGroup(
      String group, Map<String, Object?> context) async {
    final rules = _byGroup[group];
    if (rules == null || rules.isEmpty) return null;
    final results = <RuleResult>[];
    for (final rule in rules) {
      results.add(await rule.evaluate(context));
    }
    return RunResult(
      passed: results.every((r) => r.passed),
      results: results,
    );
  }

  /// Evaluate every rule sharing a group label. Never short-circuits.
  ///
  /// The strict form, and the one to reach for by default. Use [tryRunGroup]
  /// when absence is a state your own domain has an answer for. This is the
  /// one place the package is strict: emptiness — a set you were handed that
  /// happened to be empty — still folds to its identity; absence is an error.
  ///
  /// Throws [ArgumentError] if no rule carries this label.
  Future<RunResult> runGroup(
      String group, Map<String, Object?> context) async {
    final result = await tryRunGroup(group, context);
    if (result == null) {
      throw ArgumentError.value(group, 'group', 'No rules in this group');
    }
    return result;
  }
}
