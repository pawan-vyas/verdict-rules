<!-- Title: When You Decline, Record Which Question Failed -->
# A declined candidate needs the right reason recorded, not just "no"

> [`future_plan.md`](../../docs/future_plan.md#the-actual-test-not-does-it-sound-useful)
> declines a proposed addition on two questions: is there a real correctness
> subtlety worth centralizing, and is there repeated demand across two or
> more unrelated consumers. **Write down which of the two it failed.** The
> verdict survives; a wrong *reason* survives too, and is what gets acted on
> next time.

## This has now gone wrong twice, in opposite directions

| Candidate | Reason recorded | What was actually true |
| :-- | :-- | :-- |
| `NotRule` | "No real subtlety -- negation is one line" | The first question was applied only to *short-circuiting*. The subtlety was real and lived in the **result** contract: a failed negation wraps a child that passed, so it is its own failing leaf and names its passing child. It later shipped. |
| "at-least-N-of-M" | "No real subtlety" | The stopping rule is subtle and silently so -- decide when the threshold is reached **and** when it becomes unreachable. It fails the **demand** bar instead: one consumer, where two are asked for. |

Both entries read fluently and neither was challenged until a consumer asked
about that exact feature. In the `NotRule` case the wrong reason had already
been copied into the rejection of two *other* candidates by the time it was
caught.

## Why the reason rots faster than the verdict

A verdict is a fact about a decision and stays true until someone changes it.
A reason is a claim about the *library*, and the library moves. "No real
subtlety" is a claim that a whole class of correctness problem does not exist
in this shape -- which stops being true the moment the surface grows, and
nothing fails when it does.

The demand bar does not rot the same way: "one consumer, not two" is a count
at a point in time, and stating it that way invites the natural follow-up
rather than closing the question.

## How to apply

- **Name the failing question explicitly** -- "declined on the demand bar",
  "declined on the first question" -- rather than paraphrasing it into prose
  that reads like a general truth.
- **Record the trigger that would reopen it** where the bar is demand: a
  second, unrelated consumer. A trigger converts a decline into a decision
  that can be revisited on its merits instead of re-argued from scratch.
- **Put the reasoning where someone looking for the feature will land**, not
  only in `future_plan.md`. A threshold composite's reasoning sits in
  [`extending/new-rule-shape/`](../../docs/extending/new-rule-shape/README.md)
  too, because that is the page a reader opens when they want one.
- **Re-read the reason, not just the verdict, when the surface grows.** This
  is part of the sweep in
  [`a-behaviour-change-is-a-documentation-change`](a-behaviour-change-is-a-documentation-change.md):
  a new view or guarantee can make a recorded "no subtlety here" false
  without touching the entry.
- **If the bar is overridden, say so.** It was overridden once for the
  composite-parts contract, on one consumer's evidence. That is legitimate
  and is recorded as an override rather than dressed up as met -- otherwise
  the test looks satisfied when it was set aside, and the next override has
  no precedent to weigh against.
