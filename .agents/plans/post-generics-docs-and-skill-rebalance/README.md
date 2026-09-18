<!-- Title: Post-Generics Docs and Skill Rebalance Plan -->
# Post-generics rebalance: sample/extending doc bias, skill token economics

> Two follow-on problems the generics program (issue #33, PR #94)
> exposed but didn't itself need to fix, bundled into one PR (#95) per
> the user's own framing: "full completion, no deferring." This doc
> started as a plan and is now also the running record of what actually
> shipped in that PR — §0 tracks progress, §1–§3 are the original
> problem statements, §4 is a scope addition raised mid-implementation.

## 0 · Progress (updated as PR #95 lands each piece)

**Done, committed, verified:**

- Core lib comment trim, all four languages (rationale/justification
  prose cut from every doc comment and body comment; current-behavior
  facts only).
- `loyalty-tier-promotion` sample: naive-way section rewritten to a
  visible-on-sight bug (not prose-explained), converted to a typed
  `LoyaltyContext` — the first real §1 fix, all four languages,
  every code sample compiled/run-verified.
- `shipping-fee-waiver` sample: converted to a typed `ShippingContext`
  — the second §1 fix, all four languages, every code sample
  compiled/run-verified. Showcases a multi-field aggregate context
  where one field is an injected dependency (`promoCodeService`),
  typed as a `Protocol`/interface rather than `Any`/`object`/`unknown`,
  per the user's own framing of what this sample should demonstrate.
  The naive-way section already landed the "I recognize this" hook
  (§3) on inspection — no rewrite needed there.
- `graduation_verdict` and `marketplace_eligibility` example fixtures:
  same comment trim as core lib, all four languages now done. Caught
  and fixed two real bugs while trimming C#: a misplaced XML doc
  comment left over from the dispatch-table refactor (documented the
  wrong member), and a version-detection regex that never matched a
  real consumer `.csproj`'s `<PackageReference>` shape.
- §2a (`MANIFEST.toml`): compressed 201 → 67 lines via three new
  shorthand shapes (`[topics]` grouped by category, `docs` for
  destination-equals-source one-offs, `[quickstarts]` for the
  per-language destination-is-always-`<lang>/quickstart.md` case),
  verified data-equivalent at every step (exact fetch-row-count and
  fetch-catalog-per-language-count match before/after, not just "it
  still builds"). The file's own explanatory comments cut to the same
  bar as source — it's itself shipped and read by agents.
- §2b, resolved differently than either option the plan first
  considered: not a shared reference doc, not just tightened prose in
  place — a real shipped script. `skills/verdict/scripts/fetch-docs.sh`
  now does the actual fetch (reading a build-time-generated
  `fetch-catalog.tsv`, itself derived from `MANIFEST.toml` — no TOML
  parsing needed on the consumer side) and auto-detects each language's
  installed version itself, so every `agent-notes.md`'s "Fetching the
  deeper documents" section is one line:
  `scripts/fetch-docs.sh <language>`. Supports multiple languages in one
  call for a polyglot monorepo (confirmed the user's own framing: ask
  which project(s) before fetching, don't guess). Verified end to end
  against the real built `dist/verdict.skill`, not simulated — all four
  languages' auto-detection from real consumer project shapes, the
  multi-language call, the explicit-override syntax, and the
  no-project-detected path, including a real `set -e` bug caught and
  fixed only because of that real testing (a failed detection used to
  abort the whole script instead of skipping that language).
- `SKILL.md`'s "what the engine guarantees" list trimmed to the same bar,
  inline link to `docs/extending/isolating-flaky-predicates/` dropped.
- §4 (eval bias audit + new adherence evals): all 23 existing evals
  read and found not to carry the bias — each domain's dict-vs-typed
  shape already matches its sample counterpart's own deliberate
  choice. One new `employee-bonus-eligibility` adherence eval added
  per language (27 total now), assembled and validated.

**Not yet done, still in this PR's scope (no deferring, per explicit
instruction):**

- Language package version bump to `0.3.1` and skill version bump to
  `0.6.1`, per the user's own instruction once the comment-trim scope
  was discovered ("this needs fixing in this PR itself, so it will cut
  a v0.3.1 release for all, along with skill v0.6.1").
- Final full-suite verification across all four languages plus the
  eval runs (§4), before push and merge.

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

## 3 · Related, deliberately left out of this plan's original scope

[Issue #39](https://github.com/pawan-vyas/verdict-rules/issues/39) —
the naive-way sections in samples 3/4/6 (`shipping-fee-waiver`,
`loyalty-tier-promotion`, `data-driven-rule-sets`) don't land the same
"I recognize this" hook sample 2 (`dynamic-discounts`) does. Genuinely
orthogonal to §1/§2 (human-reader doc quality, not agent-facing bias or
token cost) but touches the same files §1 already identified as
typed-context candidates. **Folded in for `loyalty-tier-promotion`**
(the naive-way rewrite and the typed-context conversion landed together
in one pass, per explicit instruction). **Re-examined for
`shipping-fee-waiver`** during its own typed-context conversion and
found to already land the hook: the naive code's `await` on an external
service call sits visibly first in the ladder, ahead of the two cheap
checks — recognizable by inspection, the same bar the fix applies, so
no separate rewrite was needed there. Still open for
`data-driven-rule-sets` (not one of §1's typed-context candidates,
so untouched by either pass so far).

## 4 · Scope addition: eval bias audit + adherence evals (raised mid-implementation)

Not in the plan's original two problems — raised once §1's actual fix
(the loyalty-tier-promotion conversion) was underway and it became
concrete that the skill's own evals could carry the identical bias §1
diagnoses in the docs, just in a different shipped surface.

**Checked, not assumed**: read all 23 existing eval files
(`skills/verdict-workspace/evals/{python,js,dart,csharp,cross-language}/*.json`).
None currently mention or grade dict-context vs. typed-context at all —
so there is no active bias today in what's graded, but also no
adherence check. Confirmed via each file's real schema (`prompt`,
`expected_output`, `files`, `expectations` — not `assertions`, despite
that being the field name skill-creator's own generic instructions use;
this repo's own evals use `expectations`, a list of qualitative
checkpoints a grader reads against).

**Two things to do, both explicitly requested — both now done:**

1. **Audit the 23 existing evals**, the same question asked per eval
   as §1 asked per doc: does this domain have a knowable-ahead-of-time
   context shape, or a runtime-named one, and does the eval's own
   `expectations` implicitly reward one without saying so? Read all
   23 against that question — findings:
   - `discount-eligibility` (all four languages) and js's own
     `cdn-conditional-ui`: both describe an eligible-*region set* and
     a threshold as campaign-configurable values — the same shape
     `dynamic-discounts` documents as a deliberate dict-context stay,
     not a typed-context miss. Correctly modeled; no bias.
   - `data-driven-admin-rules` (all four) and `absence-versus-emptiness`
     (all four): both explicitly about a runtime-named or
     admin-configured catalog — the `data-driven-rule-sets`/
     `admin-eligibility-lookup` shape, which stays dict-context by
     design. Correctly modeled; no bias.
   - `shipping-fee-waiver` (all four): the underlying sample doc is now
     typed (this same PR, above) but the eval's own `expectations`
     never mentioned context shape either before or after — it grades
     `OrRule` ordering and the short-circuit proof, which is shape-
     agnostic. No change needed.
   - `flutter-conditional-banner` (dart-only): three fixed boolean
     flags on one profile — structurally a typed-context-friendly
     shape, closer to `loyalty-tier-promotion` than to
     `dynamic-discounts` — but the eval's own focus is the
     Flutter-`build()`-must-stay-synchronous problem, a different hard
     case entirely; folding a context-shape check in here would blur
     what it actually tests. Left as is; the new adherence eval below
     covers the shape question on its own instead.
   - No existing eval's `expectations` mentions "dict"/"typed"/"Map"/
     generic-parameter shape at all — confirms the earlier read (no
     active bias in what's graded today), and confirms every current
     domain's dict-vs-typed shape matches what its sample counterpart
     already models, deliberately, not by omission.
2. **Added one new eval per language**
   (`employee-bonus-eligibility`, `evals/{python,js,csharp,dart}/0{6,7}-employee-bonus-eligibility.json`),
   purpose-built to test real adherence: a fixed, known set of four
   context fields on one employee record (the same shape
   `loyalty-tier-promotion` now models), explicitly contrasted in its
   own prompt against an admin-configurable catalog ("there's no
   admin-configurable or per-tenant set of criteria here"), with an
   `expectations` entry checking whether the response reached for a
   typed context — or explicitly justified staying dict-context —
   rather than a blanket "must use typed" rule that would itself be a
   new bias in the other direction. Assembled and validated via
   `scripts/build_evals.py` (23 → 27 evals, every declared `files`
   path resolves).

**Validation pass, explicitly sequenced last**: once everything else in
this PR lands and is committed, run 1–2 evals per language through a
real subagent to confirm end-to-end usability against the actual
shipped state (including the new `fetch-docs.sh` script and the
rebalanced samples) — using the **haiku model**, per the user's own
reasoning: the library, docs, and skill are mature enough now to hold up
respectably on a smaller model, and that's a more honest test of
whether the skill's own guidance carries the weight than always
grading against a frontier model. Run sequentially, never in parallel
(rate-limit risk — see
[`.agents/memory/`](../../memory/) if a durable note on this doesn't
already exist there). This is genuinely a **validation** pass, not a
grading/scoring exercise like skill-creator's own iteration loop — the
question is "does this actually work," not "which iteration scored
higher."

## Related

- [`docs/architecture/README.md`](../../../docs/architecture/README.md#generic-context) —
  the "dict-context stays permanently first-class" position this plan's
  §1 fix must not contradict.
- [`docs/maintenance/adding-a-fixture.md`](../../../docs/maintenance/adding-a-fixture.md) —
  the three-home template any new or rebalanced sample follows.
- [`skills/verdict/MANIFEST.toml`](../../../skills/verdict/MANIFEST.toml) —
  the file §2a proposes compressing.
