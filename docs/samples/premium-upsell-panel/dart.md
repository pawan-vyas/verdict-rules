<!-- Title: Sample — Premium Upsell Panel (Dart) -->
# Sample: Premium Upsell Panel

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it, inside a Flutter
> `StatefulWidget` — `verdict_rules` is a plain Dart package, so it
> needs no Flutter-specific fork or adapter to use here.

## The naive way (and why it breaks down)

The obvious first implementation is a conditional ladder checked
straight in the render path, with the slow network call wherever it
happened to be added:

```dart
Future<bool> shouldShowUpsellPanel(User user) async {
  if (await checkWinbackOffer(user.id)) {
    return true;
  }
  if (user.planTier == 'free') {
    return true;
  }
  if (user.usageThisMonth >= user.usageCap) {
    return true;
  }
  return false;
}
```

The billing check happens to run first here, so it runs on **every
single page load** — including a free-tier user who qualifies on plan
alone and never needed the network round trip at all. See the spec for
what that costs beyond the wasted call itself.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

abstract interface class BillingService {
  Future<bool> checkWinbackEligibility(String userId);
}

Future<RuleResult> isFreeTier(Map<String, Object?> ctx) async => RuleResult(
      ruleName: 'is_free_tier',
      passed: ctx['planTier'] == 'free',
    );

Future<RuleResult> hasHitUsageCap(Map<String, Object?> ctx) async => RuleResult(
      ruleName: 'has_hit_usage_cap',
      passed: (ctx['usageThisMonth']! as num) >= (ctx['usageCap']! as num),
    );

Future<RuleResult> qualifiesForWinback(Map<String, Object?> ctx) async {
  // The expensive path: only reached if both cheaper checks above failed.
  final service = ctx['billingService']! as BillingService;
  final qualifies = await service.checkWinbackEligibility(ctx['userId']! as String);
  return RuleResult(ruleName: 'qualifies_for_winback', passed: qualifies);
}

final shouldShowUpsell = OrRule('should_show_upsell', [
  FunctionRule('is_free_tier', isFreeTier),
  FunctionRule('has_hit_usage_cap', hasHitUsageCap),
  FunctionRule('qualifies_for_winback', qualifiesForWinback), // cheapest-last, on purpose
]);

Future<bool> checkShouldShowUpsell(Map<String, Object?> context) async =>
    (await shouldShowUpsell.evaluate(context)).passed;
```

The comment on the last rule is the whole fix, made visible: cost order
is now a stated decision at the point it matters, not something the
next person to edit this file has to reconstruct from scratch.

The screen's own loading indicator wraps exactly the one `evaluate()`
call — never a second, separately-managed flag the widget has to keep
in sync with the check itself:

```dart
import 'package:flutter/material.dart';

class AccountScreen extends StatefulWidget {
  const AccountScreen({
    super.key,
    required this.planTier,
    required this.usageThisMonth,
    required this.usageCap,
    required this.userId,
    required this.billingService,
  });

  final String planTier;
  final num usageThisMonth;
  final num usageCap;
  final String userId;
  final BillingService billingService;

  @override
  State<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends State<AccountScreen> {
  bool _isChecking = true;
  bool _shouldShowUpsell = false;

  @override
  void initState() {
    super.initState();
    _updateUpsellPanel();
  }

  Future<void> _updateUpsellPanel() async {
    setState(() => _isChecking = true);
    final shouldShow = await checkShouldShowUpsell({
      'planTier': widget.planTier,
      'usageThisMonth': widget.usageThisMonth,
      'usageCap': widget.usageCap,
      'userId': widget.userId,
      'billingService': widget.billingService,
    });
    if (!mounted) return; // the widget may have been disposed mid-await
    setState(() {
      _isChecking = false;
      _shouldShowUpsell = shouldShow;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Your account')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_isChecking)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 8),
                child: Row(
                  children: [
                    SizedBox(
                      width: 16,
                      height: 16,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    ),
                    SizedBox(width: 8),
                    Text('Checking your account…'),
                  ],
                ),
              )
            else if (_shouldShowUpsell)
              const Card(
                child: Padding(
                  padding: EdgeInsets.all(16),
                  child: Text('Upgrade to Premium and unlock unlimited usage.'),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
```

Proving the saving is visible, not just theoretical — a delayed fake
stands in for the real billing service, and the widget test asserts on
the loading indicator itself rather than an internal call count:

```dart
class DelayedBillingService implements BillingService {
  DelayedBillingService(this.delay);
  final Duration delay;
  int calls = 0;

  @override
  Future<bool> checkWinbackEligibility(String userId) async {
    calls += 1;
    await Future<void>.delayed(delay);
    return userId == 'winback-eligible';
  }
}

testWidgets('free tier resolves without ever showing a loading indicator',
    (tester) async {
  final service = DelayedBillingService(const Duration(seconds: 1));
  await tester.pumpWidget(MaterialApp(
    home: AccountScreen(
      planTier: 'free',
      usageThisMonth: 0,
      usageCap: 100,
      userId: 'u1',
      billingService: service,
    ),
  ));

  // The cheap check resolves on the same microtask -- pump once (no
  // real time advance) and the loading indicator is already gone.
  await tester.pump();
  expect(find.byType(CircularProgressIndicator), findsNothing);
  expect(service.calls, 0);
});

testWidgets(
    'a user needing the network check sees the loading indicator until it resolves',
    (tester) async {
  final service = DelayedBillingService(const Duration(milliseconds: 500));
  await tester.pumpWidget(MaterialApp(
    home: AccountScreen(
      planTier: 'pro',
      usageThisMonth: 10,
      usageCap: 100,
      userId: 'winback-eligible',
      billingService: service,
    ),
  ));

  await tester.pump();
  expect(find.byType(CircularProgressIndicator), findsOneWidget);

  await tester.pump(const Duration(milliseconds: 500));
  expect(find.byType(CircularProgressIndicator), findsNothing);
  expect(service.calls, 1);
});
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`shipping-fee-waiver/dart.md`](../shipping-fee-waiver/dart.md) — the
  same `OrRule` shape in a backend checkout decision.
- [`signup-form-readiness/dart.md`](../signup-form-readiness/dart.md) —
  the `AndRule` mirror image, with no async branch.
