<!-- Title: Shared Docs Never Name One Language's File -->
# A shared doc naming one language's file is a forced edit waiting to happen

> Eighteen files — seven sample specs, seven extending scenarios, both
> authoring templates, and `docs/architecture/README.md`'s own opening
> blockquote — all repeated a version of "Each language's own file in
> this directory — [`python.md`](python.md) today — is the actual
> code." Every one of them looked like a stable structural fact and was
> actually a forced edit the day a second language's file landed
> beside it.

## How it happened

Each fix earlier in the same session (splitting `docs/architecture.md`,
`docs/samples/`, `docs/extension.md` into per-scenario directories) was
correct on its own terms — the *file structure* really is additive now,
a new language really is a new file that touches nothing existing. But
the *prose introducing* that structure, written once per scenario as
each directory was created, kept restating the current state of the
directory by name: which language's file existed, and the word "today"
marking that as temporary. The directory stopped being editable per
language; the sentence describing it did not.

Nobody caught it at the time because each instance, read on its own,
looked like ordinary, true, present-tense narration — it did not read
as a bug until grepped for as a set and seen to be the same sentence,
copy-pasted with only the scenario name changed, 18 times.

## The actual test

Would this sentence need editing the day a second language's file
exists beside the one it names? If naming the file is required for the
sentence to be true (`"see [`python.md`](python.md) for the actual
code"` when `python.md` is the *only* implementation the spec is aware
of), the sentence is temporary narration wearing the voice of a
structural fact. If the sentence would stay word-for-word true with
two, three, or ten language files in the same directory, it's durable
and the file's own directory listing can carry the naming instead — not
a `Related` section either. Adding a `Related` bullet for a new
language is additive, not an edit, but it is still a touchpoint nothing
requires: `architecture/README.md`'s own Related docs briefly carried
exactly this bullet for `python.md`, and it came back out once someone
asked why a second language would need to come back and add a line just
to be listed somewhere the directory listing already shows it for
free.

## The fix, generalized

Not "genericize the wording" — delete the pointer. A spec doesn't need
to tell a reader that an implementation exists beside it; GitHub's own
directory rendering and the skill's own fetch-tier pairing already
guarantee that. The fix applied across all 18 files was to remove the
sentence outright, not rephrase it into something equally dated but
vaguer.

The one legitimate exception is a maintainer-facing *template*
document (`doc-authoring/samples.md`, `doc-authoring/extending.md`)
teaching the pattern with a concrete example — `` "python.md, for
instance" `` is fine there, because the reader is being shown what the
pattern looks like, not handed a doc that must itself stay accurate to
every language forever. Even there, "today" still comes out — the
illustration doesn't go stale when a second language's file exists;
only the temporal hedge would.

Full standing rule, including which lists a language-specific file is
still fine to appear in (a sample or scenario index, which enumerates
by design) versus which it isn't (a same-directory "Related" section):
[`docs/maintenance/doc-authoring/README.md`](../../docs/maintenance/doc-authoring/README.md#a-shared-language-agnostic-doc-never-names-one-languages-own-file).

Related: [`adding-a-variant-is-a-new-file`](adding-a-variant-is-a-new-file.md)
is the code/structure form of this same lesson; this is what it looks
like when the *prose describing* an already-correct structure quietly
breaks the same rule the structure itself follows.
