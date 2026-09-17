/// A small, zero-dependency, async-native rule-evaluation engine.
///
/// Compose independently-changing conditions into one explainable pass/fail
/// verdict. Evaluation is sequential and never concurrent, which is what makes
/// short-circuiting a real contract rather than a best-effort optimisation.
///
/// `Rule<TContext>`/`RulesEngine<TContext>` are generic over the context
/// they read from. Dict-context is `Rule<Context>`, written out explicitly
/// -- `Context` is exported below as the `Map<String, Object?>` alias.
///
/// ```dart
/// final overEighteen = FunctionRule<Context>('over_18', (ctx) async =>
///     RuleResult(ruleName: 'over_18', passed: (ctx['age'] as int) >= 18));
///
/// final verdict = await AndRule<Context>('eligible', [overEighteen]).evaluate({'age': 21});
/// print(verdict.passed); // true
/// ```
library;

export 'src/engine.dart' show RulesEngine;
export 'src/result.dart' show RuleResult, RunResult;
export 'src/rule.dart'
    show AndRule, Context, FunctionRule, OrRule, Rule, RulePredicate;
