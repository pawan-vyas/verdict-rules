#!/usr/bin/env python3
"""Parse every ``run:`` block in every workflow as shell.

A workflow's YAML can be perfectly valid while the shell inside a ``run:``
block is broken — they are different languages, checked by different things,
and nothing was checking the second one. That gap shipped a release workflow
with an unterminated quote: the YAML parsed, the job exited 2, and the failure
was only visible after a merge to main.

``bash -n`` parses without executing, so this is safe to run anywhere.
"""

from __future__ import annotations

import subprocess
import sys
import tempfile
from pathlib import Path

import yaml

WORKFLOWS = Path(".github/workflows")


def check(path: Path) -> list[str]:
    """Return a list of human-readable failures for one workflow file."""
    failures: list[str] = []
    document = yaml.safe_load(path.read_text()) or {}

    for job_name, job in (document.get("jobs") or {}).items():
        for index, step in enumerate(job.get("steps") or []):
            script = step.get("run")
            if not script:
                continue

            name = step.get("name", f"step {index}")
            with tempfile.NamedTemporaryFile("w", suffix=".sh") as handle:
                handle.write(script)
                handle.flush()
                result = subprocess.run(
                    ["bash", "-n", handle.name],
                    capture_output=True,
                    text=True,
                )

            if result.returncode != 0:
                detail = result.stderr.strip().splitlines()
                first = detail[0] if detail else "unknown error"
                # Strip the temp path, which is noise in an error message.
                first = first.split(": ", 1)[-1]
                failures.append(f"{path}: job '{job_name}', step '{name}': {first}")

    return failures


def main() -> int:
    files = sorted(WORKFLOWS.glob("*.yml")) + sorted(WORKFLOWS.glob("*.yaml"))
    if not files:
        print(f"No workflows found under {WORKFLOWS}/", file=sys.stderr)
        return 1

    failures = [failure for path in files for failure in check(path)]

    for failure in failures:
        print(f"::error::{failure}")

    if failures:
        print(f"\n{len(failures)} run block(s) are not valid shell.", file=sys.stderr)
        return 1

    print(f"Every run block in {len(files)} workflow(s) parses as shell.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
