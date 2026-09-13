<!-- Title: Extending Scenario Doc Authoring Template -->
# Extending-scenario doc authoring template

> The structure every extension scenario's docs follow, in both
> halves — the language-agnostic spec files under
> [`../../extending/`](../../extending/) and their per-language
> implementation counterparts. This builds on the general standard in
> [`README.md`](README.md) — read that first for the rules that apply
> to every doc in this repo, not just extension scenarios. Closely
> mirrors [`samples.md`](samples.md); the difference is what each
> category demonstrates, not the file shape.

## One directory per scenario

Every scenario is a directory, `docs/extending/<slug>/`:

- **The spec** — `docs/extending/<slug>/README.md`. Language-agnostic:
  the extension point, why it exists, and what any implementation of it
  must show. Written once, read by every language. Renders
  automatically on GitHub when linking to the directory itself.
- **The implementation** — `docs/extending/<slug>/<language>.md`, one
  per language that has written this scenario up (`python.md` today).
  The real, concrete code for that language's own idiom.

A scenario that is purely architectural — no per-language code to show
beyond what a sample already demonstrates concretely
([`domain-adapter-module/`](../../extending/domain-adapter-module/README.md)
is the current example) has no `<language>.md` file at all; its spec
points at the samples that are real instances of it instead. Nothing
reserves the slot for a language that hasn't written one.

## Spec file structure

In order:

1. **Title + blockquote framing** — what the extension point is, and a
   pointer to `<language>.md` in this same directory as "the concrete
   code," when one exists.
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
   any sample that is a concrete instance of this scenario in practice.

## Implementation file structure

In order:

1. **Title + blockquote** pointing back to the spec ("read that first,"
   with a relative link).
2. **The code** — the real, concrete implementation in that language's
   own idiom, matching the spec's diagram and reasoning exactly enough
   that the two are recognizably the same scenario.
3. **Related** — the spec link, and any sample whose implementation is a
   fuller worked version of the same code shown here.

Unlike a sample doc, an extending-scenario implementation file has no
naive/`verdict` contrast to draw — there is no "obvious first attempt"
being critiqued here, only the extension mechanism itself. Use a plain
heading naming what the code shows, not "The `verdict` way".

## Sample-specific rules, on top of the general standard

- **Real code in both halves, not pseudo-code in the spec.** Unlike a
  sample spec, an extending-scenario spec's own diagram and prose are
  the language-agnostic part; a code block that appears in the spec
  (rare) is illustrative shorthand, not a contract every language must
  match verbatim, since these scenarios describe an extension mechanism
  each language's own idiom expresses somewhat differently.
- **Point at a sample instead of duplicating one.** If a full worked
  sample already demonstrates this scenario end-to-end
  (`data-driven-rule-construction` pointing at
  [`../../samples/data-driven-rule-sets/`](../../samples/data-driven-rule-sets/README.md)
  is the current example), the scenario's own code stays a short,
  illustrative version and links to the sample for the fuller one,
  rather than repeating it.

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
