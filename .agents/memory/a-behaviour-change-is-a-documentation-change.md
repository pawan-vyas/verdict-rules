<!-- Title: A Behaviour Change Is A Documentation Change -->
# A behaviour change is not done until every doc that described the old behaviour is corrected

> Changing what the library does is the small half. The larger half is
> that a dozen files now describe something untrue, and **nothing fails
> when they do** — tests pass, CI is green, and the wrong answer sits
> there until a reader believes it.

## Why this needs to be a rule

`0.1.1` changed `run_group` to raise on an unknown group. The code and
its tests were updated. Three skill reference files were updated. And
`docs/testing.md` went on saying *"`RulesEngine.run_group()` on an
unknown group passes"* — the exact opposite — plus citing a test that
had been renamed out of existence. It was found by an audit two versions
later, not by anything automated.

That is the failure mode: documentation drift is **silent by
construction**. There is no build step that catches it.

## The sweep, every time behaviour changes

Work outward from the code. Each ring describes the same thing at a
different altitude, so a change that reaches one usually reaches all.

1. **Source docstrings** — the API reference for most readers.
2. **`docs/architecture.md`** — the design position, *and its diagrams*.
   A class diagram missing a new method is stale; a decision diagram
   that no longer covers the choices is worse, because it looks
   authoritative.
3. **`docs/extension.md`** — does this enable a recipe that did not
   exist, or invalidate one that did? A capability with no recipe is a
   capability nobody finds.
4. **`docs/testing.md`** — it names specific tests. Renaming a test
   breaks this doc silently.
5. **`docs/maintenance.md`** — release procedure, versioning, where a
   change goes.
6. **Language quickstarts and samples** — `<lang>/docs/`.
7. **The shared fixture** — if the change is behavioural, does
   `fixtures/graduation_verdict/` need to pin it? If it does and you
   skip it, every future port can get it wrong.
8. **`skills/verdict/references/`** — an agent acting on a stale skill
   writes wrong code confidently. Bump `.claude-plugin/plugin.json` in
   the same commit or CI fails.
9. **That package's own `CHANGELOG.md`**, beside its manifest — the entry
   is written with the version bump, never backfilled.
10. **`README.md`** — usually untouched, but check: it makes claims too.

## Checks worth running, since none of this is automatic

```
grep -rn '<the old API or behaviour>' docs/ python/packages/*/docs/ skills/ README.md
```

Then: every relative link and anchor still resolves, and every mermaid
diagram still validates.

### Every code sample runs, actually

Not eyeballed — **executed against the built package**, in a scratch
file, before the doc is called done. A sample that looks right and is
subtly wrong is worse than no sample: a reader trusts it, copies it, and
debugs their own code for the mismatch.

The bar scales with position. The **first or headline example in a
document must run verbatim**, imports and all, because that is the one
people paste. Later examples in the same document may assume the
imports and setup already established above them — repeating the
boilerplate at every snippet makes an advanced doc unreadable — but
everything *after* that implied preamble must still be correct as
written.

This is cheap to check and catches real errors: the samples in Recipe 6
were run line by line against the real engine before `0.2.0` shipped,
and the README's opening example was executed to confirm the `detail`
string it prints is character-for-character what the library actually
returns.

## Extend the diagrams, do not only correct them

A new capability usually deserves to be *shown*, not just described —
the recipe for it especially, where a reader is deciding between
options. Correcting a diagram's text while leaving it describing the old
shape is the most misleading outcome available, because a diagram reads
as more authoritative than prose.

## The generalisation, for every language

This is not Python's procedure. Each SDK carries the same rings — its
own docstrings, its own README, its own reference content under
`skills/verdict/references/<lang>/` — and the shared docs at the repo
root are common to all of them. A behaviour change lands in every
language under [`features-land-in-every-language`](features-land-in-every-language.md),
so its documentation sweep does too.

Confirmed with the maintainer, 2026-09-12, while shipping `0.2.0`.
