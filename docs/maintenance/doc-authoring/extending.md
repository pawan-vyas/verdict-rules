<!-- Title: Extending Scenario Doc Authoring Template -->
# Extending-scenario doc authoring template

> The structure every extension scenario's docs follow, in both
> halves — the language-agnostic spec files under
> [`../../extending/`](../../extending/) and their per-language
> implementation counterparts. This builds on the general standard in
> [`README.md`](README.md) — read that first for the rules that apply
> to every doc in this repo, not just extension scenarios. Closely
> follows the same spec-plus-per-language shape; the difference is what each
> category demonstrates, not the file shape.

## One directory per scenario

Every scenario is a directory, `docs/extending/<slug>/`:

- **The spec** — `docs/extending/<slug>/README.md`. Language-agnostic:
  the extension point, why it exists, and what any implementation of it
  must show. Written once, read by every language. Renders
  automatically on GitHub when linking to the directory itself.
- **The implementation** — `docs/extending/<slug>/<language>.md`, one
  per language that has written this scenario up (`python.md`, for
  instance). The real, concrete code for that language's own idiom.

A scenario that is architectural rather than a rule shape still earns a
small, generic `<language>.md` per language
([`domain-adapter-module/`](../../extending/domain-adapter-module/README.md)
is the current example) — illustrative code, not a contract, kept
deliberately generic (a rate-limiting or access-control illustration,
never a real named consumer's domain) so a reader isn't left with only
prose. Its spec still points at the
[`fixtures/`](../../../fixtures/README.md) scenarios, which are real,
full-scale instances of it. Only a scenario with no code worth showing
at all skips the `<language>.md` file entirely; nothing reserves the
slot for a language that hasn't written one.

## Spec file structure

In order:

1. **Title + blockquote framing** — what the extension point is. Never
   a pointer to a specific `<language>.md` by name or with a "today"
   hedge — see [`README.md`](README.md)'s durable-shared-doc rule.
2. **The extension point, explained** — what it lets a consumer do
   without changing this package's own source, and why the underlying
   type structure (a structural `Rule`, not a fixed hierarchy) is what
   makes it free.
3. **A diagram**, when the scenario has a shape worth showing — most
   scenarios do, since they're demonstrating a structural relationship
   (nesting, nothing catching an exception, an adapter boundary).
4. **What this demonstrates** — a checklist, one bullet per point the
   scenario proves.
5. **Related** — links to sibling scenarios, the architecture doc, and
   any [fixture](../../../fixtures/README.md) that is a concrete,
   full-scale instance of this scenario in practice.

## Implementation file structure

In order:

1. **Title + blockquote** pointing back to the spec ("read that first,"
   with a relative link).
2. **The code** — the real, concrete implementation in that language's
   own idiom, matching the spec's diagram and reasoning exactly enough
   that the two are recognizably the same scenario.
3. **Related** — the spec link, and any fixture whose implementation is
   a fuller worked version of the same code shown here.

An extending-scenario implementation file has no naive-versus-correct
contrast to draw — there is no "obvious first attempt" being critiqued
here, only the extension mechanism itself. Use a plain heading naming
what the code shows.

## Rules specific to this category

- **Real code in both halves, and it has to run.** The spec's diagram
  and prose are the language-agnostic part; a code block in the spec
  (rare) is illustrative shorthand rather than a contract every language
  matches verbatim, since each idiom expresses an extension mechanism
  somewhat differently. Every `<language>.md` block, though, is real code
  a reader will paste — execute it against the installed package before
  calling the file done, and compare any commented output against what
  actually printed. Six of the eight scenarios shipped broken code for
  three languages at once because nobody had run them.
- **Point at a fixture instead of duplicating one.** Where a
  [fixture](../../../fixtures/README.md) already demonstrates this
  scenario end-to-end, the scenario's own code stays a short,
  illustrative version and links to the fuller one rather than repeating
  it.

## Adding a new scenario

1. Create `docs/extending/<slug>/` and write the spec (`README.md`)
   first, following the structure above.
2. Write the first language's implementation doc (`<language>.md`)
   against it, in the same directory — or omit it, with a note in the
   spec, if the scenario is purely architectural.
3. Add both to the relevant "Related" sections of any sibling scenario
   the new one pairs or contrasts with.
4. Update [`../../extending/README.md`](../../extending/README.md)'s
   index.

## Adding a second language's implementation to an existing scenario

Add `docs/extending/<slug>/<language>.md` to the existing directory —
nothing else in it changes.
