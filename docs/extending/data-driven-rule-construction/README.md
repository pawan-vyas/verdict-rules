<!-- Title: Extending — Building Rule Sets From Stored Configuration -->
# Extending verdict: build rule sets from stored configuration at runtime

> Because rules are plain objects, nothing stops building them from
> whatever configuration a caller already has, rather than hand-writing
> one rule per case at import time.

A list of maps, database rows, a settings file — any of them can drive
a factory function that turns one config entry into one rule, combined
under a composite the same way a hand-written rule set would be. An
empty configuration source produces an empty composite, which vacuously
passes: "nothing configured" and "nothing to enforce" fall out of the
same rule, with no special-casing needed at the call site.

See [`../../samples/data-driven-rule-sets/README.md`](../../samples/data-driven-rule-sets/README.md)
for a fuller worked version of this, carried through for both
rate-limit windows and access-control conditions.

This is exactly the shape where testing has to scale with the rule set:
as the configuration source grows more varied, a handful of hand-picked
fixtures stops being enough coverage, the same way it stopped being
enough for the sample above. Reach for property-based testing or an
oracle/differential approach — an independent reference implementation
checked against many randomly-generated configurations — rather than
adding fixtures one at a time as bugs are found. Full guidance in
[`../../testing/`](../../testing/README.md).

## What this demonstrates

- Rules are built from configuration data at runtime, never hardcoded
  as one literal per case.
- An empty configuration source and "nothing to enforce" are the same
  outcome, not a case requiring special handling.
- Test coverage scales with the configuration space, not with however
  many fixtures happened to get written first.

## Related

- [`../../samples/data-driven-rule-sets/README.md`](../../samples/data-driven-rule-sets/README.md) —
  the full worked version of this scenario.
- [`../../testing/`](../../testing/README.md) — the property-based/oracle
  testing guidance this scenario's test coverage needs.
