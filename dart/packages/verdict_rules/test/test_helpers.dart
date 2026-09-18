import 'package:verdict_rules/verdict_rules.dart';

/// A rule that always passes, mirroring Python's own `_pass` test helper.
FunctionRule pass(String name, {String? group, Object? data}) => FunctionRule(
      name,
      (ctx) async => RuleResult(ruleName: name, passed: true, data: data),
      group: group,
    );

/// A rule that always fails, mirroring Python's own `_fail` test helper.
FunctionRule failing(String name, {String? group, String detail = ''}) =>
    FunctionRule(
      name,
      (ctx) async => RuleResult(ruleName: name, passed: false, detail: detail),
      group: group,
    );

/// A rule that records every evaluation, so short-circuiting can be proven by
/// what actually ran rather than by the final boolean alone. A port that
/// evaluated concurrently would return the same boolean and fail only here.
FunctionRule counting(
  String name,
  bool passes,
  List<String> log, {
  String? group,
}) =>
    FunctionRule(name, (ctx) async {
      log.add(name);
      return RuleResult(ruleName: name, passed: passes);
    }, group: group);
