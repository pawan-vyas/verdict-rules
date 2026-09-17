<!-- Title: Extending Verdict -->
# Extending verdict

> For people building *on top of* verdict from their own codebase — a
> new rule shape, a new domain, a new way of assembling rules — without
> changing anything under this package's own source itself. If the
> change you're making genuinely belongs inside this package, see
> [`../maintenance/README.md`](../maintenance/README.md) instead.

The short version: because `Rule` is a structural type, not a fixed
class hierarchy — see
[`../architecture/README.md`](../architecture/README.md#type-structure) —
everything below is possible with **zero registration, zero imports
beyond the ones you already need, and zero subclassing**. You never ask
this package's permission to add a new kind of rule.

Each scenario here follows the same template as
[`../samples/`](../samples/README.md): a language-agnostic spec in
`README.md`, plus one concrete file per language that has written it
up. See
[`../maintenance/doc-authoring/extending.md`](../maintenance/doc-authoring/extending.md)
for that template.

| Scenario | What it covers |
| --- | --- |
| [`wrapping-a-predicate/`](wrapping-a-predicate/README.md) | The common case: any predicate becomes a rule by wrapping, not subclassing. |
| [`new-rule-shape/`](new-rule-shape/README.md) | A combination shape `AndRule`/`OrRule` don't cover, written as a new type in your own code. |
| [`domain-adapter-module/`](domain-adapter-module/README.md) | Keeping your own domain's vocabulary out of verdict, in one module. |
| [`data-driven-rule-construction/`](data-driven-rule-construction/README.md) | Building rules from stored configuration at runtime instead of hardcoding them. |
| [`nesting-composites/`](nesting-composites/README.md) | Composites holding composites, to any depth, with no special-casing. |
| [`absence-vs-failure/`](absence-vs-failure/README.md) | Deciding for yourself what a missing named rule or group should mean. |
| [`isolating-flaky-predicates/`](isolating-flaky-predicates/README.md) | Stopping one predicate's own exception from taking out an entire run. |
| [`reusing-a-rule-across-contexts/`](reusing-a-rule-across-contexts/README.md) | Composing a typed rule into more than one composite context via a small projecting adapter, without weakening the composite's own same-context guarantee. |

## What you never need to do

- Register a new rule shape anywhere in this package.
- Subclass anything — `Rule` is a structural type, not a base class to
  extend.
- Import `verdict` from more than one adapter module per domain (see
  [`domain-adapter-module/`](domain-adapter-module/README.md)) — if you
  find yourself doing that, that's the signal to consolidate.
- Change anything under a language's own package source for any of the
  scenarios above — if a scenario seems to require that, it likely
  belongs in [`../maintenance/README.md`](../maintenance/README.md)
  instead, as a change to the package itself rather than an extension of
  it.

## More scenarios are welcome

This list is not closed. A pattern you've built on top of verdict that
isn't covered above, and that another consumer would plausibly hit too,
is a good candidate for a new scenario here — open an issue or a pull
request adding it, following the template linked above. Adding one never
touches an existing scenario's files.

The same goes for first-class language integrations these scenarios
currently leave to consumer code — an extensions entry point
(`verdict_rules.extensions` in Python, `verdict-rules/extensions` in a
future JS package, or that language's own idiomatic equivalent) is a
reasonable ask once real, repeated demand for one shows up, and belongs
as a proposal in [`../future_plan.md`](../future_plan.md) rather than
something to guess at speculatively ahead of that demand.

## Related docs

- [`../architecture/README.md`](../architecture/README.md) — why `Rule`
  being a structural type is what makes all of the above free.
- [`../maintenance/README.md`](../maintenance/README.md) — changing
  this package itself.
- [`../testing/`](../testing/README.md) — testing verdict itself, if a
  scenario here turns out to need a change on that side after all.
- [`../samples/README.md`](../samples/README.md) — full worked
  examples using these scenarios end-to-end.
