---
name: Feature request
about: Propose a new primitive or capability for verdict itself
title: ""
labels: enhancement
---

Before filling this in, most "convenience" additions turn out to be a
one-line recipe in your own adapter code, not a change verdict itself
needs — see
[`docs/future_plan.md`](../../docs/future_plan.md#the-actual-test-not-does-it-sound-useful)
for the full reasoning and worked examples of proposals rejected on
exactly this basis. The two questions below are the same test that doc
applies.

**What's the actual behavioral contract this needs, that a consumer
couldn't write themselves in a few lines?**
<!-- Short-circuiting, vacuous-truth polarity, and a composite's children living in
     its result's sub-results (exactly what it evaluated, never padded to the full
     sub-rule list, never flattened into the parent) are contracts verdict's own core
     has earned a place for. Ask the question of the result surface too, not just the
     evaluation order: NotRule earned its place there, because a failed negation wraps
     a child that passed. If your proposal doesn't have something in that shape, it's
     very likely a scenario for docs/extending/ instead. -->

**Has this come up more than once, from more than one real use case?**
<!-- A single hypothetical use isn't enough signal on its own; the
     "Two candidates this file named, both now shipped" section of docs/future_plan.md
     shows what "repeated, organic demand" actually looks like as evidence. -->

**Proposed shape**
<!-- What would the new Rule/RulesEngine surface actually look like? A short code
     sketch is more useful here than prose. -->

**What it replaces**
<!-- What would someone write today, without this feature, and why is that not
     good enough? -->
