<!-- Title: Post-Generics Docs and Skill Rebalance Plan -->
# Post-generics rebalance: sample/extending doc bias, skill token economics

> Two follow-on problems the generics program (issue #33, PR #94)
> exposed but didn't itself need to fix, bundled into one future PR per
> the user's own framing. Nothing here is built yet — this records the
> evidence and the shape of the fix, not the fix itself.

## 1 · Docs dict-bias (an agent-facing risk, not a human-facing one)

**The problem, stated precisely**: dict-context is documented as
"exactly as first-class as a typed context, never a fallback for the
untyped" ([`docs/architecture/README.md`](../../../docs/architecture/README.md#generic-context)).
That's the correct design position. But an agent building with verdict
learns idiom by *pattern-matching against the samples it reads*, not by
reading the position statement in isolation — and the samples
overwhelmingly show only one of the two patterns.

**Measured, not assumed** (counted before this plan was written):

| Doc category | Dict-shaped | Typed-context shown |
| --- | --- | --- |
| `docs/samples/*/python.md` (10 samples) | 6 confirmed dict (`Rule[dict...]`/`context: dict`) | 0 — except `marketplace-eligibility`, the flagship generics showcase, not a "normal" sample |
| `docs/extending/*/python.md` (8 scenarios) | 7 confirmed dict | 1 (`reusing-a-rule-across-contexts`, added this cycle for exactly this reason) |

An agent skimming either index for "how do I write a rule" lands on
dict-context 6-7 times out of 7-8 before ever seeing the typed path.
That's a real bias risk **even though nothing in either doc is wrong**
— each sample is individually correct dict-context code. The skew is
statistical, not factual, which is exactly the kind of thing a doc
review checklist won't catch (nothing is *false*) but an agent's
learned prior will.

**What the fix is not**: rewriting samples to typed context, or
demoting dict-context's prominence. That would just invert the bias
and contradict the architecture doc's own stated position.

**What the fix likely is**: pick a small, deliberately-reasoned subset
of existing samples/scenarios whose own domain naturally reads better
typed — a fixed, known-shape context (e.g. an order or a shipment) is a
more natural `OrderContext`/`ShipmentContext` than an
`admin-eligibility-lookup`-style runtime-string-keyed catalog, which
structurally *wants* dict-context and should stay that way. The
question to answer per sample before touching it: does this domain's
context shape get decided by a human author ahead of time, or by
something the caller only knows the name of at runtime? The former
naturally reads typed; the latter naturally reads dict. Converting a
sample whose domain genuinely wants dict-context, just to balance a
count, would be the same mistake in the other direction.

**Candidates worth triaging first** (not a commitment, a starting
list): `shipping-fee-waiver` and `loyalty-tier-promotion` both check a
fixed, small set of known fields per entity — plausible typed
candidates. `admin-eligibility-lookup` and `data-driven-rule-sets` are
explicitly about a runtime-named catalog — stay dict, and the doc
should say so explicitly rather than leave it implicit.

## 2 · Skill token economics

Three separate but related sub-problems, all under
[`skills/verdict/`](../../../skills/verdict/):

### 2a · `MANIFEST.toml` repetition

201 lines, 28 `[[bundled]]`/`[[fetch]]`/`[[fetch_group]]` entries. The
dominant shape (18 of the 28) is:

```toml
[[fetch_group]]
readme = "docs/<category>/<topic>/README.md"
pattern = "docs/<category>/<topic>/{lang}.md"
```

`readme` and `pattern` are mechanically derivable from `<category>` and
`<topic>` alone in every one of those 18 cases — no entry needed either
field to deviate from the convention. A more compact shape (e.g. a
single `topic = "extending/reusing-a-rule-across-contexts"` line, with
the assembler expanding both paths from it) would cut each of those 18
entries from 3 lines to 1 without losing anything real.

**The constraint this has to preserve**: the file's own header states
the reason it's hand-authored rather than glob-discovered — *"adding a
doc to the repo must never silently enlarge the skill... an entry here
is a deliberate decision."* Any compression has to keep that property:
still one new line per topic, still a build-time or CI-time check that
every topic a hand-authored line claims actually resolves (already
covered by `scripts/skill_manifest.py`'s own row-generation, worth
re-verifying it still catches a typo'd topic after any format change).

**Confirmed, not a gap**: the two docs added this generics cycle
(`reusing-a-rule-across-contexts`, `marketplace-eligibility`) are
already correctly registered and resolve via `scripts/skill_manifest.py
fetch` — checked directly before writing this plan, not assumed.
Nothing to fix there; noted only because it was asked and worth having
on record.

### 2b · `SKILL.md` and `agent-notes.md` wording density

Not measured as rigorously as 2a yet — worth a real pass, not a guess,
before deciding what to cut. First target: the near-verbatim structural
boilerplate repeated across all four `references/<lang>/agent-notes.md`
files' own "Fetching the deeper documents" sections (confirmed via a
direct diff between Python's and Dart's before writing this plan) — the
explanation of bundled-vs-fetch-tier, the tag-resolution pattern, the
"record what you fetched in `.version`" convention are the same prose
skeleton four times, with only the per-language version-detection
command and path substituted.

**The real tension to resolve, not just cut**: each `agent-notes.md` is
bundled and read independently per language — a JS-only install
shouldn't have to pull in Python's own file just to read the shared
"how fetching works" paragraph. So this isn't a trivial find-and-delete;
it needs either (a) a shared fetched-once explainer doc each
`agent-notes.md` points at instead of restating, weighed against the
extra round-trip that costs an agent who only needed the one language's
notes, or (b) a genuinely shorter shared phrasing repeated 4x is
accepted as the right tradeoff once trimmed to its actual information
content. Decide which, with real numbers (current token count per file,
projected token count under each option), not by feel.

### 2c · What "token efficient" does not mean here

Not: stripping the "mistakes that show up in generated `<lang>`
specifically" sections — those are the highest-value part of each
`agent-notes.md`, this session's own dispatch-ladder incident is exactly
the kind of thing that section exists to prevent recurring. Compression
targets structural repetition and narration density, never the
per-language gotchas themselves.

## 3 · Related, deliberately left out of this plan's scope

[Issue #39](https://github.com/pawan-vyas/verdict-rules/issues/39) —
the naive-way sections in samples 3/4/6 (`shipping-fee-waiver`,
`loyalty-tier-promotion`, `data-driven-rule-sets`) don't land the same
"I recognize this" hook sample 2 (`dynamic-discounts`) does. Still open,
still valid, unaddressed by anything in the generics program. Genuinely
orthogonal to §1/§2 above (human-reader doc quality, not agent-facing
bias or token cost) but touches the same three files §1 already
identified as typed-context candidates — worth deciding whether it
folds into the same PR opportunistically or stays separately triaged,
not deciding that here.

## Related

- [`docs/architecture/README.md`](../../../docs/architecture/README.md#generic-context) —
  the "dict-context stays permanently first-class" position this plan's
  §1 fix must not contradict.
- [`docs/maintenance/adding-a-fixture.md`](../../../docs/maintenance/adding-a-fixture.md) —
  the three-home template any new or rebalanced sample follows.
- [`skills/verdict/MANIFEST.toml`](../../../skills/verdict/MANIFEST.toml) —
  the file §2a proposes compressing.
