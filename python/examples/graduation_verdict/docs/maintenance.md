<!-- Title: Graduation Verdict — Maintenance -->
# Graduation Verdict — Maintenance

> How to extend this project. See [`architecture.md`](architecture.md)
> for why it's built the way it is, and [`testing.md`](testing.md) for
> how it's tested — including why its own test suite also serves as a
> regression net for `verdict` itself, not just for this example.

## How to extend it

| You want to... | Touch this |
|---|---|
| Change a passing threshold | One field in `policies.json`. No Python touched. |
| Add a subject of an existing type | One new object in `policies.json`. No Python touched. |
| Add a subject of a genuinely new type | One new object in `policies.json`, **plus** one new branch in `rule_for_subject()` (`graduation_verdict.py`) — a real code change, since a new *kind* of pass condition is a new concept, not new data. |
| Add a student scenario | One new entry in the shared `students.json`, with its own `expected` block (verdict, how many rules should run, the failing chain, run-mode and group counts). No Python touched — `test_graduation_verdict.py` iterates every entry generically. Observe the numbers from a passing run rather than hand-writing them; see [the fixture contract](../../../../fixtures/graduation_verdict/README.md). |
| Change the elective requirement (e.g. 2-of-3 → 3-of-4) | `policies.json`'s top-level `elective_minimum` field. No Python touched. |
| Test a new edge case across a wide random space, not just one hand-picked student | Nothing here at all — that's `test_chaos.py`; see [`testing.md`](testing.md#the-chaos-suite-differential-testing-against-an-independent-oracle). Raise `NUM_CASES` there if you want more coverage. |

After any change, re-run the tests — see
[`testing.md`](testing.md#running-the-tests).

## Related

- [`architecture.md`](architecture.md) — why it's built the way it is.
- [`testing.md`](testing.md) — how it's tested, and its regression-net
  role for `verdict` itself.
- [`../README.md`](../README.md) — how to run the demo and the tests.
- [`../../../../docs/maintenance.md`](../../../../docs/maintenance.md) —
  verdict's own maintenance guide, whose consumer-impact checklist
  points back here.
