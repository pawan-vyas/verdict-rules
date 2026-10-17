/**
 * Unit tests for shrink.js's own mechanism -- independent of any real or
 * injected engine bug. These prove the shrinker's algorithm itself
 * (candidate generation, the fixed-point loop, fixture writing) behaves
 * correctly in isolation, using synthetic `stillFails` predicates.
 *
 * The shrinker's end-to-end behavior against a real (deliberately
 * injected, then fully reverted) regression is demonstrated separately,
 * by hand, exactly once -- see ../docs/testing.md's "verifying the
 * shrinker" section for what was run and how it was confirmed clean
 * afterward. That demonstration is throwaway by design and leaves no
 * trace here.
 */
import assert from "node:assert/strict";
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";

import { generateCase } from "../chaos-data.js";
import { Rng } from "../rng.js";
import { shrinkFailingCase, writeShrunkFixture } from "../shrink.js";

const CHAOS_SEED = 20260907;

describe("shrinkFailingCase", () => {
  it("shrinks every subject away when the predicate is tautological", async () => {
    const rng = new Rng(CHAOS_SEED);
    const original = generateCase(rng, 7);
    assert.ok(original.policies.length > 0, "fixture assumption: the generated case has subjects to shrink away");

    const shrunk = await shrinkFailingCase(original, () => true);

    assert.deepEqual(shrunk.policies, [], "a predicate that always fails should shrink down to zero subjects");
    assert.equal(shrunk.electiveMinimum, 0);
  });

  it("never simplifies past the point the predicate stops reproducing", async () => {
    const rng = new Rng(CHAOS_SEED + 1);
    const original = generateCase(rng, 5);

    // Only "fails" while at least 2 subjects remain -- the shrinker must
    // stop exactly there, not keep going to zero.
    const stillFails = (candidate) => candidate.policies.length >= 2;

    const shrunk = await shrinkFailingCase(original, stillFails);

    assert.equal(shrunk.policies.length, 2, "should stop simplifying once the failure would stop reproducing");
  });

  it("rounds numeric context fields toward 0 without changing subject count", async () => {
    const original = {
      policies: [],
      context: { scores: {}, cgpa: 7.3, cgpaFloor: 4.1, attendancePct: 91.2, attendanceFloor: 50.9 },
      electiveMinimum: 0,
    };

    // "Fails" only while cgpa stays above 2 -- forces the shrinker to round
    // cgpa down but not all the way to 0.
    const shrunk = await shrinkFailingCase(original, (c) => c.context.cgpa > 2);

    assert.ok(shrunk.context.cgpa > 2, "must still reproduce the failure");
    assert.ok(shrunk.context.cgpa < original.context.cgpa, "must be simpler than the original");
  });

  it("is idempotent: shrinking an already-minimal case changes nothing", async () => {
    const minimal = { policies: [], context: { scores: {}, cgpa: 0, cgpaFloor: 0, attendancePct: 0, attendanceFloor: 0 }, electiveMinimum: 0 };

    const shrunk = await shrinkFailingCase(minimal, () => true);

    assert.deepEqual(shrunk, minimal);
  });
});

describe("writeShrunkFixture", () => {
  it("writes the shrunk case and any metadata as readable JSON", () => {
    const dir = mkdtempSync(join(tmpdir(), "verdict-rules-shrink-fixture-"));
    const path = join(dir, "shrunk-example.json");
    try {
      const shrunkCase = {
        policies: [],
        context: { scores: {}, cgpa: 0, cgpaFloor: 0, attendancePct: 0, attendanceFloor: 0 },
        electiveMinimum: 0,
      };

      const returned = writeShrunkFixture(path, shrunkCase, { note: "unit test fixture, not a real failure" });

      assert.equal(returned, path);
      assert.ok(existsSync(path));
      const written = JSON.parse(readFileSync(path, "utf8"));
      assert.equal(written.note, "unit test fixture, not a real failure");
      assert.deepEqual(written.policies, shrunkCase.policies);
      assert.deepEqual(written.context, shrunkCase.context);
      assert.equal(written.electiveMinimum, 0);
    } finally {
      rmSync(dir, { recursive: true, force: true });
    }
  });
});
