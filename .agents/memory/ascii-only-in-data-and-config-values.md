<!-- Title: ASCII-Only In Data And Config Values -->
# No em-dash, or other non-ASCII typographic punctuation, in a data/config value

> Found while auditing the shared fixture: `—` (a JSON-escaped
> em-dash) sitting in `fixtures/graduation_verdict/students.json`,
> `edge_cases.json`, and `.claude-plugin/plugin.json`'s `description`
> field. Not a bug — every conformant JSON parser decodes `—`
> identically to a raw em-dash — but investigating it surfaced the real
> concern: a config value is read by tooling, not by a person reading
> prose, and an escape sequence most readers don't recognize on sight is
> worse than just not putting the character there.

## The actual rule, and where the line sits

**Prose documentation, docstrings, and code comments**: em-dashes are
fine, expected, this repo's own established house style — the same
shape [`docs/adding-a-language.md`](../../docs/adding-a-language.md),
this whole `AGENTS.md`, and every doc in `docs/` already use throughout.
No change there.

**A JSON field, a YAML value a tool parses or displays** (a workflow
input's `description:`, a manifest field, a fixture's own data): plain
ASCII only. Use `--` where an em-dash would otherwise go. This is the
exact same shape as the mermaid-diagram skill's own emoji rule — fine in
a diagram node, restricted everywhere dispatch/parsing actually happens
— applied to punctuation instead of emoji.

**Why draw the line there and not at "anywhere in the repo"**: the repo
has roughly 2000 em-dashes across 137 markdown files, all of it
deliberate prose voice with zero evidence of causing any actual parsing
failure — audited directly (`grep` across every source extension) and
found no leaked escape sequence, no mojibake, nothing broken. Purging
those would reverse an established, working style for no real defect
found. The actual, verified risk is narrower: a value serialized by one
language's JSON/YAML library and consumed or re-serialized by another's,
where a human reading the raw file sees an opaque `\uXXXX` escape rather
than the character it represents.

## How to apply

- Writing or editing a JSON/YAML value meant for machine consumption:
  no em-dash, no other non-ASCII typographic punctuation (curly quotes,
  en-dash, ellipsis character) — plain ASCII, `--` where an em-dash
  would read naturally.
- Writing prose, a docstring, or a code comment: unchanged, em-dashes
  are this repo's own voice.
- A workflow input's `description:` field counts as a config value
  (GitHub parses and displays it), even though it lives inside a `.yml`
  file that also contains ordinary comments in the house prose style —
  judge by whether the string is consumed by tooling, not by the file
  extension it sits in.
