<!-- Title: Verdict Is A Protocol Spec -->
# verdict is a protocol spec, not a literal API port

> Each SDK implements the same **behavioural contract** while **first-classing
> its own language's idioms, ergonomics and fundamentals**. A divergence that
> exists because the language demands it is correct, not a deviation — and
> honouring it takes priority over surface symmetry with the other SDKs.

## The three layers, which get different treatment

**1. The contract — identical everywhere, no exceptions.**
Sequential never-concurrent evaluation, real short-circuiting, vacuous-truth
polarity per composite shape, emptiness-is-not-absence, opaque result data,
the one-adapter-module boundary. This is the spec. It is enforced mechanically
by `fixtures/graduation_verdict/`, not by anyone remembering.

**2. The idiomatic surface — expected to differ, and should.**
Naming conventions, error types, the async shape, cancellation, ownership,
nullability, how a rule is declared. A .NET consumer expects a
`CancellationToken` on an async API. A Rust port would return `Result<T, E>`
rather than throwing at all. Python already gets cancellation implicitly
through `asyncio` task cancellation at every `await`; Dart's `Future` has no
standard cancellation at all; C# makes it explicit by convention. **Three
languages, three correct answers to the same question.**

Forcing these into a single shape would produce four SDKs that each feel
foreign in their own ecosystem, which is a worse outcome than any amount of
symmetry buys.

**3. Capabilities — all languages or none.**
A genuinely new rule shape, a new run mode, a new concept in the model. These
follow [`features-land-in-every-language`](features-land-in-every-language.md).

## The distinction that matters

**A new capability is a change to the spec. An idiomatic affordance is not.**

`CancellationToken` in C# does not give .NET users a rule engine that can do
something Python's cannot — it gives them the same engine, expressed the way
their language expresses async work. That is layer 2, not layer 3, and the
parity rule does not reach it.

Ask: *would this change what the shared fixture asserts?* If yes, it is a
capability and needs every language. If no, it is surface, and the language's
own conventions decide.

## Why this framing is the right one

It makes the project honest about what it is. "The same seven types in four
languages" is a weaker and less useful claim than "the same guarantees,
expressed natively in four languages." The first invites a port that reads like
a translation; the second invites one that reads like it was written there.

Confirmed with the maintainer, 2026-09-12, while deciding whether C# should
take a `CancellationToken` that no other SDK has.
