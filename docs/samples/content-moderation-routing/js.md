<!-- Title: Sample — Content Moderation Routing (JS/TS) -->
# Sample: Content Moderation Routing

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the JS/TS implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is one function with an `if`/`if`
ladder, one line per signal:

```ts
interface Submission {
  hasBannedTerms: boolean;
  spamScore: number;
  spamThreshold: number;
  length: number;
  minLength: number;
  authorPostCount: number;
}

async function routeSubmission(content: Submission): Promise<string> {
  if (content.hasBannedTerms || content.spamScore >= content.spamThreshold) {
    return "auto_rejected";
  }
  if (content.length >= content.minLength && content.authorPostCount >= 10) {
    return "auto_published";
  }
  return "sent_to_review";
}
```

Every new signal is a code change and a full re-test of the whole
ladder, and a submission that trips the first reject condition never
reveals whether it would have tripped the second one too. See the spec
for the rest of what this shape gets wrong.

## The `verdict-rules` way

```ts
import { FunctionRule, RulesEngine, type RuleResult } from "verdict-rules";

interface SubmissionContext {
  text: string;
  bannedTerms: string[];
  spamScore: number;
  spamThreshold: number;
  minLength: number;
  authorPostCount: number;
}

async function containsBannedTerms(context: SubmissionContext): Promise<RuleResult> {
  const text = context.text.toLowerCase();
  const hit = context.bannedTerms.some((term) => text.includes(term));
  return { ruleName: "contains_banned_terms", passed: !hit };
}

async function flaggedBySpamScore(context: SubmissionContext): Promise<RuleResult> {
  return { ruleName: "flagged_by_spam_score", passed: context.spamScore < context.spamThreshold };
}

async function meetsLengthMinimum(context: SubmissionContext): Promise<RuleResult> {
  return { ruleName: "meets_length_minimum", passed: context.text.length >= context.minLength };
}

async function authorIsEstablished(context: SubmissionContext): Promise<RuleResult> {
  return { ruleName: "author_is_established", passed: context.authorPostCount >= 10 };
}

const engine = new RulesEngine<SubmissionContext>([
  new FunctionRule("contains_banned_terms", containsBannedTerms, "auto_reject"),
  new FunctionRule("flagged_by_spam_score", flaggedBySpamScore, "auto_reject"),
  new FunctionRule("meets_length_minimum", meetsLengthMinimum, "auto_publish"),
  new FunctionRule("author_is_established", authorIsEstablished, "auto_publish"),
]);

async function routeSubmission(context: SubmissionContext): Promise<string> {
  const rejectCheck = await engine.runGroup("auto_reject", context);
  if (!rejectCheck.passed) {
    return "auto_rejected";
  }

  const publishCheck = await engine.runGroup("auto_publish", context);
  return publishCheck.passed ? "auto_published" : "sent_to_review";
}
```

All three routing outcomes, from the same engine:

```ts
const trustedPost: SubmissionContext = {
  text: "a perfectly reasonable long post about gardening",
  bannedTerms: ["spam", "scam"],
  spamScore: 2,
  spamThreshold: 10,
  minLength: 20,
  authorPostCount: 50,
};
await routeSubmission(trustedPost);
// "auto_published" -- clears the auto_reject group, then the auto_publish group

await routeSubmission({ ...trustedPost, authorPostCount: 1 });
// "sent_to_review" -- clears auto_reject, but the new-author signal fails auto_publish

await routeSubmission({ ...trustedPost, spamScore: 15 });
// "auto_rejected" -- trips the auto_reject group; auto_publish is never even checked
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`loyalty-tier-promotion/js.md`](../loyalty-tier-promotion/js.md) — the
  `runAll()` counterpart, for a single engine's every rule rather than
  a named subset.
- [`../../architecture/js.md`](../../architecture/js.md#three-ways-to-run-rules-concretely) —
  the full comparison of all three `RulesEngine` run modes.
