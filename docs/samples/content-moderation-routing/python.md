<!-- Title: Sample — Content Moderation Routing -->
# Sample: Content Moderation Routing

> The problem, the design, and what a solution must demonstrate are
> language-agnostic and live in [`README.md`](README.md) — read that
> first. This page is the Python implementation of it.

## The naive way (and why it breaks down)

The obvious first implementation is one function with an `if`/`if`
ladder, one line per signal:

```python
async def route_submission(content: dict) -> str:
    if content["has_banned_terms"] or content["spam_score"] >= content["spam_threshold"]:
        return "auto_rejected"
    if content["length"] >= content["min_length"] and content["author_post_count"] >= 10:
        return "auto_published"
    return "sent_to_review"
```

Every new signal is a code change and a full re-test of the whole
ladder, and a submission that trips the first reject condition never
reveals whether it would have tripped the second one too. See the spec
for the rest of what this shape gets wrong.

## The `verdict` way

```python
from dataclasses import dataclass

from verdict import FunctionRule, RulesEngine, RuleResult


@dataclass(frozen=True)
class SubmissionContext:
    text: str
    banned_terms: list[str]
    spam_score: float
    spam_threshold: float
    min_length: int
    author_post_count: int


async def contains_banned_terms(context: SubmissionContext) -> RuleResult:
    hit = any(term in context.text.lower() for term in context.banned_terms)
    return RuleResult(rule_name="contains_banned_terms", passed=not hit)


async def flagged_by_spam_score(context: SubmissionContext) -> RuleResult:
    return RuleResult(rule_name="flagged_by_spam_score", passed=context.spam_score < context.spam_threshold)


async def meets_length_minimum(context: SubmissionContext) -> RuleResult:
    return RuleResult(rule_name="meets_length_minimum", passed=len(context.text) >= context.min_length)


async def author_is_established(context: SubmissionContext) -> RuleResult:
    return RuleResult(rule_name="author_is_established", passed=context.author_post_count >= 10)


engine: RulesEngine[SubmissionContext] = RulesEngine([
    FunctionRule("contains_banned_terms", contains_banned_terms, group="auto_reject"),
    FunctionRule("flagged_by_spam_score", flagged_by_spam_score, group="auto_reject"),
    FunctionRule("meets_length_minimum", meets_length_minimum, group="auto_publish"),
    FunctionRule("author_is_established", author_is_established, group="auto_publish"),
])


async def route_submission(context: SubmissionContext) -> str:
    reject_check = await engine.run_group("auto_reject", context)
    if not reject_check.passed:
        return "auto_rejected"

    publish_check = await engine.run_group("auto_publish", context)
    return "auto_published" if publish_check.passed else "sent_to_review"
```

All three routing outcomes, from the same engine:

```python
from dataclasses import replace

trusted_post = SubmissionContext(
    text="a perfectly reasonable long post about gardening",
    banned_terms=["spam", "scam"],
    spam_score=2,
    spam_threshold=10,
    min_length=20,
    author_post_count=50,
)
await route_submission(trusted_post)
# "auto_published" — clears the auto_reject group, then the auto_publish group

await route_submission(replace(trusted_post, author_post_count=1))
# "sent_to_review" — clears auto_reject, but the new-author signal fails auto_publish

await route_submission(replace(trusted_post, spam_score=15))
# "auto_rejected" — trips the auto_reject group; auto_publish is never even checked
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`loyalty-tier-promotion/python.md`](../loyalty-tier-promotion/python.md) — the
  `run_all()` counterpart, for a single engine's every rule rather than
  a named subset.
- [`../../architecture/python.md`](../../architecture/python.md#three-ways-to-run-rules-concretely) —
  the full comparison of all three `RulesEngine` run modes.
