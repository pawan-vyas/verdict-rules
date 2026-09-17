<!-- Title: Sample — Content Moderation Routing (C#) -->
# Sample: Content Moderation Routing

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the C# implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is one method with an `if`/`if`
ladder, one line per signal:

```csharp
static string RouteSubmission(Content content)
{
    if (content.HasBannedTerms || content.SpamScore >= content.SpamThreshold)
    {
        return "auto_rejected";
    }
    if (content.Length >= content.MinLength && content.AuthorPostCount >= 10)
    {
        return "auto_published";
    }
    return "sent_to_review";
}
```

Every new signal is a code change and a full re-test of the whole
ladder, and a submission that trips the first reject condition never
reveals whether it would have tripped the second one too. See the spec
for the rest of what this shape gets wrong.

## The `verdict` way

```csharp
using VerdictRules;

static Task<RuleResult> ContainsBannedTerms(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default)
{
    var text = ((string)context["text"]!).ToLowerInvariant();
    var bannedTerms = (IReadOnlyList<string>)context["banned_terms"]!;
    var hit = bannedTerms.Any(text.Contains);
    return Task.FromResult(new RuleResult("contains_banned_terms", !hit));
}

static Task<RuleResult> FlaggedBySpamScore(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("flagged_by_spam_score", (double)context["spam_score"]! < (double)context["spam_threshold"]!));

static Task<RuleResult> MeetsLengthMinimum(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("meets_length_minimum", ((string)context["text"]!).Length >= (double)context["min_length"]!));

static Task<RuleResult> AuthorIsEstablished(IReadOnlyDictionary<string, object?> context, CancellationToken cancellationToken = default) =>
    Task.FromResult(new RuleResult("author_is_established", (double)context["author_post_count"]! >= 10));

var engine = new RulesEngine(new IRule[]
{
    new FunctionRule("contains_banned_terms", ContainsBannedTerms, "auto_reject"),
    new FunctionRule("flagged_by_spam_score", FlaggedBySpamScore, "auto_reject"),
    new FunctionRule("meets_length_minimum", MeetsLengthMinimum, "auto_publish"),
    new FunctionRule("author_is_established", AuthorIsEstablished, "auto_publish"),
});

async Task<string> RouteSubmission(IReadOnlyDictionary<string, object?> context)
{
    var rejectCheck = await engine.RunGroupAsync("auto_reject", context);
    if (!rejectCheck.Passed)
    {
        return "auto_rejected";
    }

    var publishCheck = await engine.RunGroupAsync("auto_publish", context);
    return publishCheck.Passed ? "auto_published" : "sent_to_review";
}
```

All three routing outcomes, from the same engine:

```csharp
var trustedPost = new Dictionary<string, object?>
{
    ["text"] = "a perfectly reasonable long post about gardening",
    ["banned_terms"] = new List<string> { "spam", "scam" },
    ["spam_score"] = 2.0,
    ["spam_threshold"] = 10.0,
    ["min_length"] = 20.0,
    ["author_post_count"] = 50.0,
};
await RouteSubmission(trustedPost);
// "auto_published" -- clears the auto_reject group, then the auto_publish group

await RouteSubmission(new Dictionary<string, object?>(trustedPost) { ["author_post_count"] = 1.0 });
// "sent_to_review" -- clears auto_reject, but the new-author signal fails auto_publish

await RouteSubmission(new Dictionary<string, object?>(trustedPost) { ["spam_score"] = 15.0 });
// "auto_rejected" -- trips the auto_reject group; auto_publish is never even checked
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`loyalty-tier-promotion/csharp.md`](../loyalty-tier-promotion/csharp.md) — the
  `RunAllAsync()` counterpart, for a single engine's every rule rather than
  a named subset.
- [`../../architecture/csharp.md`](../../architecture/csharp.md#three-ways-to-run-rules-concretely) —
  the full comparison of all three `RulesEngine` run modes.
