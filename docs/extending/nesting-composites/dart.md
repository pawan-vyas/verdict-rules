<!-- Title: Extending — Nesting Composites Arbitrarily (Dart) -->
# Nesting composites arbitrarily: Dart

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Dart code.

```dart
import 'package:verdict_rules/verdict_rules.dart';

class AccountContext {
  final String accountStatus;
  final bool isPremiumMember;
  final String? promoCode;
  final num spend;
  final num spendThreshold;

  AccountContext({
    required this.accountStatus,
    required this.isPremiumMember,
    this.promoCode,
    required this.spend,
    required this.spendThreshold,
  });
}

Future<RuleResult> isActiveAccount(AccountContext context) async =>
    RuleResult(
      ruleName: 'is_active_account',
      passed: context.accountStatus == 'active',
    );

Future<RuleResult> isPremiumMember(AccountContext context) async =>
    RuleResult(
      ruleName: 'is_premium_member',
      passed: context.isPremiumMember,
    );

Future<RuleResult> hasPromoCode(AccountContext context) async => RuleResult(
      ruleName: 'has_promo_code',
      passed: context.promoCode != null,
    );

Future<RuleResult> meetsSpendThreshold(AccountContext context) async =>
    RuleResult(
      ruleName: 'meets_spend_threshold',
      passed: context.spend >= context.spendThreshold,
    );

// Nesting doesn't care what built its sub-rules -- each of the four leaves
// here is a plain FunctionRule, but any Rule (a custom shape, another
// composite) would compose exactly the same way.
final qualifies = AndRule<AccountContext>('qualifies', [
  FunctionRule('is_active_account', isActiveAccount),
  OrRule<AccountContext>('has_a_valid_reason', [
    FunctionRule('is_premium_member', isPremiumMember),
    FunctionRule('has_promo_code', hasPromoCode),
    FunctionRule('meets_spend_threshold', meetsSpendThreshold),
  ]),
]);
```

```dart
final context = AccountContext(
  accountStatus: 'active',
  isPremiumMember: false,
  promoCode: 'SAVE10',
  spend: 20,
  spendThreshold: 100,
);
final result = await qualifies.evaluate(context);
result.passed;
// true
result.data;
// [ RuleResult(ruleName: is_active_account, passed: true),
//   RuleResult(ruleName: has_a_valid_reason, passed: true, data:
//     [ RuleResult(ruleName: is_premium_member, passed: false),
//       RuleResult(ruleName: has_promo_code, passed: true) ]) ]
// meetsSpendThreshold never ran -- has_a_valid_reason short-circuited
// once has_promo_code passed, exactly as a plain, unnested OrRule would
```

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
