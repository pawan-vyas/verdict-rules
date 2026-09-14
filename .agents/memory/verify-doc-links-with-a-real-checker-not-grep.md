<!-- Title: Verify Doc Links With A Real Checker, Not Grep -->
# A large doc restructuring needs a real link/anchor checker, not grep

> Grep-based staleness sweeps have specific, repeatable blind spots —
> not "grep is imprecise" in the abstract, but concrete failure modes
> worth naming so the next large restructuring doesn't rediscover them
> by shipping a broken link.

## The blind spots, each one actually hit

- **Unanchored substring patterns miss prefixed variants.** A grep
  pattern built as `(architecture\.md)` (literal parens included, to
  match a markdown link's closing syntax) does not match
  `(../architecture.md)`, because the substring `(architecture.md)`
  genuinely does not appear inside `(../architecture.md)` — there is a
  `../` in between. Every anchored grep pattern needs to be re-run
  unanchored (a bare substring search) as a second pass, or it silently
  skips every reference one directory level away from where the
  pattern was written.
- **Bare backtick-quoted filenames are invisible to every link
  checker**, real or grep-based, because they are not links at all —
  `` `extension.md` `` mentioned in prose, never wrapped in
  `[...](...)`. No tool that walks `]( ... )` syntax will ever see one.
  This is why bare mentions need their own dedicated sweep, and why
  [`docs/maintenance/doc-authoring/README.md`](../../docs/maintenance/doc-authoring/README.md#every-mention-of-another-doc-is-a-link-never-a-bare-filename)
  now requires every doc mention to be a real link — the standing fix,
  not just the one-time sweep.
- **A naive anchor-slug generator produces false positives around
  em-dashes.** GitHub's real heading-to-anchor algorithm strips
  punctuation but does not collapse the resulting double space before
  turning spaces into hyphens, so `"Recipe 2 — a genuinely new rule
  shape"` becomes `recipe-2--a-genuinely-new-rule-shape` (double
  hyphen). A slugifier that collapses whitespace first produces
  `recipe-2-a-genuinely-new-rule-shape` (single hyphen) — technically
  wrong, and it flags dozens of already-correct anchors as broken.
  Verify against GitHub's actual algorithm, not an intuitive
  approximation of it, before trusting a "broken anchor" report.
- **Extended link labels produce false positives in naive bare-mention
  detection.** A regex checking "is this backtick-quoted filename
  immediately followed by `](`" misses a real, correct link whose label
  extends past the filename, like
  `` [`AGENTS.md`'s "What this repo is" section](AGENTS.md#what-this-repo-is) `` —
  the filename is followed by `'s "..." section]`, not `](`, so it
  looks bare when it is not.
- **Double-backtick code spans defeat any regex-based link scanner.**
  `` `` [`extension.md`](../extension.md) `` `` — used deliberately to
  show a reader the *syntax* of a link as prose, not to create a real
  one — still contains `](...)` and gets flagged as a real link with a
  wrong relative path by any checker that does not understand
  Markdown's own code-span escaping.

## What an actual verification pass requires

1. A real link-existence checker: resolve every `](path)` against the
   filesystem, from the *linking* file's own directory, not the repo
   root.
2. A real anchor checker: extract every heading from the *target* file
   and slugify it with GitHub's actual algorithm (strip punctuation,
   replace spaces with hyphens, **do not** collapse repeated hyphens).
3. A dedicated bare-mention sweep, separate from both of the above,
   since a bare mention is invisible to link/anchor checking by
   definition.
4. Manual judgment on every bare-mention hit — a filename naming a
   *pattern* ("each package's own `CHANGELOG.md`") or a file that does
   not exist yet is correctly left bare; only genuine references to one
   specific, currently-existing file or section get linked.

None of this needs a heavyweight tool — a short Python script per check
is enough, and disposable (write it to `.agents/scratch/`, not
committed). The discipline that matters is running all of it, in this
order, rather than trusting a single grep pass to have caught
everything.
