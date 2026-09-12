#!/usr/bin/env python3
"""Assemble the skill eval set the harness reads from per-target sources.

The eval harness takes one JSON file listing every eval. Keeping that file
hand-maintained would mean every language's evals living in a single document
that each language branch edits — the conflict this repository restructures
everything else to avoid.

So the committed truth is one JSON file per eval, inside a directory named for
what it targets, and this script assembles them. Adding a language is a new
directory; adding an eval is a new file. Neither touches anything shared.

    python3 scripts/build_evals.py                 # every target
    python3 scripts/build_evals.py --target python # one target's evals only
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

WORKSPACE = Path("skills/verdict-workspace")
EVALS = WORKSPACE / "evals"
OUTPUT = EVALS / "evals.json"
SKILL_NAME = "verdict"

# A target directory holds eval definitions; this subdirectory holds the input
# files an eval hands to the agent, and is not itself a target.
FILES_DIR = "files"

REQUIRED = ("eval_name", "prompt", "expected_output", "expectations")


def targets() -> list[Path]:
    return sorted(d for d in EVALS.iterdir() if d.is_dir() and d.name != FILES_DIR)


def load(path: Path, target: str) -> dict:
    """One eval definition, validated and normalised for the harness."""
    data = json.loads(path.read_text())

    missing = [field for field in REQUIRED if field not in data]
    if missing:
        raise SystemExit(f"{path}: missing required field(s): {', '.join(missing)}")
    if "id" in data:
        raise SystemExit(
            f"{path}: carries an 'id'. Ids are assigned here so that adding a "
            f"target never has to renumber another one's evals."
        )

    # The harness keys its output directories off eval_name, so it has to be
    # unique across every target, not just within one.
    data["eval_name"] = f"{target}-{data['eval_name']}"

    # Paths are written relative to the target directory, because that is where
    # the author is looking; the harness resolves them from the workspace root.
    staged = []
    for entry in data.get("files", []):
        resolved = EVALS / target / entry
        if not resolved.exists():
            raise SystemExit(f"{path}: declares input file '{entry}', which does not exist")
        staged.append(str(resolved.relative_to(WORKSPACE)))
    data["files"] = staged

    return data


def build(selected: str | None) -> dict:
    chosen = [t for t in targets() if selected is None or t.name == selected]
    if selected is not None and not chosen:
        raise SystemExit(
            f"no target directory '{selected}'. Available: "
            f"{', '.join(t.name for t in targets())}"
        )

    evals = []
    for target in chosen:
        for source in sorted(target.glob("*.json")):
            evals.append(load(source, target.name))

    # Ids are positional and deliberately not stable across a target being
    # added. Nothing durable keys off them: every artifact the harness writes
    # is regenerable point-in-time material, and eval_name is the lasting name.
    for position, entry in enumerate(evals, start=1):
        entry["id"] = position

    order = ["id", "eval_name", "prompt", "expected_output", "files", "expectations"]
    return {
        "skill_name": SKILL_NAME,
        "evals": [{k: e[k] for k in order if k in e} for e in evals],
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--target", default=None, help="assemble only this target's evals")
    parser.add_argument("--output", type=Path, default=OUTPUT)
    args = parser.parse_args()

    assembled = build(args.target)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(assembled, indent=2) + "\n")

    print(f"{len(assembled['evals'])} eval(s) -> {args.output}")
    for entry in assembled["evals"]:
        print(f"  {entry['id']:>3}  {entry['eval_name']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
