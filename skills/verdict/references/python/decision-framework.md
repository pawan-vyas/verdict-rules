# Decision Framework

Two decisions come up on almost every verdict-shaped task: does this
even need a rule engine, and once you're building with one, which
`RulesEngine` run mode (or bare composite) actually fits the caller.

## Does this need verdict at all?

The shape test: is the requirement "combine several
independently-changing conditions into one pass/fail verdict"? Concrete
signals it fits:

- The conditions come from configuration, an admin screen, or a
  database row — not fixed at write time.
- There are genuinely multiple conditions (3+) that can each change
  independently of the others.
- Someone will eventually ask "why did/didn't this pass?" and deserves
  a real per-condition answer, not just a boolean.
- The same kind of check needs to happen from more than one call site,
  or needs both a fast yes/no *and* a full diagnostic breakdown.

Signals it's **over-engineering**:

- One or two conditions, unlikely to grow, that nobody's asking to
  configure.
- The "why not" question doesn't matter to any real user of the
  feature.
- A plain function reads more clearly than the `Rule`/`RulesEngine`
  machinery would for this specific case.

When in doubt, write the plain function first. Reaching for verdict
later, once a second or third condition shows up, costs nothing — the
rules can be built from whatever data already exists by then.

## Which run mode / composite shape fits this caller?

| You need... | Reach for... |
|---|---|
| The fastest possible yes/no, short-circuiting on the first deciding rule | A bare `AndRule`/`OrRule`, calling `.evaluate()` directly — this is the common case for "is this allowed?" |
| A full diagnostic picture — every rule's own pass/fail, for a status page, an audit trail, or a "why not" screen | `RulesEngine.run_all()` — deliberately never short-circuits |
| One specific, already-known rule, independent of any others | `RulesEngine.run_named()` |
| A named subset of a larger rule collection, without pulling in unrelated rules registered on the same engine | `RulesEngine.run_group()` — also never short-circuits |
| Any of the above, where the name or label may legitimately not exist | `RulesEngine.try_run_named()` / `try_run_group()` — returns `None` rather than raising, so your domain decides what absence means |

These aren't mutually exclusive — an engine can hold a composite
(`AndRule`/`OrRule`) as one of its own named/grouped rules, and the
*same* built `Rule` objects can back both a `RulesEngine` (for
diagnostics) and a separate composite (for the fast verdict), built
once and reused, never rebuilt per lookup. This is the right default
whenever a feature needs both "does this pass" and "show me every
reason" — build the rule objects once, then hand them to whichever
structure the caller actually needs.

## A worked example of the split

A graduation-eligibility check needs: a student portal asking "did I
pass Biology" (→ `run_named`), a registrar screen showing every core
subject's own pass/fail (→ `run_group("core")`), a full transcript (→
`run_all()`), and the actual graduation decision, computed as cheaply
as possible (→ a separate `AndRule` built from the same subject rules).
All four reuse one set of `Rule` objects, built once:

```python
subject_rules = [rule_for_subject(p) for p in policies]  # built once
engine = RulesEngine(subject_rules)                       # for lookups/diagnostics
graduates = AndRule("graduates", [                        # for the fast verdict
    AndRule("core", [r for r in subject_rules if r.group == "core"]),
    # ... other requirements
])
```

See `extension-recipes.md` for how `rule_for_subject` itself gets built
from external data, and `testing-patterns.md` for how to prove all of
this stays correct as the underlying data changes.
