<!-- Title: Main Requires A Review, So Merges Use Admin -->
# `main` requires an approving review; the maintainer merges with `--admin`

> Branch policy on `main` blocks a normal merge of any PR until it is
> approved, and the repo has one maintainer. A merge therefore needs
> `gh pr merge <n> --merge --admin`, and merging a PR that bumps a version
> *is* the release.

## What to know

- `--admin` is the maintainer's call. An agent's `--admin` merge can be
  refused by the harness's permission check even after the maintainer asked
  for it in conversation; when that happens, hand over the exact command
  for them to run (typed with the `!` prefix) rather than looking for
  another route to the same merge.
- Squash merging is disabled on the repo; use `--merge`.
- Pass the commit subject and body as `--subject` and `--body-file`, never
  inline: authored text with backticks is executed by the shell (see
  [incident 001](../incidents/001-release-notes-mangled-by-shell.md)).
- A PR that touches only evals or docs releases nothing; one that bumps
  `plugin.json` or an SDK manifest releases that target on merge, and the
  workflows create the tag themselves.
