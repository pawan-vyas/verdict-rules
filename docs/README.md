<!-- Title: Verdict Docs -->
# Verdict — docs

> What's in this directory, and the order to read it in depending on
> what you're doing. The root [`../README.md`](../README.md) is the
> narrative entry point; everything here goes deeper than that.

```text
docs/
  README.md            you are here
  architecture/         the "why" — language-agnostic design
    README.md            shared design: type structure, execution model
    <language>.md         each language's own concrete realization
  extending/            building on top of verdict, no changes here
    README.md             the index of extension scenarios
    <scenario>/            one directory per scenario, spec + per-language code
  maintenance/          changing verdict itself
    README.md             where a given kind of change goes
    adding-a-language.md   the one-time ritual for a new language SDK
    releases/              the release pipeline, shared and per-target
    doc-authoring/         the standard every doc in this repo follows
    ...
  testing.md           what a change has to prove
  future_plan.md       exploratory, not-yet-decided feature candidates
  samples/             language-agnostic specs for the worked samples
```

## Reading order

1. **[`../README.md`](../README.md)** — the narrative overview of what
   this package is and why, with no code.
2. **That language's own quickstart** — the core concepts and one
   complete, runnable example.
3. **[`architecture/`](architecture/README.md)** — the "why": type
   structure, the execution model, and the reasoning behind each design
   choice, for anyone who needs more than the quickstart before relying
   on this package.
4. **From here, two audiences split**:
   - Changing `verdict` itself → [`maintenance/`](maintenance/README.md),
     then [`testing.md`](testing.md) to prove the change.
   - Building something on top of it, without changing anything here →
     [`extending/`](extending/README.md), whose scenarios have full worked
     instances in [`samples/`](samples/README.md) and each language's
     own samples directory (generic domains — discounts, fee waivers,
     tier promotions, moderation routing — plus the data-driven pattern
     this package is designed for).
5. **[`future_plan.md`](future_plan.md)** — exploratory feature
   candidates rejected or deferred so far, and the test used to
   evaluate a new one.
