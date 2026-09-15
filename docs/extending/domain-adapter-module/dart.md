<!-- Title: Extending — Keep Your Own Domain Out Of Verdict (Dart) -->
# Keep your own domain out of verdict: Dart

> The concept and the diagram are in [`README.md`](README.md) — read
> that first. This page is the concrete Dart code, illustrating a
> rate-limiting adapter (one of the two illustrations the spec names),
> and — since the point of the boundary is that `verdict_rules` itself
> is replaceable behind it — a second implementation of the identical
> contract that doesn't use `verdict_rules` at all.

```dart
import 'package:verdict_rules/verdict_rules.dart';

/// This adapter's own domain type -- verdict never sees it directly,
/// only hands it back as RuleResult.data's opaque payload.
class RateLimitStatus {
  final String window;
  final int used;
  final int quota;

  RateLimitStatus({required this.window, required this.used, required this.quota});

  @override
  String toString() => 'RateLimitStatus(window: $window, used: $used, quota: $quota)';
}

/// The contract every call site depends on. Nothing here mentions
/// verdict_rules -- an `abstract interface class` so it's implemented,
/// never extended, the same reasoning Rule itself uses.
abstract interface class RateLimiter {
  Future<List<RateLimitStatus>> check(
    Map<String, Object?> context,
    Map<String, int> windows,
  );
}

/// The only place in this codebase that imports from verdict_rules.
class VerdictRateLimiter implements RateLimiter {
  @override
  Future<List<RateLimitStatus>> check(
    Map<String, Object?> context,
    Map<String, int> windows,
  ) async {
    Rule ruleFor(String window, int quota) => FunctionRule(
          '${window}_under_quota',
          (ctx) async {
            final used = ctx['${window}_used']! as int;
            final status = RateLimitStatus(window: window, used: used, quota: quota);
            return RuleResult(
              ruleName: '${window}_under_quota',
              passed: used < quota,
              data: status,
            );
          },
        );

    final combined = AndRule(
      'rate_limits',
      windows.entries.map((e) => ruleFor(e.key, e.value)).toList(),
    );
    final result = await combined.evaluate(context);
    final subResults = result.data! as List<RuleResult>;
    return subResults.map((r) => r.data! as RateLimitStatus).toList();
  }
}

/// A hand-rolled replacement for VerdictRateLimiter -- same contract,
/// zero verdict_rules. Adding this is a new class; nothing above
/// changed to make room for it.
class SimpleRateLimiter implements RateLimiter {
  @override
  Future<List<RateLimitStatus>> check(
    Map<String, Object?> context,
    Map<String, int> windows,
  ) async {
    return windows.entries
        .map((e) => RateLimitStatus(
              window: e.key,
              used: context['${e.key}_used']! as int,
              quota: e.value,
            ))
        .toList();
  }
}
```

The composition root — the one place that decides which implementation
is actually running — is a single line:

```dart
// Before: wired to the verdict_rules-backed implementation.
RateLimiter rateLimiter = VerdictRateLimiter();

// After: swapped for the hand-rolled one. One line, here, changes.
rateLimiter = SimpleRateLimiter();

// Every call site in the codebase, unaffected either way:
final statuses = await rateLimiter.check(
  {'minute_used': 3, 'hour_used': 40},
  {'minute': 5, 'hour': 100},
);
print(statuses);
// [RateLimitStatus(window: minute, used: 3, quota: 5), RateLimitStatus(window: hour, used: 40, quota: 100)]
```

That last line is the actual proof: it's identical before and after the
swap. Nothing that calls `rateLimiter.check(...)` knows or cares which
class it's holding. Unlike JS/TS's structural `interface`, `RateLimiter`
here is a nominal contract — both classes say `implements RateLimiter`
explicitly — but the boundary still holds exactly the same way: nothing
about `SimpleRateLimiter` names or depends on `VerdictRateLimiter`, so
the swap costs one assignment, not a rewrite.

## Related

- [`README.md`](README.md) — the language-agnostic scenario this page
  implements.
