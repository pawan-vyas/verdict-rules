<!-- Title: Docs Consolidate, Code Stays In Its Own Tree -->
# A fixture's docs and data can consolidate centrally; its code cannot

> When something that used to be scattered per-language gets
> centralized, the right boundary is **docs and data vs. code**, not
> "everything about this scenario." Docs and data carry no dependency
> on any language's own build or test tooling; code does, and moving it
> breaks that tooling silently rather than loudly.

## Where this came from

`graduation_verdict` is both a worked sample and a cross-language
parity fixture. The instinct, once its design narrative moved to
[`docs/samples/graduation-requirement-verdict/`](../../docs/samples/graduation-requirement-verdict/README.md)
and its data contract stayed at
[`fixtures/graduation_verdict/`](../../fixtures/graduation_verdict/README.md),
was to also move the Python *implementation* under
`fixtures/graduation_verdict/python/` for symmetry. That would have
silently broken two things at once: `uv run pytest`'s bare
auto-discovery only walks `python/`'s own tree, and
`test-python.yml`'s CI trigger only watches `git diff` under
`python/**` — a change to code moved outside `python/` would stop
triggering CI at all, with no error, just silence.

## The actual boundary

- **Docs and data** — a spec, a fixture's JSON files, a `README.md`
  describing what each expectation proves — depend on nothing but being
  readable. These consolidate freely: `docs/samples/<scenario>/`,
  `fixtures/<name>/`.
- **Code** — depends on that language's own build/test tooling
  discovering it from inside that language's own conventional root
  (`pytest`'s rootdir, an npm workspace's `package.json`, a cargo
  workspace's `Cargo.toml`). It stays inside that language's own
  top-level directory (`python/examples/graduation_verdict/` today),
  reading the shared fixture via a relative path — exactly as it
  already did before any of this session's restructuring.

## The real-world precedent

This is not a novel shape. Apache Arrow's cross-language integration
tests, gRPC's interop suite, and the JSON Schema Test Suite all keep
shared test data/specs in one place while each language's own
implementation stays inside that language's own tree, reading the
shared location by a relative path. None of them relocate a language's
source into the shared fixture directory.

## The test to apply next time

Before moving *anything* under a "consolidate this scenario" banner,
ask: does this specific file depend on a language's own tooling to be
discovered or run? If yes, it stays in that language's own tree no
matter how neatly it would otherwise fit alongside the docs and data
that just moved.

See [`adding-a-variant-is-a-new-file.md`](adding-a-variant-is-a-new-file.md)
for the general "new file, not a shared edit" shape this boundary is a
special case of, and
[`docs/maintenance/adding-a-fixture.md`](../../docs/maintenance/adding-a-fixture.md)
for the concrete template this produced.
