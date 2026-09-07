---
name: Feature request
about: Propose a new primitive or capability for verdict itself
title: ""
labels: enhancement
---

Before filling this in, most "convenience" additions turn out to be a
one-line recipe in your own adapter code, not a change verdict itself
needs — see `docs/future_plan.md` for the full reasoning and a few
worked examples of proposals that were rejected on exactly this basis.
The two questions below are the same test that doc applies.

**What's the actual behavioral contract this needs, that a consumer
couldn't write themselves in a few lines?**
<!-- Short-circuiting, vacuous-truth polarity, and an invariant on RuleResult.data
     are the three contracts verdict's own core has earned a place for. If your
     proposal doesn't have something in that shape, it's very likely a
     docs/extension.md recipe instead. -->

**Has this come up more than once, from more than one real use case?**
<!-- A single hypothetical use isn't enough signal on its own -- see
     docs/future_plan.md's "Two smaller, better-grounded candidates" section for
     what "repeated, organic demand" actually looks like as evidence. -->

**Proposed shape**
<!-- What would the new Rule/RulesEngine surface actually look like? A short code
     sketch is more useful here than prose. -->

**What it replaces**
<!-- What would someone write today, without this feature, and why is that not
     good enough? -->
