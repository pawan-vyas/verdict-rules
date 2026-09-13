<!-- Title: The Two Constraints That Must Never Quietly Slip -->
# The two constraints that must never quietly slip

> Both are called out in the root [`README.md`](../../README.md) as
> load-bearing, not incidental — a maintainer's first job on any change
> is checking it doesn't erode either one.

1. **Zero external dependencies.** `pyproject.toml`'s `dependencies` list
   is empty on purpose. A change that reaches for a third-party package —
   even a small, well-regarded one — is a discussion-worthy exception,
   not a default. If a future need genuinely can't be met without one,
   that's a real design conversation (does it belong in this package at
   all, or in a consumer's own adapter?), not a routine dependency bump.
2. **No knowledge of any specific domain.** Nothing under a language's
   own package source (today: `python/packages/verdict-rules/src/verdict/`) should ever import
   or reference rate limiting, access grants,
   discounts, or any other consumer's vocabulary. Domain-specific logic
   belongs in the *consumer's own* adapter module — see
   [`../extending/domain-adapter-module/`](../extending/domain-adapter-module/README.md)
   for two living examples of where that vocabulary actually goes. If a
   change to this package only makes sense described in terms of one
   consumer's problem, it's in the wrong file.
