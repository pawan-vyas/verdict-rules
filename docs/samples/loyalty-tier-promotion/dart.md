<!-- Title: Sample — Loyalty Tier Promotion (Dart) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation ends up as *two* functions that have to
be kept in sync by hand — one for the yes/no decision, one for the
customer-facing checklist:

```dart
class Customer {
  final num trailing12moSpend;
  final num goldSpendThreshold;
  final num trailing12moOrders;
  final num goldOrderThreshold;
  final num returnRate;
  final num goldMaxReturnRate;
  final String accountStatus;

  Customer({
    required this.trailing12moSpend,
    required this.goldSpendThreshold,
    required this.trailing12moOrders,
    required this.goldOrderThreshold,
    required this.returnRate,
    required this.goldMaxReturnRate,
    required this.accountStatus,
  });
}

Map<String, bool> goldChecklist(Customer customer) => {
      'spend': customer.trailing12moSpend >= customer.goldSpendThreshold,
      'orders': customer.trailing12moOrders >= customer.goldOrderThreshold,
      'returns': customer.returnRate <= customer.goldMaxReturnRate,
      'standing': customer.accountStatus == 'active',
    };

bool isEligibleForGold(Customer customer) =>
    goldChecklist(customer).values.every((v) => v);
```

Nothing enforces the coupling between the two functions — adding a
fifth requirement to one without the other produces a promotion
decision the UI's own checklist can't explain. See the spec for the
rest of what this shape gets wrong.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<RuleResult> meetsSpendThreshold(Map<String, Object?> context) async {
  final passed = (context['trailing12moSpend']! as num) >=
      (context['goldSpendThreshold']! as num);
  return RuleResult(ruleName: 'meets_spend_threshold', passed: passed);
}

Future<RuleResult> meetsOrderCount(Map<String, Object?> context) async {
  final passed = (context['trailing12moOrders']! as num) >=
      (context['goldOrderThreshold']! as num);
  return RuleResult(ruleName: 'meets_order_count', passed: passed);
}

Future<RuleResult> returnRateBelowMax(Map<String, Object?> context) async {
  final passed = (context['returnRate']! as num) <=
      (context['goldMaxReturnRate']! as num);
  return RuleResult(ruleName: 'return_rate_below_max', passed: passed);
}

Future<RuleResult> accountInGoodStanding(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'account_in_good_standing',
      passed: context['accountStatus'] == 'active',
    );

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so runAll() reports every criterion's own
// outcome, with no short-circuiting hiding a later criterion's result.
final engine = RulesEngine([
  FunctionRule('meets_spend_threshold', meetsSpendThreshold),
  FunctionRule('meets_order_count', meetsOrderCount),
  FunctionRule('return_rate_below_max', returnRateBelowMax),
  FunctionRule('account_in_good_standing', accountInGoodStanding),
]);

Future<RunResult> promotionChecklist(Map<String, Object?> customerContext) async {
  final result = await engine.runAll(customerContext);
  // result.passed is true only if all four passed -- the actual promotion decision.
  // result.results is one RuleResult per criterion, always all four -- the UI checklist.
  return result;
}
```

This is the one case where reaching for a bare `AndRule` and reaching
for the engine's `runAll()` produce genuinely different, both-correct
answers depending on what the caller actually needs — see
[`../../architecture/dart.md`](../../architecture/dart.md#three-ways-to-run-rules-concretely)
for the general rule of thumb.

A customer meeting three of the four criteria — the same case the
spec's own diagram shows:

```dart
final customer = {
  'trailing12moSpend': 6000,
  'goldSpendThreshold': 5000,
  'trailing12moOrders': 20,
  'goldOrderThreshold': 15,
  'returnRate': 0.08,
  'goldMaxReturnRate': 0.05,
  'accountStatus': 'active',
};

final checklist = await promotionChecklist(customer);
checklist.passed;
// false -- not promoted, the return rate is over the limit
checklist.results.map((r) => [r.ruleName, r.passed]).toList();
// [ [meets_spend_threshold, true], [meets_order_count, true],
//   [return_rate_below_max, false], [account_in_good_standing, true] ]
// all four report, not just the one that decided the outcome
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`content-moderation-routing/dart.md`](../content-moderation-routing/dart.md) —
  another `run*` (this time `runGroup()`) example, for when a single
  engine needs to serve more than one independent decision.
