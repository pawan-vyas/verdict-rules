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

  /// Evaluate exactly one rule, looked up by name.
  ///
  /// Throws [ArgumentError] if no rule has this name.
  Future<RuleResult> runNamed(String name, Map<String, Object?> context) {
    final rule = _byName[name];
    if (rule == null) {
      throw ArgumentError.value(name, 'name', 'No rule with this name');
    }
    return rule.evaluate(context);
  }

  /// Evaluate every rule sharing a group label. Never short-circuits.
  ///
  /// Throws [ArgumentError] if no rule carries this label.
  ///
  /// An unknown group raises rather than returning a vacuous pass, matching
  /// [runNamed]. A group exists only because some rule declared it, so an
  /// empty-but-real group is not representable and a lookup matching nothing
  /// can only be a typo or a stale name. Returning a pass there would mean a
  /// misspelled group silently approves. Use [groupNames] to check first if a
  /// group may legitimately be absent.
  ///
  /// This is the one place the package is strict. Emptiness — a set you were
  /// handed that happened to be empty — still folds to its identity; absence
  /// is an error.
  Future<RunResult> runGroup(String group, Map<String, Object?> context) async {
    final rules = _byGroup[group];
    if (rules == null || rules.isEmpty) {
      throw ArgumentError.value(group, 'group', 'No rules in this group');
    }
    final results = <RuleResult>[];
    for (final rule in rules) {
      results.add(await rule.evaluate(context));
    }
    return RunResult(
      passed: results.every((r) => r.passed),
      results: results,
    );
  }
}
