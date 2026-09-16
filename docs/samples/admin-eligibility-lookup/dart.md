<!-- Title: Sample — Admin Eligibility Lookup (Dart) -->
# Sample: Admin Eligibility Lookup

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

Before reaching for a rule engine at all, the obvious first
implementation is a plain map of configured checks and a lookup
function — no `verdict_rules` in sight yet:

```dart
final eligibilityChecks = <String, List<(String, Object?)>>{
  'gold_tier': [('spend', 1000)],
  'beta_feature': [], // not filled in yet
};

bool checkEligibility(String checkName, Map<String, Object?> customer) {
  final conditions = eligibilityChecks[checkName];
  if (conditions == null) {
    return false; // "not eligible" either way
  }

  return conditions.every((c) => customer[c.$1] == c.$2);
}
```

A typo and a genuine rejection render identically here, and
`Iterable.every` over an empty list is `true` — the same vacuous truth
every language's `all()`/`every()` gives — so a check the team hasn't
finished configuring yet silently passes. See the spec for the rest of
what this shape gets wrong.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

// "eligible"/"notEligible" come from a check that exists and actually
// ran. "unknownCheck" and "notConfigured" are both absence-shaped, and
// kept distinct from each other and from a genuine verdict so a support
// agent never mistakes "we don't know" for "we checked and the answer
// is no".
enum CheckStatus { eligible, notEligible, unknownCheck, notConfigured }

class LookupResult {
  final CheckStatus status;
  final String detail;

  LookupResult({required this.status, required this.detail});
}

class Condition {
  final String field;
  final Object? expected;

  Condition({required this.field, required this.expected});
}

RulePredicate conditionPredicate(String field, Object? expected) {
  return (context) async {
    final actual = context[field];
    return RuleResult(
      ruleName: field,
      passed: actual == expected,
      detail: '$field=$actual, needs $expected',
    );
  };
}

/// One configured check, plus whether it currently has zero conditions.
///
/// An empty `conditions` list still produces a valid `AndRule` -- it
/// just vacuously passes if ever evaluated directly. `EligibilityLookup`
/// intercepts that case before evaluation (see `check` below), so the
/// vacuous pass never reaches the caller as a real "eligible".
(Rule, bool) buildCheck(String name, List<Condition> conditions) {
  final conditionRules = [
    for (var i = 0; i < conditions.length; i++)
      FunctionRule('$name[$i]', conditionPredicate(conditions[i].field, conditions[i].expected)),
  ];
  return (AndRule(name, conditionRules), conditions.isEmpty);
}

/// Looks up one named eligibility check by name, typed fresh each time.
///
/// [configuredChecks] mirrors whatever an admin settings screen already
/// holds: one entry per check, each a list of condition objects. A check
/// with an empty list is a real, valid state a row can be in while a
/// team is still filling it in -- not an error.
class EligibilityLookup {
  final RulesEngine _engine;
  final Set<String> _unconfigured = {};

  EligibilityLookup(Map<String, List<Condition>> configuredChecks)
      : _engine = RulesEngine([
          for (final entry in configuredChecks.entries) buildCheck(entry.key, entry.value).$1,
        ]) {
    for (final entry in configuredChecks.entries) {
      if (entry.value.isEmpty) _unconfigured.add(entry.key);
    }
  }

  /// Look up and run one named check. Never throws on a bad [name] --
  /// that is the entire point of this class existing between the raw
  /// engine and the screen that renders its result.
  Future<LookupResult> check(String name, Map<String, Object?> customer) async {
    if (_unconfigured.contains(name)) {
      return LookupResult(
        status: CheckStatus.notConfigured,
        detail: "'$name' has no conditions configured yet",
      );
    }

    final result = await _engine.tryRunNamed(name, customer);
    if (result == null) {
      return LookupResult(
        status: CheckStatus.unknownCheck,
        detail: "no eligibility check named '$name' exists",
      );
    }

    return LookupResult(
      status: result.passed ? CheckStatus.eligible : CheckStatus.notEligible,
      detail: result.detail,
    );
  }
}
```

Run against a small configuration — one real check, one the team hasn't
finished, and one lookup with a typo:

```dart
final lookup = EligibilityLookup({
  'gold_tier': [Condition(field: 'spend', expected: 1000)],
  'beta_feature': [], // not filled in yet
});

await lookup.check('gold_tier', {'spend': 1000});
// LookupResult(status: eligible, detail: '')

await lookup.check('gold_tier', {'spend': 5});
// LookupResult(status: notEligible, detail: "'gold_tier[0]' failed: spend=5, needs 1000")

await lookup.check('beta_feature', {'spend': 1000});
// LookupResult(status: notConfigured, detail: "'beta_feature' has no conditions configured yet")

await lookup.check('gold_teir', {'spend': 1000}); // typo
// LookupResult(status: unknownCheck, detail: "no eligibility check named 'gold_teir' exists")
```

The screen can still show "not eligible" for the last two — the page
keeps working, exactly as the naive version intended — but `status` is
what tells a support agent *which* of the four things actually
happened, rather than one boolean standing in for all of them.

### Why not just wrap `runNamed` in a `try`/`catch`?

`tryRunNamed` and `runNamed` share one lookup path — the strict form is
a two-line assertion on top of the lenient one, not a second
implementation. Reaching for `tryRunNamed` directly says "absence is
expected here and I have an answer for it," which is true on this
screen; wrapping the strict form in `try`/`catch` says the same thing
by accident, and reads as "I expect this to throw and I'm suppressing
it" to the next person editing this file. The behavior is identical
either way — the difference is only which one tells the truth about why.

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`../../extending/absence-vs-failure/`](../../extending/absence-vs-failure/README.md) —
  the general absence-vs-emptiness guidance this sample is one concrete
  instance of.
- [`data-driven-rule-sets/dart.md`](../data-driven-rule-sets/dart.md) —
  building the `Rule` objects themselves from stored configuration, the
  same pattern `EligibilityLookup` uses to turn each check's conditions
  into an `AndRule`.
