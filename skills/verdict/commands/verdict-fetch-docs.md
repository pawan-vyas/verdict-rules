---
description: Fetch verdict's deeper documentation at the version this project has installed, into the skill's own references directory.
---

<!-- markdownlint-disable MD041 (a Claude Code command file's title is its own filename and description field; the body starts with the instruction, not a heading) -->

Pull the documents the verdict skill does not bundle — the testing guide, the
language quickstart, the worked samples, and the worked example.

1. **Determine which language(s) to fetch for.** Look for each language's own
   manifest (`pyproject.toml`, `package.json`, `*.csproj`, `pubspec.yaml`) in
   the project. If more than one is found (a polyglot monorepo), ask the user
   which project(s) to fetch for rather than guessing. If no
   `references/<language>/agent-notes.md` exists in this skill for a found
   manifest, that language has no verdict SDK; skip it and inform the user.

2. **Run the script**, from inside the relevant project, passing every
   confirmed language:

   ```bash
   scripts/fetch-docs.sh <language> [<language> ...]
   ```

   It detects each language's installed version itself, resolves the
   `<language>-v<version>` tag, fetches every document this skill's manifest
   names for that language, and writes `references/<language>/.version`. If
   a version can't be detected or its tag doesn't exist, it says so and
   continues with the rest — it never falls back to the default branch.

3. **Report** what the script printed — how many documents were fetched per
   language, and which version. If anything was skipped, say so and continue
   with what is bundled.

Fetched documents keep their original repository-relative links. A link that
does not resolve locally resolves against the source repository at the same
tag: `https://github.com/pawan-vyas/verdict-rules/blob/<tag>/<path>`.
