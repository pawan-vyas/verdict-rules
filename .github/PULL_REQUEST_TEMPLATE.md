## What this changes and why

<!-- One or two sentences. Link the issue this addresses, if any. -->

## Checklist

- [ ] Scoped to one change — no unrelated fixes bundled in.
- [ ] Tests added/updated and passing locally (`uv run pytest` from `python/`).
- [ ] If this is a new `Rule` shape or run mode: short-circuit behavior
      and the vacuous-input case both have their own test, not just an
      assertion on the final `passed` boolean (see `docs/testing.md`'s
      checklist).
- [ ] If this touches `RuleResult`/`RunResult`'s shape or `Rule`'s
      required attributes: ran `docs/maintenance.md`'s consumer-impact
      checklist.
- [ ] Docs updated in this same PR if the change touches anything
      documented — not left for a follow-up.
- [ ] Still zero external dependencies, and nothing under a language's
      own package source references a specific domain (rate limiting,
      access grants, etc.) — see `AGENTS.md`.
