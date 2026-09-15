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

FunctionRule makeRule(RuleConfig ruleConfig) {
  return FunctionRule(ruleConfig.name, (context) async {
    final actual = context[ruleConfig.field];
    final passed = actual == ruleConfig.expected;
    return RuleResult(ruleName: ruleConfig.name, passed: passed);
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
// RuleResult(ruleName: combined, passed: true, ...)

await combinedRule.evaluate({'role': 'manager', 'office': 'Remote'});
// RuleResult(ruleName: combined, passed: false, ...) -- in_headquarters fails
```

An empty `loadRuleConfigs()` produces an empty `AndRule`, which
vacuously passes.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
- [`../../samples/data-driven-rule-sets/dart.md`](../../samples/data-driven-rule-sets/dart.md) —
  the fuller worked version, in Dart.
