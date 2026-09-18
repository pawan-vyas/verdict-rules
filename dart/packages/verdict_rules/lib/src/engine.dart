import 'result.dart';
import 'rule.dart';

/// Holds a set of rules and answers questions about them.
///
/// The engine's run modes never short-circuit.
class RulesEngine<TContext> {
  final List<Rule<TContext>> _rules;
  final Map<String, Rule<TContext>> _byName;
  final Map<String, List<Rule<TContext>>> _byGroup;

  RulesEngine(List<Rule<TContext>> rules)
      : _rules = List.unmodifiable(rules),
        _byName = {for (final r in rules) r.name: r},
        _byGroup = _indexByGroup(rules);

  static Map<String, List<Rule<TContext>>> _indexByGroup<TContext>(
      List<Rule<TContext>> rules) {
    final byGroup = <String, List<Rule<TContext>>>{};
    for (final rule in rules) {
      final group = rule.group;
      if (group != null && group.isNotEmpty) {
        byGroup.putIfAbsent(group, () => <Rule<TContext>>[]).add(rule);
      }
    }
    return byGroup;
  }

  /// Every rule name registered here, in registration order.
  Iterable<String> get ruleNames => _byName.keys;

  /// Every group label carried by at least one rule, in first-seen order.
  Iterable<String> get groupNames => _byGroup.keys;

  /// Evaluate every registered rule. Never short-circuits.
  Future<RunResult> runAll(TContext context) async {
    final results = <RuleResult>[];
    for (final rule in _rules) {
      results.add(await rule.evaluate(context));
    }
    return RunResult(passed: results.every((r) => r.passed), results: results);
  }

  /// Evaluate one rule by name, or return null if no such rule exists.
  ///
  /// Null means absent, never failed.
  Future<RuleResult?> tryRunNamed(
    String name,
    TContext context,
  ) async {
    final rule = _byName[name];
    if (rule == null) return null;
    return rule.evaluate(context);
  }

  /// Evaluate exactly one rule, looked up by name.
  ///
  /// Throws [ArgumentError] if no rule has this name.
  Future<RuleResult> runNamed(String name, TContext context) async {
    final result = await tryRunNamed(name, context);
    if (result == null) {
      throw ArgumentError.value(name, 'name', 'No rule with this name');
    }
    return result;
  }

  /// Evaluate a group, or return null if no such group exists.
  ///
  /// Null means absent, never vacuously passed.
  Future<RunResult?> tryRunGroup(
    String group,
    TContext context,
  ) async {
    final rules = _byGroup[group];
    if (rules == null || rules.isEmpty) return null;
    final results = <RuleResult>[];
    for (final rule in rules) {
      results.add(await rule.evaluate(context));
    }
    return RunResult(passed: results.every((r) => r.passed), results: results);
  }

  /// Evaluate every rule sharing a group label. Never short-circuits.
  ///
  /// Throws [ArgumentError] if no rule carries this label.
  Future<RunResult> runGroup(String group, TContext context) async {
    final result = await tryRunGroup(group, context);
    if (result == null) {
      throw ArgumentError.value(group, 'group', 'No rules in this group');
    }
    return result;
  }
}
