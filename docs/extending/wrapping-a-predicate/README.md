<!-- Title: Extending — Wrapping A Predicate -->
# Extending verdict: wrapping a predicate you already have

> The common case, and the one `verdict` itself expects to cover most
> needs. Each language's own file in this directory —
> [`python.md`](python.md) today — shows the concrete code.

Any async function that inspects a context and decides pass/fail is a
rule the moment it's wrapped in the language's own `FunctionRule`. No
new class, no new file needed for this shape — it's what the large
majority of rules should end up being.

Because `Rule` is a structural type, not an abstract base class, this
wrapping requires **zero registration, zero imports beyond the ones you
already need, and zero subclassing**. You never ask this package's
permission to add a new kind of rule.

## What this demonstrates

- A plain predicate function becomes a `Rule` by construction, not by
  inheritance.
- `FunctionRule` is the on-ramp for the overwhelming majority of rules;
  reaching for a custom `Rule` implementation (see
  [`../new-rule-shape/`](../new-rule-shape/README.md)) is the exception,
  not the default.

## Related

- [`../new-rule-shape/`](../new-rule-shape/README.md) — for when
  `AndRule`/`OrRule` don't cover the combination logic you need and a
  predicate wrapper isn't enough either.
- [`../README.md`](../README.md) — the full index of extension
  scenarios.
