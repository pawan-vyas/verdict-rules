<!-- Title: An Adopter's Needs Are Input, Not Requirements -->
# An adopter's needs are input to a decision, never the decision

> A consumer built on verdict will report gaps and ask for features, often
> with a real, specific requirement behind the ask. **Answer on verdict's own
> terms.** Don't change verdict's plan, API or code to suit a consumer unless
> the change stands on its own for verdict, judged against the two questions
> in [`future_plan.md`](../../docs/future_plan.md#the-actual-test-not-does-it-sound-useful).
> Confirmed with the maintainer while answering an adopter session directly.

## Why the distinction is load-bearing

A specific, well-argued requirement from a real consumer is the most
persuasive input there is, and it is exactly the input most likely to get a
feature shipped that verdict should not own. The consumer is optimising for
their own codebase; verdict is optimising for being the same small engine in
four languages for every consumer. Those usually agree. When they don't, the
consumer's version wins on urgency and loses on scope.

The useful reframing: **a report is evidence, not a request** -- which is
already what
[`skills/verdict/references/issue-reporting.md`](../../skills/verdict/references/issue-reporting.md)
tells an agent to write when reporting *into* verdict. The same framing
applies on the receiving side.

## How to apply

1. **Verify the claim in the source first.** Every adopter report acted on so
   far was accurate about the symptom and incomplete about the cause. One
   reported composites keeping their parts private, which was true in all
   four SDKs -- and C# turned out to already expose the capability internally
   for its own debugger, which changed what the fix was.
2. **Separate the symptom from their proposed remedy.** One report asked for
   read-only parts *or* a visitor; both would have left a consumer's *own*
   composite opaque, which was the half of their problem that mattered most.
   The shape that fixed it was neither: a published contract.
3. **Apply the two questions, and say which one decides.** A threshold
   composite was declined because it fails the demand bar (one consumer,
   where two unrelated ones are asked for), *not* because it lacks subtlety.
   Naming the right reason is what lets it be reconsidered later instead of
   re-argued -- see
   [`when-you-decline-record-which-question-failed`](when-you-decline-record-which-question-failed.md).
4. **Declining is cheap when the plumbing is public.** `SequentialEvaluator`
   and the composite contract exist so a consumer can build their own shape.
   A decline should hand over the worked primitive and the traps, not just a
   no.
5. **Record the decision in verdict's own records**, not only in the reply.
   A reply is correspondence; `future_plan.md`, a plan, a changelog and the
   relevant `extending/` scenario are where the next person looks. If the
   only trace of a decision is a message, it is lost.

## The channel, when one exists

An adopter session and a verdict session may exchange numbered messages
through a file-based channel under
[`../scratch/`](../scratch/) -- gitignored, because the correspondence is
transient by design and the decisions belong in each repo's own records.
Two things learned from running one:

- **Write only your own side's file**, append-only, newest last, and correct
  a message by appending a superseding one rather than editing it.
- **Watch for the other side's new messages whenever your session is
  active**, not only while a thread is open. A new thread is by definition
  sent when no thread is open, so a watch gated on an open thread cannot
  deliver the one message type that starts a conversation. Neither side is a
  daemon: when a session ends its watch dies, so a sender should not assume
  a message has been seen.
