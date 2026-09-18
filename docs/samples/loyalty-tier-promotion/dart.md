<!-- Title: Sample — Loyalty Tier Promotion (Dart) -->
# Sample: Loyalty Tier Promotion

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is *two* functions, one for the
customer-facing checklist and one for the actual yes/no decision, each
with the four criteria's thresholds written inline:

```dart
class Customer {
  final num trailing12moSpend;
  final num trailing12moOrders;
  final num returnRate;
  final String accountStatus;

  Customer({
    required this.trailing12moSpend,
    required this.trailing12moOrders,
    required this.returnRate,
    required this.accountStatus,
  });
}

Map<String, bool> goldChecklist(Customer customer) => {
      'spend': customer.trailing12moSpend >= 5000,
      'orders': customer.trailing12moOrders >= 15,
      'returns': customer.returnRate <= 0.05,
      'standing': customer.accountStatus == 'active',
    };

bool isEligibleForGold(Customer customer) =>
    customer.trailing12moSpend >= 5000 &&
    customer.trailing12moOrders >= 20 &&
    customer.returnRate <= 0.05 &&
    customer.accountStatus == 'active';
```

`goldChecklist` promotes at 15 orders; `isEligibleForGold` at 20 —
visible by reading the two functions side by side, not a hypothetical
future drift. See the spec for the rest of what this shape gets wrong.

## The `verdict` way

A typed context, not a `Map` — each threshold has exactly one
definition, read by name from every rule that needs it:

```dart
import 'package:verdict_rules/verdict_rules.dart';

class LoyaltyContext {
  final num trailing12moSpend;
  final num goldSpendThreshold;
  final num trailing12moOrders;
  final num goldOrderThreshold;
  final num returnRate;
  final num goldMaxReturnRate;
  final String accountStatus;

  LoyaltyContext({
    required this.trailing12moSpend,
    required this.goldSpendThreshold,
    required this.trailing12moOrders,
    required this.goldOrderThreshold,
    required this.returnRate,
    required this.goldMaxReturnRate,
    required this.accountStatus,
  });
}

Future<RuleResult> meetsSpendThreshold(LoyaltyContext context) async {
  final passed = context.trailing12moSpend >= context.goldSpendThreshold;
  return RuleResult(ruleName: 'meets_spend_threshold', passed: passed);
}

Future<RuleResult> meetsOrderCount(LoyaltyContext context) async {
  final passed = context.trailing12moOrders >= context.goldOrderThreshold;
  return RuleResult(ruleName: 'meets_order_count', passed: passed);
}

Future<RuleResult> returnRateBelowMax(LoyaltyContext context) async {
  final passed = context.returnRate <= context.goldMaxReturnRate;
  return RuleResult(ruleName: 'return_rate_below_max', passed: passed);
}

Future<RuleResult> accountInGoodStanding(LoyaltyContext context) async =>
    RuleResult(
      ruleName: 'account_in_good_standing',
      passed: context.accountStatus == 'active',
    );

// Registered as four independent named rules on one engine -- not nested
// in an AndRule -- precisely so runAll() reports every criterion's own
// outcome, with no short-circuiting hiding a later criterion's result.
final engine = RulesEngine<LoyaltyContext>([
  FunctionRule('meets_spend_threshold', meetsSpendThreshold),
  FunctionRule('meets_order_count', meetsOrderCount),
  FunctionRule('return_rate_below_max', returnRateBelowMax),
  FunctionRule('account_in_good_standing', accountInGoodStanding),
]);

Future<RunResult> promotionChecklist(LoyaltyContext context) async {
  final result = await engine.runAll(context);
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
final customer = LoyaltyContext(
  trailing12moSpend: 6000,
  goldSpendThreshold: 5000,
  trailing12moOrders: 20,
  goldOrderThreshold: 15,
  returnRate: 0.08,
  goldMaxReturnRate: 0.05,
  accountStatus: 'active',
);

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
