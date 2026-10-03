<!-- Title: Extending — Building Rule Sets From Stored Configuration (Dart) -->
# Building rule sets from stored configuration: Dart

> The concept and the testing implication are in
> [`README.md`](README.md) — read that first. This page is the concrete
> Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

class RuleConfig {
  final String name;
  final String field;
  final Object? expected;

  RuleConfig({required this.name, required this.field, required this.expected});
}

FunctionRule<Context> makeRule(RuleConfig ruleConfig) {
  return FunctionRule(ruleConfig.name, (context) async {
    final actual = context[ruleConfig.field];
    return PredicateOutcome(actual == ruleConfig.expected);
  });
}

/// Stands in for a real config source for this example.
List<RuleConfig> loadRuleConfigs() => [
      RuleConfig(name: 'is_manager', field: 'role', expected: 'manager'),
      RuleConfig(name: 'in_headquarters', field: 'office', expected: 'HQ'),
    ];

final configuredRules = loadRuleConfigs().map(makeRule).toList();
final combinedRule = AndRule('combined', configuredRules);
```

```dart
await combinedRule.evaluate({'role': 'manager', 'office': 'HQ'});
// passed: true

final refused =
    await combinedRule.evaluate({'role': 'manager', 'office': 'Remote'});
refused.failingLeaves.first.ruleName;
// in_headquarters -- the config-driven name, carried through
```

The rule name comes from the config once, at the `FunctionRule` call —
the predicate no longer repeats it, so a config whose name changes cannot
leave a result labelled with the old one.

An empty `loadRuleConfigs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
