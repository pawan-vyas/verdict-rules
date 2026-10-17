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
- **A link's label can name a file that does not exist while the link
  itself is valid, and nothing anywhere reports it.** A link checker
  resolves the target and passes; an anchor checker passes; a bare-mention
  sweep skips it because it *is* a link. The root `README.md` carried four
  of these at once — `` [`python/README.md`](python/packages/verdict-rules/README.md) ``
  and the same for three more languages, where no `<language>/README.md`
  file has ever existed. A reader sees a path, trusts it, and cannot find
  it. The check is its own pass: for every link whose label is a
  backtick-quoted path, resolve **the label** as well as the target, and
  require them to agree.
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
3. A label-versus-target pass: wherever a link's label is itself a
   backtick-quoted path, resolve that path too and require it to match
   what the link points at. **This one is now a gate** --
   `scripts/check_link_labels.py`, run by `check-release-readiness.yml`
   on every pull request -- so it no longer needs doing by hand.

   This repo's labels are deliberately **shortened**, so the check needs
   that convention built in or it reports mostly noise. A label names the
   meaningful suffix of the target and drops the `../` climb and the
   `README.md` leaf — `` [`testing/`](../../testing/README.md) ``,
   `` [`SKILL.md`](../../skills/verdict/SKILL.md) ``. A suffix match is
   correct; the failure to flag is a label that is a *complete path claim*
   and still wrong, which is either a path that never existed
   (`python/README.md`) or a relative path with the wrong number of
   levels. Python's own quickstart carried the second kind — `../../`
   labels on four-level targets, stale from before the package moved
   under `packages/` — while every other language's quickstart wrote the
   full climb correctly.
4. A dedicated bare-mention sweep, separate from all of the above,
   since a bare mention is invisible to link/anchor checking by
   definition.
5. Manual judgment on every bare-mention hit — a filename naming a
   *pattern* ("each package's own `CHANGELOG.md`") or a file that does
   not exist yet is correctly left bare; only genuine references to one
   specific, currently-existing file or section get linked.

None of this needs a heavyweight tool — a short Python script per check
is enough. Write one to `.agents/scratch/` first and see what it finds;
promote it to `scripts/` only once it has earned it, which means verified
in **both** directions: clean on a correct tree *and* firing on the real
defect, reintroduced deliberately. A check that only ever passes is
indistinguishable from a no-op, and the first draft of
`check_link_labels.py` was exactly that — it blanked out code spans
before looking for links, and since every label here is a backtick-quoted
path, it erased the thing it was searching for and reported a clean tree
forever. It was caught by re-introducing a known defect and noticing the
gate stayed green.

The discipline that matters is running all of it, in this order, rather
than trusting a single grep pass to have caught everything.
