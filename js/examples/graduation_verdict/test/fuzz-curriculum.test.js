/**
 * Fuzz testing for `curriculumFromObject`/`loadCurriculum` -- the readers
 * that turn a parsed (or raw-text) policies.json into typed subject
 * policies. See ../docs/testing.md for how this complements chaos.test.js
 * and invariants.test.js, which both fuzz *valid* schema-shaped data; this
 * file fuzzes the reader with data that deliberately isn't.
 *
 * Every malformed variant is built from a seeded Rng -- see rng.js -- so a
 * failing case reproduces exactly from its own caseIndex, the same
 * guarantee chaos.test.js makes for the oracle comparison.
 *
 * The only acceptable outcomes feeding malformed input to either reader:
 *   (a) it parses successfully into a well-shaped policy list, or
 *   (b) it raises TypeError (shape problems: wrong type where an object or
 *       array was required, a required field missing) or SyntaxError
 *       (`loadCurriculum` only -- invalid JSON text).
 * Never an unrelated crash (an accidental TypeError from deep inside
 * unguarded property access, with a confusing message) and never a silent,
 * wrong-but-plausible policy list from a value that should have been
 * rejected. See curriculumFromObject's own docstring in
 * ../graduation-verdict.js for exactly what shape it validates and what it
 * deliberately leaves unchecked.
 */
import assert from "node:assert/strict";
import { mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { describe, it } from "node:test";

import { randomPolicy } from "../chaos-data.js";
import { curriculumFromObject, loadCurriculum } from "../graduation-verdict.js";
import { Rng } from "../rng.js";

// Pinned, distinct from chaos.test.js's CHAOS_SEED -- this fuzzer generates
// its own kind of case (malformed shapes), not the schema-valid cases
// chaos.test.js and invariants.test.js share.
const SHAPE_FUZZ_SEED = 20260908;
const NUM_SHAPE_CASES = 150;
const TEXT_FUZZ_SEED = 20260909;
const NUM_TEXT_CASES = 20;

/** The raw snake_case JSON shape policies.json itself uses for one subject. */
function toRawSubjectRow(policy) {
  const row = {
    subject_id: policy.subjectId,
    subject_type: policy.subjectType,
    written_min_pct: policy.writtenMinPct,
  };
  if (policy.practicalMinPct !== undefined) row.practical_min_pct = policy.practicalMinPct;
  if (policy.exemptionAllowed) row.exemption_allowed = policy.exemptionAllowed;
  if (policy.isElective) row.is_elective = policy.isElective;
  return row;
}

/**
 * One well-formed curriculum, built from chaos-data.js's own `randomPolicy`
 * -- reusing the existing generator rather than inventing a second one --
 * converted to the raw snake_case JSON shape a real policies.json uses.
 */
function randomValidCurriculum(rng, numSubjects = 5) {
  const subjects = Array.from({ length: numSubjects }, (_, i) => toRawSubjectRow(randomPolicy(rng, `SUBJ${i}`)));
  return { elective_minimum: rng.randint(0, numSubjects), subjects };
}

const REQUIRED_TOP_LEVEL_FIELDS = ["elective_minimum", "subjects"];
const REQUIRED_ROW_FIELDS = ["subject_id", "subject_type", "written_min_pct"];
const WRONG_TYPE_VALUES = [true, "forty", null, [1, 2, 3], { nested: { deeply: { junk: [1, { more: true }] } } }];
const NON_OBJECT_VALUES = ["not-an-object", 42, true, [1, 2, 3]];

// One corruption strategy per malformed category named in the brief -- a
// lookup table, not an if/else-if ladder, so adding a new malformation is a
// new row here, never a new branch in the fuzz loop below.
const SHAPE_CORRUPTIONS = {
  dropTopLevelKey(curriculum, rng) {
    const copy = structuredClone(curriculum);
    delete copy[rng.choice(REQUIRED_TOP_LEVEL_FIELDS)];
    return copy;
  },
  dropRequiredRowKey(curriculum, rng) {
    const copy = structuredClone(curriculum);
    const row = rng.choice(copy.subjects);
    if (row !== undefined) delete row[rng.choice(REQUIRED_ROW_FIELDS)];
    return copy;
  },
  addUnexpectedKey(curriculum) {
    const copy = structuredClone(curriculum);
    copy.unexpected_top_level_field = "surprise";
    copy.subjects = copy.subjects.map((row) => ({ ...row, unexpected_row_field: 123 }));
    return copy;
  },
  wrongTypeRowField(curriculum, rng) {
    const copy = structuredClone(curriculum);
    const row = rng.choice(copy.subjects);
    if (row !== undefined) row.written_min_pct = rng.choice(WRONG_TYPE_VALUES);
    return copy;
  },
  wrongTypeElectiveMinimum(curriculum, rng) {
    const copy = structuredClone(curriculum);
    copy.elective_minimum = rng.choice(WRONG_TYPE_VALUES);
    return copy;
  },
  nullifyRow(curriculum, rng) {
    const copy = structuredClone(curriculum);
    if (copy.subjects.length > 0) copy.subjects[rng.randint(0, copy.subjects.length - 1)] = null;
    return copy;
  },
  rowNotObject(curriculum, rng) {
    const copy = structuredClone(curriculum);
    if (copy.subjects.length > 0) {
      copy.subjects[rng.randint(0, copy.subjects.length - 1)] = rng.choice(NON_OBJECT_VALUES);
    }
    return copy;
  },
  subjectsNotArray(curriculum, rng) {
    const copy = structuredClone(curriculum);
    copy.subjects = rng.choice(NON_OBJECT_VALUES);
    return copy;
  },
  emptySubjects(curriculum) {
    return { ...structuredClone(curriculum), subjects: [] };
  },
  curriculumIsNull() {
    return null;
  },
  curriculumNotObject(curriculum, rng) {
    return rng.choice(NON_OBJECT_VALUES);
  },
};

/** Feed one malformed curriculum object to the reader and check the outcome is one of the two acceptable shapes. */
function assertAcceptableObjectOutcome(malformed, corruptionName, caseIndex) {
  try {
    const result = curriculumFromObject(malformed);
    assert.ok(
      Array.isArray(result.policies),
      `case ${caseIndex} (${corruptionName}): parsed "successfully" but policies isn't an array`,
    );
    // A "successful" parse must be a genuinely well-shaped policy list, not
    // silent garbage -- every policy this reader hands back must carry the
    // three fields it has no default for.
    for (const policy of result.policies) {
      for (const field of ["subjectId", "subjectType", "writtenMinPct"]) {
        assert.notEqual(
          policy[field],
          undefined,
          `case ${caseIndex} (${corruptionName}): "successfully" parsed a policy missing required field "${field}"`,
        );
      }
    }
  } catch (error) {
    assert.ok(
      error instanceof TypeError,
      `case ${caseIndex} (${corruptionName}): raised ${error.constructor.name} ("${error.message}") -- ` +
        "only TypeError is an acceptable outcome for malformed input shape",
    );
  }
}

describe("fuzzing curriculumFromObject with malformed shapes", () => {
  for (let caseIndex = 0; caseIndex < NUM_SHAPE_CASES; caseIndex++) {
    it(`shape fuzz case ${caseIndex}`, () => {
      const rng = new Rng(SHAPE_FUZZ_SEED + caseIndex);
      const base = randomValidCurriculum(rng);
      const corruptionName = rng.choice(Object.keys(SHAPE_CORRUPTIONS));
      const malformed = SHAPE_CORRUPTIONS[corruptionName](base, rng);

      assertAcceptableObjectOutcome(malformed, corruptionName, caseIndex);
    });
  }
});

// Text-level corruptions, fed through loadCurriculum (which runs JSON.parse
// first) rather than curriculumFromObject -- these exercise the JSON-decode
// failure path a pre-parsed object can never reach.
const TEXT_CORRUPTIONS = {
  truncateEnd(text, rng) {
    return text.slice(0, rng.randint(1, Math.max(1, text.length - 1)));
  },
  truncateStart(text, rng) {
    return text.slice(rng.randint(1, Math.max(1, text.length - 1)));
  },
  dropRandomChar(text, rng) {
    const index = rng.randint(0, text.length - 1);
    return text.slice(0, index) + text.slice(index + 1);
  },
  injectGarbage(text, rng) {
    const index = rng.randint(0, text.length);
    return text.slice(0, index) + "}}}{{{not json" + text.slice(index);
  },
  empty() {
    return "";
  },
};

/** Feed one malformed JSON text file to loadCurriculum and check the outcome is acceptable. */
function assertAcceptableTextOutcome(path, corruptionName, caseIndex) {
  try {
    const result = loadCurriculum(path);
    assert.ok(
      Array.isArray(result.policies),
      `text case ${caseIndex} (${corruptionName}): parsed "successfully" but policies isn't an array`,
    );
  } catch (error) {
    assert.ok(
      error instanceof SyntaxError || error instanceof TypeError,
      `text case ${caseIndex} (${corruptionName}): raised ${error.constructor.name} ("${error.message}") -- ` +
        "only SyntaxError (invalid JSON) or TypeError (valid JSON, wrong shape) is acceptable",
    );
  }
}

describe("fuzzing loadCurriculum with malformed JSON text", () => {
  for (let caseIndex = 0; caseIndex < NUM_TEXT_CASES; caseIndex++) {
    it(`text fuzz case ${caseIndex}`, () => {
      const rng = new Rng(TEXT_FUZZ_SEED + caseIndex);
      const base = randomValidCurriculum(rng);
      const corruptionName = rng.choice(Object.keys(TEXT_CORRUPTIONS));
      const corruptedText = TEXT_CORRUPTIONS[corruptionName](JSON.stringify(base), rng);

      const dir = mkdtempSync(join(tmpdir(), "verdict-rules-fuzz-curriculum-"));
      const path = join(dir, "policies.json");
      try {
        writeFileSync(path, corruptedText, "utf8");
        assertAcceptableTextOutcome(path, corruptionName, caseIndex);
      } finally {
        rmSync(dir, { recursive: true, force: true });
      }
    });
  }
});
