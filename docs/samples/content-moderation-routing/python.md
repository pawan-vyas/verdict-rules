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
from verdict import FunctionRule, RulesEngine, RuleResult


async def contains_banned_terms(context: dict) -> RuleResult:
    hit = any(term in context["text"].lower() for term in context["banned_terms"])
    return RuleResult(rule_name="contains_banned_terms", passed=not hit)


async def flagged_by_spam_score(context: dict) -> RuleResult:
    return RuleResult(rule_name="flagged_by_spam_score", passed=context["spam_score"] < context["spam_threshold"])


async def meets_length_minimum(context: dict) -> RuleResult:
    return RuleResult(rule_name="meets_length_minimum", passed=len(context["text"]) >= context["min_length"])


async def author_is_established(context: dict) -> RuleResult:
    return RuleResult(rule_name="author_is_established", passed=context["author_post_count"] >= 10)


engine = RulesEngine([
    FunctionRule("contains_banned_terms", contains_banned_terms, group="auto_reject"),
    FunctionRule("flagged_by_spam_score", flagged_by_spam_score, group="auto_reject"),
    FunctionRule("meets_length_minimum", meets_length_minimum, group="auto_publish"),
    FunctionRule("author_is_established", author_is_established, group="auto_publish"),
])


async def route_submission(context: dict) -> str:
    reject_check = await engine.run_group("auto_reject", context)
    if not reject_check.passed:
        return "auto_rejected"

    publish_check = await engine.run_group("auto_publish", context)
    return "auto_published" if publish_check.passed else "sent_to_review"
```

## Related

- [`README.md`](README.md) —
  the language-agnostic spec this page implements.
- [`loyalty-tier-promotion/python.md`](../loyalty-tier-promotion/python.md) — the
  `run_all()` counterpart, for a single engine's every rule rather than
  a named subset.
- [`../../architecture/python.md`](../../architecture/python.md#three-ways-to-run-rules-concretely) —
  the full comparison of all three `RulesEngine` run modes.
