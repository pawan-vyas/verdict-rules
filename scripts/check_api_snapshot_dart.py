#!/usr/bin/env python3
"""Fail if the committed Dart api-snapshot.json no longer matches live code.

Regenerates a fresh ``dart-apitool extract`` dump in memory (never
overwriting the committed file) and compares its normalized rows -- via
``api_snapshot_dart.py``'s own ``_extract_from_dump``, the same
normalization :func:`api_snapshot_dart.extract_symbols` applies to the
committed file -- against the committed snapshot's own rows. A mismatch
means a change to ``dart/packages/verdict_rules/lib/`` landed without
regenerating ``dart/packages/verdict_rules/api-snapshot.json``; see
docs/maintenance/api-snapshots.md for how to update it.

A raw byte/JSON diff of the two files would not work here: dart_apitool
stamps every extraction with its own fresh ``packageApi.packagePath`` (a
machine-specific temp directory, never the same value twice even for two
back-to-back extractions of identical source), so comparing the files
verbatim would fail on every single run regardless of whether the public
surface actually changed. Comparing through ``_extract_from_dump`` sidesteps
this entirely -- that function never reads ``packagePath`` at all.

Invoked from ``.github/workflows/test-dart.yml``. Needs ``dart-apitool`` on
PATH (``dart pub global activate dart_apitool``, matching the tool's own
documented installation method rather than a pubspec.yaml dev_dependency --
see this package's own pubspec.yaml for why).
"""

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from api_snapshot_dart import SNAPSHOT_PATH, _extract_from_dump  # noqa: E402


def _normalized(rows: list[dict[str, str | None]]) -> list[tuple[str, str | None]]:
    """Sort rows into an order-independent, directly comparable shape.

    Sorted on ``member or ""`` rather than ``member`` itself -- a type-level
    row's ``member`` is ``None``, which Python's default ordering can't
    compare against another row's string ``member`` at all.
    """
    pairs = [(row["type"], row["member"]) for row in rows]
    return sorted(pairs, key=lambda pair: (pair[0], pair[1] or ""))


def main() -> int:
    committed_rows = _normalized(_extract_from_dump(json.loads(SNAPSHOT_PATH.read_text())))

    with tempfile.TemporaryDirectory() as tmp_dir:
        live_snapshot_path = Path(tmp_dir) / "live-api-snapshot.json"
        subprocess.run(
            [
                "dart-apitool",
                "extract",
                "--input",
                str(SNAPSHOT_PATH.parent),
                "--output",
                str(live_snapshot_path),
            ],
            check=True,
        )
        live_rows = _normalized(_extract_from_dump(json.loads(live_snapshot_path.read_text())))

    if committed_rows == live_rows:
        print("Committed api-snapshot.json matches the live package's public surface.")
        return 0

    print(
        "::error::dart/packages/verdict_rules/api-snapshot.json no longer matches "
        "the live package's public surface. Regenerate it with "
        "'dart-apitool extract --input dart/packages/verdict_rules "
        "--output dart/packages/verdict_rules/api-snapshot.json' from the repo "
        "root, then commit the result. See docs/maintenance/api-snapshots.md.",
        file=sys.stderr,
    )
    added = sorted(set(live_rows) - set(committed_rows))
    removed = sorted(set(committed_rows) - set(live_rows))
    if added:
        print(f"  New in the live package, missing from the snapshot: {added}", file=sys.stderr)
    if removed:
        print(f"  In the snapshot, no longer in the live package: {removed}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
