/// A small, zero-dependency, async-native rule-evaluation engine.
///
/// `Rule<TContext>`/`RulesEngine<TContext>` are generic over the context
/// they read from. Dict-context is `Rule<Context>`, written out explicitly
/// -- `Context` is exported below as the `Map<String, Object?>` alias.
///
/// ```dart
/// final overEighteen = FunctionRule<Context>('over_18', (ctx) async =>
///     PredicateOutcome((ctx['age'] as int) >= 18));
///
/// final verdict = await AndRule<Context>('eligible', [overEighteen]).evaluate({'age': 21});
/// print(verdict.passed); // true
/// ```
library;

export 'src/engine.dart' show RulesEngine;
export 'src/result.dart' show RuleResult, RunResult;
export 'src/rule.dart'
    show
        AndRule,
        CompositeRule,
        Context,
        FunctionRule,
        NotRule,
        OrRule,
        PredicateOutcome,
        Rule,
        RulePredicate,
        SequentialEvaluator,
        ShortCircuitEvaluator,
        StepDecider;
