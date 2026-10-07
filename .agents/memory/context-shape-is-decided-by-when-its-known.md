<!-- Title: Context Shape Is Decided By When It's Known -->
# Whether a sample's context should be typed or dict-shaped has one test

> A reusable design-review test for every sample, extending scenario, or
> eval fixture: one question that decides whether its context should be
> typed or dict-shaped. Settled while rebalancing the generics docs and
> the skill, 2026-09-18.

## The test

Does this domain's context shape get decided by a human author ahead
of time, or by something the caller only knows the name of at runtime?

- **Decided ahead of time** (a fixed, known set of fields — an order,
  an employee record, an account) → reads naturally as a typed context
  (`OrderContext`, `EmployeeContext`). Converting it to dict-context
  loses real information for no benefit.
- **Named by the caller at runtime** (an admin-configured catalog, a
  runtime-named rule set, a per-tenant criteria list) → genuinely wants
  dict-context. Converting it to typed, just to balance a count or
  "prefer typing," would be the same mistake in the other direction.

## Why this needed writing down

[`docs/architecture/README.md`](../../docs/architecture/README.md#generic-context)
already states dict-context is "exactly as first-class as a typed
context, never a fallback for the untyped" — correct, but a human or
an agent learns idiom by pattern-matching the samples it reads, not by
reading the position statement in isolation. An agent building with
verdict can land on the wrong shape for a new sample's domain without
a concrete test to apply, even when the architecture doc's stated
position is exactly right.

## How to apply

Before writing or reviewing a new sample, extending scenario, or eval
fixture: ask the test above about its domain. Don't treat "most
existing samples are dict-context" as precedent on its own — each
sample's shape is a deliberate per-domain decision, not a house style
to default into.
