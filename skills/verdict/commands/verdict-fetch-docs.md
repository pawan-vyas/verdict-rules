---
description: Fetch verdict's deeper documentation at the version this project has installed, into the skill's own references directory.
---

<!-- markdownlint-disable MD041 (a Claude Code command file's title is its own filename and description field; the body starts with the instruction, not a heading) -->

Pull the documents the verdict skill does not bundle — the testing guide, the
language quickstart, the worked samples, and the worked example.

1. **Determine which language(s) to fetch for.** This skill ships
   `references/<language>/agent-notes.md` per language it has an SDK for —
   list `references/` to see which. For each, check whether the project
   actually uses that language (its own conventional manifest or project
   file). If more than one is found (a polyglot monorepo), ask the user
   which project(s) to fetch for rather than guessing. A language with no
   `agent-notes.md` here has no verdict SDK; say so rather than improvising.

2. **Determine the installed version for each language**, following that
   language's own `agent-notes.md` — its own package manager's canonical
   query, not a fixed set of files to parse.

3. **Run the script**, passing every confirmed `language=version` pair:

   ```bash
   scripts/fetch-docs.sh <language>=<version> [<language>=<version> ...]
   ```

   It resolves the `<language>-v<version>` tag, fetches every document this
   skill's manifest names for that language, and writes
   `references/<language>/.version`. If the tag doesn't exist, it says so and
   continues with the rest — it never falls back to the default branch. If
   that happens, re-check the version you determined in step 2 before
   assuming the release is missing.

4. **Report** what the script printed — how many documents were fetched per
   language, and which version. If anything was skipped, say so and continue
   with what is bundled.

Fetched documents keep their original repository-relative links. A link that
does not resolve locally resolves against the source repository at the same
tag: `https://github.com/pawan-vyas/verdict-rules/blob/<tag>/<path>`.
