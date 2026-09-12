---
description: Fetch verdict's deeper documentation at the version this project has installed, into the skill's own references directory.
---

Pull the documents the verdict skill does not bundle — the testing guide, the
language quickstart, the worked samples, and the worked example — so they are
available locally for the rest of this session and afterwards.

Follow these steps exactly.

1. **Determine the language** from the project's own manifest
   (`pyproject.toml`, `package.json`, `*.csproj`, `pubspec.yaml`). If no
   `references/<language>/agent-notes.md` exists in this skill, stop: verdict
   has no SDK for that language, and there is nothing to fetch.

2. **Read the installed version** of verdict from that manifest or its
   lockfile — not the latest release. The tag is `<language>-v<version>`,
   e.g. `python-v0.2.0`.

3. **If no matching tag exists, stop.** Do not fall back to the default
   branch. Documentation for a version the project does not have describes an
   API it does not have, which is worse than reading nothing. Say that the
   deeper documents were unavailable and continue with what is bundled.

4. **Fetch each `fetch` entry in `MANIFEST`** from
   `https://raw.githubusercontent.com/pawan-vyas/verdict-rules/<tag>/<source>`
   into `references/<destination>`, creating directories as needed.

5. **Record the tag** in `references/<language>/.version` so a later reader can
   tell whether these documents still match what is installed.

6. **Report** what was fetched and at which tag. If any fetch failed, say which
   and leave the rest in place — a partial fetch is fine, a silent one is not.

Fetched documents keep their original repository-relative links. A link that
does not resolve locally resolves against the source repository at the same
tag: `https://github.com/pawan-vyas/verdict-rules/blob/<tag>/<path>`.
