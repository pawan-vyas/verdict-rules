<!-- Title: The Two Constraints That Must Never Quietly Slip -->
# The two constraints that must never quietly slip

> Both are called out in the root [`README.md`](../../README.md) as
> load-bearing, not incidental — a maintainer's first job on any change
> is checking it doesn't erode either one.

1. **No third-party runtime dependencies.** Each language's own
   manifest lists none, on purpose, and every registry-visible
   description says so — which is what makes taking one a
   release-visible change rather than a routine bump. A **first-party**
   package, maintained by the language's own owner (`package:meta` from
   the Dart team, a `Microsoft.*` package), is permitted but is still a
   decision: it has to be worth amending that claim for. A third-party
   package, however small or well-regarded, is a real design
   conversation first — does this belong in this package at all, or in a
   consumer's own adapter?
2. **No knowledge of any specific domain.** Nothing under a language's
   own package source should ever import or reference rate limiting,
   access grants, discounts, or any other consumer's vocabulary.
   Domain-specific logic belongs in the *consumer's own* adapter module — see
   [`../extending/domain-adapter-module/`](../extending/domain-adapter-module/README.md)
   for two living examples of where that vocabulary actually goes. If a
   change to this package only makes sense described in terms of one
   consumer's problem, it's in the wrong file.
