## What this changes and why

<!-- One or two sentences. -->

## Checklist

For changes that do not affect behaviour: documentation wording, repository
configuration, CI, tooling, chores.

If you find yourself wanting to tick something about the shared fixture or the
skill, this is the wrong template — the change probably affects behaviour, and
the default one applies.

- [ ] Scoped to one change.
- [ ] No library source touched (or if it was, only comments).
- [ ] Every relative link and anchor still resolves.
- [ ] Every mermaid diagram touched still validates.
- [ ] Any code sample added or changed was **executed**, not eyeballed.
- [ ] If `skills/verdict/**` was touched at all, `.claude-plugin/plugin.json`
      is bumped — CI will fail otherwise, and that is deliberate: it is the
      only signal a marketplace user gets that their copy is stale.
