<!-- Title: Public API Snapshots -->
# Public API snapshots

> What catches an accidental breaking change or a silent drift between
> languages before either ships -- one committed surface file per
> package, each language's own real tooling generating it, plus a
> cross-language diff with an explicit allow-list for every genuine
> difference.

## Per-language snapshot, each language's own real tool

No hand-rolled reflection script here -- each language already has a
maintained tool built for exactly this, and using it means this repo
benefits from that tool's own correctness work instead of re-deriving it:

| Language | Tool | Committed snapshot |
| :-- | :-- | :-- |
| C# | [`Microsoft.CodeAnalysis.PublicApiAnalyzers`](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md) | `csharp/src/VerdictRules/PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` |
| Python | [`griffe`](https://mkdocstrings.github.io/griffe/) | `python/packages/verdict-rules/api-snapshot.json` (`griffe dump`) |
| JS/TS | [`@microsoft/api-extractor`](https://api-extractor.com/) | `js/packages/verdict-rules/etc/verdict-rules.api.md` |
| Dart | [`dart_apitool`](https://pub.dev/packages/dart_apitool) | `dart/packages/verdict_rules/api-snapshot.json` (`dart-apitool extract`) |

Each tool's own build/CI step fails if the live code's public surface no
longer matches its committed snapshot -- a declared public symbol that
disappeared, or a new one nobody acknowledged. Updating the snapshot is
part of making the change, the same way updating a changelog is: both
are how a reviewer sees what actually changed without reading a diff of
every source file.

## The cross-language layer: a concept map, not a format-level diff

The four tools above produce four genuinely different native formats
(C#'s flat `Type.Member` lines, Python's nested JSON, TypeScript's
pseudocode `.api.md`, Dart's own JSON model) and four different naming
conventions for the *same* concept (`EvaluateAsync` vs. `evaluate`,
`RuleNames` vs. `rule_names` vs. `ruleNames`). Diffing the raw formats
against each other would be mostly naming-convention noise, not signal.

[`api-concepts.yaml`](api-concepts.yaml) is the normalized layer: one row
per concept that exists in every language, each with that concept's own
spelling per language. [`scripts/check_api_snapshots.py`](../../scripts/check_api_snapshots.py)
reads each language's native snapshot (via a small per-language
extractor converting it to a flat `(type, member)` set), then asserts:

1. **Every concept row resolves in all four snapshots** -- except a
   language whose own cell is `""`, which means "not tracked here," not
   "missing." That reads as an absence claim only when the language
   genuinely has no such symbol (`RulePredicate` has no Python
   counterpart, since Python needs no named type for a plain callable).
   It also legitimately covers a real tool blind spot: every "has a
   readable string/debugger representation" row leaves Dart's cell empty
   because `dart_apitool` drops every `@override` member outright, so
   `toString()` is invisible to it in its entirety even though the
   method genuinely exists and is covered by that language's own
   diagnostics tests. This mechanism only asserts what a given tool can
   re-confirm on every future change. A concept
   silently missing from a language's actual exported surface, where the
   tool *can* see it, is exactly the drift this exists to catch.
2. **Every symbol a snapshot actually exports is accounted for** --
   either by a concept row, or by an entry in
   [`api-surface-allowlist.yaml`](api-surface-allowlist.yaml) carrying a
   one-line reason. A new public symbol with neither fails the gate,
   forcing an explicit decision (add it to the concept map because every
   language should have it, or allow-list it with a stated reason)
   rather than letting it silently exist unclassified.

## Updating either file

- **A new concept shared by every language** (a new method, a new type):
  add one row to `api-concepts.yaml` with that concept's spelling in each
  language, after implementing it everywhere.
- **A genuinely language-specific addition** (nothing like JS's
  `UnknownLookupError`, which the other three cover with a built-in
  exception type instead): add one row to `api-surface-allowlist.yaml`
  stating why it has no counterpart elsewhere.
- **Either file growing without the other matching concept landing in
  every language** is the signal this mechanism exists to raise -- see
  it as a prompt to ask "should this exist everywhere?" before reaching
  for the allow-list as the easy way past a failing gate.
