<!-- Title: Before Merging — Consumer Impact and Testing -->
# Before merging: consumer impact and testing

> What a shape change has to prove before it merges, and what a new rule
> shape needs tested regardless of whether it's a shape change.

## Which changes ripple outward

Most extensions are purely additive and cannot break an existing
consumer: a rule is satisfied structurally or by an explicit declaration
depending on the language, and either way nothing has to be registered
here for a new shape to work. Four kinds of change do ripple, and each
is a change to something every consumer already depends on the shape of:

| Change | Why it ripples |
| :-- | :-- |
| A result type's field names or types | Every adapter that reads one, in every consumer |
| The rule contract — required members, the evaluate signature | Every rule implementation anywhere, including a consumer's own |
| What a predicate returns | Every predicate ever written against the old contract; the library now rejects the wrong shape rather than accepting it |
| Which result views are stored versus derived | A hand-built result, or anything reconstructing one from serialized form |

## Before merging one of those

1. **Find every consumer's own usage and read each hit.** The import to
   search for is that language's own:

   | Language | Search for |
   | :-- | :-- |
   | Python | `from verdict import` / `import verdict` |
   | JS/TS | `from "verdict-rules"` / `require("verdict-rules")` |
   | C# | `using VerdictRules` |
   | Dart | `package:verdict_rules/` |

2. **Confirm every real adapter still constructs and consumes the
   changed type correctly** — by hand, per hit. See
   [`../extending/domain-adapter-module/`](../extending/domain-adapter-module/README.md)
   for what a well-formed adapter looks like.
3. **Run this package's own suite, then each consumer's own suite.** A
   change that is internally consistent here can still break a
   consumer's assumption about a field it reads out of the opaque
   payload slot.
4. **Run the example projects too** — the one consumer always available
   without access to anyone else's codebase, and broad enough
   (heterogeneous rule shapes, a custom rule type, every run mode
   together, a differential suite against an independent oracle) to
   catch an interaction bug the narrower checks miss. The commands are
   in [`../testing/`](../testing/README.md), per language.
5. **Regenerate the public-API snapshot and run the cross-language
   gate** — see [`api-snapshots.md`](api-snapshots.md). A shape change
   that does not update its snapshot fails the build; one that changes a
   shared concept also needs its row in the concept map.
6. **Update the shared fixture if the change is observable through it**,
   and pin the new expectation in every language — see
   [`adding-a-fixture.md`](adding-a-fixture.md). This is the only check
   here that catches a language silently disagreeing with the others.

A consumer can also vendor this package by local path rather than a
registry dependency — an editable install, a project reference, a path
dependency, a `file:` dependency. That is a sharper version of the same
risk: no version pin at all between a change here and that consumer's
next process restart, so steps 1 through 3 matter more in that setup,
not less.

## Testing a change

A new rule shape needs a short-circuit-and-vacuous-case test if it is a
composite, or a plain delegation test if it is not. A change to a result
view needs the case where the views disagree, not only the case where
they happen to match. [`../testing/`](../testing/README.md) has the full
checklist by change type, and why line coverage alone proves none of the
contracts that actually matter here.
