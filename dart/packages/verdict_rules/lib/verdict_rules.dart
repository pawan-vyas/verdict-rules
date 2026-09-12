/// A small, zero-dependency, async-native rule-evaluation engine.
///
/// Compose independently-changing conditions into one explainable pass/fail
/// verdict. Evaluation is sequential and never concurrent, which is what makes
/// short-circuiting a real contract rather than a best-effort optimisation.
///
/// ```dart
/// final overEighteen = FunctionRule('over_18', (ctx) async =>
///     RuleResult(ruleName: 'over_18', passed: (ctx['age'] as int) >= 18));
///
/// final verdict = await AndRule('eligible', [overEighteen]).evaluate({'age': 21});
/// print(verdict.passed); // true
/// ```
library;

export 'src/engine.dart' show RulesEngine;
export 'src/result.dart' show RuleResult, RunResult;
export 'src/rule.dart' show AndRule, FunctionRule, OrRule, Rule, RulePredicate;
