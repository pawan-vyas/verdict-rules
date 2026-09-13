<!-- Title: Avoid Harness-Meaningful Filenames For Ordinary Docs -->
# A doc named after a harness's own convention can be silently misread by it

> `docs/maintenance/releases/skill.md` was renamed to
> `verdict-agent-skill.md` before it ever shipped, specifically because
> `skill.md` is not just a filename in this ecosystem — some harnesses
> treat a file with that exact name as a real skill definition to load,
> not prose to read.

## The risk, concretely

This repo's own AI-agent skill lives at
[`skills/verdict/SKILL.md`](../../skills/verdict/SKILL.md), and Claude
Code (among other harnesses) discovers skills partly by filename
convention. A maintainer doc that happens to be named `skill.md` --
describing *how* the skill is released, not a skill itself -- risks
being picked up by that same discovery mechanism, or at minimum reads
as ambiguous to a human skimming the tree. The failure mode is
harness-specific and easy to miss in review: nothing in the doc's own
content is wrong, only its name collides with a convention the content
was never meant to invoke.

## The fix, and why it works twice over

`verdict-agent-skill.md` names the specific thing being documented (the
`verdict` AI-agent skill's own release procedure) rather than reusing
the generic word a harness convention is keyed on. This also resolves
cleanly on GitHub for free: a directory's `README.md` renders
automatically when linking to the directory itself, so
[`docs/maintenance/releases/`](../../docs/maintenance/releases/README.md)
already serves as the index without needing `skill.md` to be memorable
on its own.

## The test to apply next time

Before naming any new doc, ask whether the exact filename means
something specific to a harness or tool this repo is read by (`SKILL.md`,
`AGENTS.md`, `CLAUDE.md`, a language's own manifest filename like
`package.json`) -- not just whether the name reads well to a human. A
name that is merely descriptive to a person can still be a trigger to a
machine.
