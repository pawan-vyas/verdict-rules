<!-- Title: Sample Doc Authoring Template -->
# Sample doc authoring template

> The structure every sample scenario's docs follow, in both halves —
> the language-agnostic spec files under
> [`../../samples/`](../../samples/) and their per-language
> implementation counterparts. This builds on the general standard in
> [`README.md`](README.md) — read that first for the rules that apply
> to every doc in this repo, not just samples.

## One directory per scenario

Every scenario is a directory, `docs/samples/<slug>/`:

- **The spec** — `docs/samples/<slug>/README.md`. Language-agnostic: the
  problem, the design a correct solution demonstrates, and what any
  implementation of it must show. Written once, read by every language.
  Renders automatically on GitHub when linking to the directory itself.
- **The implementation** — `docs/samples/<slug>/<language>.md`, one per
  language that has built this sample (`python.md` today). Real,
  runnable-shaped code in that language's own idiom, plus the
  language-specific variant of the naive approach it's replacing.

A doc isn't bound by a language's own build/test tooling the way its
code is, so a sample's per-language documentation has no reason to live
inside that language's own directory tree — it lives here, next to the
spec, regardless of which language it documents. A language that hasn't
built a given sample yet simply has no file for it in that directory;
nothing reserves the slot.

A sample backed by a full tested project rather than a markdown code
block (graduation-requirement-verdict is the current example) keeps its
*code* in that language's own idiomatic location
(`python/examples/graduation_verdict/` today) and, if the scenario is
also a cross-language parity fixture, its shared data contract under
`fixtures/<name>/` — see
[`../adding-a-fixture.md`](../adding-a-fixture.md). Only the *docs*
consolidate here. Its implementation file has no inline naive/`verdict`
code pair to contrast, so "The `verdict` way" heading doesn't apply —
it points at the real project instead (a heading naming what it maps,
like "Where the design lives in the code," fits this case).

## Spec file structure

In order:

1. **Title + blockquote framing** — what the scenario is, and a pointer
   to `<language>.md` in this same directory as "the actual code."
2. **The question** — one line, plain language, the exact yes/no or
   selection the scenario resolves.
3. **Why it's a good fit** — one short paragraph naming the shape of the
   problem (an AND of independent conditions, an OR of qualifying paths,
   a data-driven rule set, ...) and why that shape is what makes a rule
   engine's guarantees matter here, rather than being incidental to the
   example.
4. **What the naive approach gets wrong** — a pseudo-code block (generic
   syntax, not a real language) shaped like the obvious first
   implementation, followed by bullets naming its actual, concrete
   engineering cost, per [`README.md`](README.md)'s "argue from cost, not
   taste" rule.
5. **The `verdict` way** — the shape of the fix, a diagram, and a short
   paragraph on what the correct shape makes testable in isolation that
   the naive shape doesn't. Not "The design a correct solution
   demonstrates" or any other generic phrasing — these docs demonstrate
   `verdict` specifically, not a general-purpose curriculum, and the
   heading should say so.
6. **What a solution must demonstrate** — a checklist, one bullet per
   point raised above, always including one bullet specifically about
   independent unit-testability.
7. **Related** — links to sibling specs this one pairs or contrasts with.

## Implementation file structure

In order:

1. **Title + blockquote** pointing back to the spec ("read that first,"
   with a relative link).
2. **The naive way (and why it breaks down)** — real code in that
   language, matching the spec's naive shape exactly enough that the two
   are recognizably the same example. One sentence pointing back to the
   spec for the full reasoning — never restate the spec's bullets here.
3. **The `verdict` way** — the real, correct, runnable-shaped
   implementation. Not "The code" — mirror the spec's own heading (§5
   above) rather than a generic label, and keep the naive/`verdict`
   contrast visible in both files' headings.
4. **Related** — the spec link, sibling samples in this language, and
   any architecture/extension doc anchor the sample is a worked instance
   of.

## Sample-specific rules, on top of the general standard

- **Pseudo-code in specs, real code in implementations.** A spec's code
  block is illustrative and language-neutral; only the implementation
  file's code is expected to run.
- **The naive-approach critique argues from concrete engineering cost,
  not taste.** The defect is never "this looks messy" or "this could be
  hard to change someday." It's a specific, present-tense cost: editing
  shipped, already-correct code to add a path (regression risk), a
  decision made by one person that the next person inherits with no
  record of why (knowledge loss), or a real resource cost incurred right
  now (an API call nothing needed to make). Prefer the cost that is
  already true today over one that depends on some hypothetical future
  edit.
- **Testability is argued, not asserted.** The design section's
  testability paragraph names *what specifically* becomes independently
  testable and *why* the naive shape lacks that seam — not just a claim
  that the correct shape is "more testable." The checklist bullet in
  "What a solution must demonstrate" is the same claim, compressed to
  one line.

## Adding a new sample

1. Create `docs/samples/<slug>/` and write the spec (`README.md`)
   first, following the structure above.
2. Write the first language's implementation doc (`<language>.md`)
   against it, in the same directory.
3. Add both to the relevant "Related" sections of any sibling sample the
   new one pairs or contrasts with.
4. Update [`../../samples/README.md`](../../samples/README.md)'s
   index, and [`../../architecture/`](../../architecture/README.md) or
   [`../../extending/`](../../extending/README.md) if the sample is a
   worked instance of a scenario documented there.

## Adding a second language's implementation to an existing sample

Add `docs/samples/<slug>/<language>.md` to the existing directory —
nothing else in it changes. If that language's own real code lives
somewhere other than a markdown code block, the implementation doc
points at it (see [`../adding-a-fixture.md`](../adding-a-fixture.md)
for the fixture-backed case).
