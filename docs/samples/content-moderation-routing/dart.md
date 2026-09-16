<!-- Title: Sample — Content Moderation Routing (Dart) -->
# Sample: Content Moderation Routing

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Dart implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is one function with an `if`/`if`
ladder, one line per signal:

```dart
class Submission {
  final bool hasBannedTerms;
  final num spamScore;
  final num spamThreshold;
  final int length;
  final int minLength;
  final int authorPostCount;

  Submission({
    required this.hasBannedTerms,
    required this.spamScore,
    required this.spamThreshold,
    required this.length,
    required this.minLength,
    required this.authorPostCount,
  });
}

String routeSubmission(Submission content) {
  if (content.hasBannedTerms || content.spamScore >= content.spamThreshold) {
    return 'auto_rejected';
  }
  if (content.length >= content.minLength && content.authorPostCount >= 10) {
    return 'auto_published';
  }
  return 'sent_to_review';
}
```

Every new signal is a code change and a full re-test of the whole
ladder, and a submission that trips the first reject condition never
reveals whether it would have tripped the second one too. See the spec
for the rest of what this shape gets wrong.

## The `verdict` way

```dart
import 'package:verdict_rules/verdict_rules.dart';

Future<RuleResult> containsBannedTerms(Map<String, Object?> context) async {
  final text = (context['text']! as String).toLowerCase();
  final bannedTerms = context['bannedTerms']! as List<String>;
  final hit = bannedTerms.any((term) => text.contains(term));
  return RuleResult(ruleName: 'contains_banned_terms', passed: !hit);
}

Future<RuleResult> flaggedBySpamScore(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'flagged_by_spam_score',
      passed: (context['spamScore']! as num) < (context['spamThreshold']! as num),
    );

Future<RuleResult> meetsLengthMinimum(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'meets_length_minimum',
      passed: (context['text']! as String).length >= (context['minLength']! as int),
    );

Future<RuleResult> authorIsEstablished(Map<String, Object?> context) async =>
    RuleResult(
      ruleName: 'author_is_established',
      passed: (context['authorPostCount']! as int) >= 10,
    );

final engine = RulesEngine([
  FunctionRule('contains_banned_terms', containsBannedTerms, group: 'auto_reject'),
  FunctionRule('flagged_by_spam_score', flaggedBySpamScore, group: 'auto_reject'),
  FunctionRule('meets_length_minimum', meetsLengthMinimum, group: 'auto_publish'),
  FunctionRule('author_is_established', authorIsEstablished, group: 'auto_publish'),
]);

Future<String> routeSubmission(Map<String, Object?> context) async {
  final rejectCheck = await engine.runGroup('auto_reject', context);
  if (!rejectCheck.passed) {
    return 'auto_rejected';
  }

  final publishCheck = await engine.runGroup('auto_publish', context);
  return publishCheck.passed ? 'auto_published' : 'sent_to_review';
}
```

All three routing outcomes, from the same engine:

```dart
final trustedPost = {
  'text': 'a perfectly reasonable long post about gardening',
  'bannedTerms': ['spam', 'scam'],
  'spamScore': 2,
  'spamThreshold': 10,
  'minLength': 20,
  'authorPostCount': 50,
};
await routeSubmission(trustedPost);
// "auto_published" -- clears the auto_reject group, then the auto_publish group

await routeSubmission({...trustedPost, 'authorPostCount': 1});
// "sent_to_review" -- clears auto_reject, but the new-author signal fails auto_publish

await routeSubmission({...trustedPost, 'spamScore': 15});
// "auto_rejected" -- trips the auto_reject group; auto_publish is never even checked
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`loyalty-tier-promotion/dart.md`](../loyalty-tier-promotion/dart.md) —
  the `runAll()` counterpart, for a single engine's every rule rather
  than a named subset.
- [`../../architecture/dart.md`](../../architecture/dart.md#three-ways-to-run-rules-concretely) —
  the full comparison of all three `RulesEngine` run modes.
