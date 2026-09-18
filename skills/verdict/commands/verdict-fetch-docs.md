---
description: Fetch verdict's deeper documentation at the version this project has installed, into the skill's own references directory.
---

<!-- markdownlint-disable MD041 (a Claude Code command file's title is its own filename and description field; the body starts with the instruction, not a heading) -->

Pull the documents the verdict skill does not bundle — the testing guide, the
language quickstart, the worked samples, and the worked example.

1. **Determine which language(s) to fetch for.** Look for each language's own
   manifest (`pyproject.toml`, `package.json`, `*.csproj`, `pubspec.yaml`) in
   the project. If more than one is found (a polyglot monorepo), ask the user
   which project(s) to fetch for rather than guessing — `scripts/fetch-docs.sh`
   accepts any number of language/version pairs in one call. If no
   `references/<language>/agent-notes.md` exists in this skill for a found
   manifest, that language has no verdict SDK; skip it and inform the user.

2. **Read each installed version** from that language's own manifest or
   lockfile — not the latest release.

3. **Run the script**, passing every confirmed language and its version:

   ```bash
   scripts/fetch-docs.sh <language> <version> [<language> <version> ...]
   ```

   It resolves each `<language>-v<version>` tag, fetches every document this
   skill's manifest names for that language, and writes
   `references/<language>/.version`. If a tag does not exist, it reports that
   and continues with the rest — it never falls back to the default branch.

4. **Report** what the script printed — how many documents were fetched per
   language, and which version. If a tag was missing, say so and continue
   with what is bundled.

Fetched documents keep their original repository-relative links. A link that
does not resolve locally resolves against the source repository at the same
tag: `https://github.com/pawan-vyas/verdict-rules/blob/<tag>/<path>`.
