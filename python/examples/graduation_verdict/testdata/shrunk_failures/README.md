<!-- Title: Shrunk Failing Cases -->
# Shrunk Failing Cases

> Where `shrinking.shrink_case` writes a minimal, human-readable JSON
> fixture for a case that fails one of the structural invariants or
> disagrees with the oracle — a per-language debugging aid, not a
> cross-language fixture.

## What lands here

Empty right now — the chaos suite, the structural invariants, and the
oracle comparison all pass across every one of the 500 `CHAOS_SEED`-derived
cases, so there has never been a real failure to shrink. If one of those
suites ever does fail, re-run the failing `case_index` through
[`shrinking.shrink_case`](../../shrinking.py) with a `still_reproduces`
predicate built from the failing assertion, and it writes the minimized
`(policies, context, elective_minimum)` here as a new, descriptively named
JSON file.

This directory is **not** the shared cross-language fixture contract —
that's [`fixtures/graduation_verdict/`](../../../../../fixtures/graduation_verdict/README.md)
at the repo root, read by every language's own port. A file here is a
debugging aid for this Python port alone, safe to delete once the bug
it was minimized from is fixed.

## Related

- [`../../shrinking.py`](../../shrinking.py) — the shrinking mechanism
  that writes fixtures here.
- [`../../docs/testing.md`](../../docs/testing.md) — how shrinking fits
  alongside the chaos suite and the structural invariants.
