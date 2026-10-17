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

Future<PredicateOutcome> isActiveAccount(AccountContext context) async =>
    PredicateOutcome(context.accountStatus == 'active');

Future<PredicateOutcome> isPremiumMember(AccountContext context) async =>
    PredicateOutcome(context.isPremiumMember);

Future<PredicateOutcome> hasPromoCode(AccountContext context) async =>
    PredicateOutcome(context.promoCode != null);

Future<PredicateOutcome> meetsSpendThreshold(AccountContext context) async =>
    PredicateOutcome(context.spend >= context.spendThreshold);

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

A nested result is read the same way at every level — `subResults` holds
one level, and never the whole tree:

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

result.subResults.map((r) => r.ruleName).toList();
// [is_active_account, has_a_valid_reason]

final inner = result.subResults[1];
inner.subResults.map((r) => r.ruleName).toList();
// [is_premium_member, has_promo_code]
// meetsSpendThreshold never ran -- has_a_valid_reason short-circuited
// once has_promo_code passed, exactly as a plain, unnested OrRule would
```

Three views answer three different questions about the same tree:

```dart
result.leaves.map((r) => r.ruleName).toList();
// [is_active_account, is_premium_member, has_promo_code]
// fully recursive -- the terminal checks, however deep

result.failingLeaves;
// [] -- a passing result has none, even though is_premium_member failed
// on the way to the inner OrRule's pass

result.decidedBy.map((r) => r.ruleName).toList();
// [is_active_account, has_a_valid_reason]
// one level: an AndRule that had to evaluate everything is explained by
// everything

inner.decidedBy.map((r) => r.ruleName).toList();
// [has_promo_code]
// one level again, but an OrRule that stopped early is explained by just
// the sub-rule that stopped it
```

`data` is not part of this. It is an opaque slot for a caller's own
payload, never written to by a composite — a composite's children are in
`subResults`.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
