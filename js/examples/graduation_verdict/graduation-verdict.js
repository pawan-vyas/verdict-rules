/**
 * Graduation requirement verdict, implemented with verdict-rules.
 *
 * See docs/samples/graduation-requirement-verdict/README.md for the
 * design and fixtures/graduation_verdict/README.md for the fixture
 * contract.
 */
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { AndRule, FunctionRule, OrRule, RulesEngine } from "verdict-rules";

const __dirname = dirname(fileURLToPath(import.meta.url));
// Fixture data lives at the repo root, shared by every language's own port of
// this example -- see fixtures/graduation_verdict/README.md for the contract.
const FIXTURES = join(__dirname, "..", "..", "..", "fixtures", "graduation_verdict");

/**
 * One subject's grading policy -- read from wherever a real curriculum stores it.
 *
 * @param {object} row
 * @param {string} row.subjectId - Unique subject code (e.g. "MATH101").
 * @param {string} row.subjectType - "academic" | "vocational" | "language" --
 *   decides which Rule shape `ruleForSubject` builds.
 * @param {number} row.writtenMinPct - Minimum passing percentage on the
 *   written score every subject type checks.
 * @param {number | undefined} row.practicalMinPct - Minimum passing
 *   percentage on the practical score -- vocational subjects only,
 *   `undefined` otherwise.
 * @param {boolean} row.exemptionAllowed - Whether an approved exemption can
 *   substitute for the written score -- language subjects only.
 * @param {boolean} row.isElective - Whether this subject counts toward the
 *   elective requirement rather than the core requirement.
 * @returns {object} A frozen subject-policy record with every optional
 *   field defaulted.
 */
export function subjectPolicy({
  subjectId,
  subjectType,
  writtenMinPct,
  practicalMinPct = undefined,
  exemptionAllowed = false,
  isElective = false,
}) {
  return Object.freeze({
    subjectId,
    subjectType,
    writtenMinPct,
    practicalMinPct,
    exemptionAllowed,
    isElective,
  });
}

/**
 * Passes if at least `minimum` of the given sub-rules pass.
 *
 * Not part of verdict-rules itself; see docs/extending/new-rule-shape/.
 * Evaluates every sub-rule unconditionally.
 */
export class AtLeastNRule {
  #rules;
  #minimum;

  constructor(name, rules, minimum, group) {
    this.name = name;
    this.group = group;
    this.#rules = rules;
    this.#minimum = minimum;
  }

  async evaluate(context) {
    const subResults = [];
    for (const rule of this.#rules) {
      subResults.push(await rule.evaluate(context));
    }
    const passedCount = subResults.filter((r) => r.passed).length;
    return {
      ruleName: this.name,
      passed: passedCount >= this.#minimum,
      detail: `${passedCount} of ${this.#rules.length} passed, needed ${this.#minimum}`,
      data: subResults,
    };
  }
}

function writtenPredicate(policy) {
  return async (context) => {
    const pct = context.scores[policy.subjectId].writtenPct;
    return {
      ruleName: policy.subjectId,
      passed: pct >= policy.writtenMinPct,
      detail: `${pct} vs ${policy.writtenMinPct}`,
    };
  };
}

function practicalRule(policy, name) {
  return new FunctionRule(name, async (context) => {
    const pct = context.scores[policy.subjectId].practicalPct;
    return {
      ruleName: name,
      passed: pct >= policy.practicalMinPct,
      detail: `${pct} vs ${policy.practicalMinPct}`,
    };
  });
}

function exemptionRule(policy, name) {
  return new FunctionRule(name, async (context) => {
    const exempt = context.scores[policy.subjectId].hasExemption ?? false;
    return { ruleName: name, passed: exempt };
  });
}

function vocationalSubjectRule(policy, sid, group) {
  return new AndRule(
    sid,
    [
      new FunctionRule(`${sid}:written`, writtenPredicate(policy)),
      practicalRule(policy, `${sid}:practical`),
    ],
    group,
  );
}

function languageSubjectRule(policy, sid, group) {
  if (policy.exemptionAllowed) {
    return new OrRule(
      sid,
      [new FunctionRule(`${sid}:written`, writtenPredicate(policy)), exemptionRule(policy, `${sid}:exemption`)],
      group,
    );
  }
  return new FunctionRule(sid, writtenPredicate(policy), group);
}

function academicSubjectRule(policy, sid, group) {
  return new FunctionRule(sid, writtenPredicate(policy), group);
}

// One builder per subjectType, keyed by the value itself -- adding a fifth
// subject type is a new function plus a new entry here, never a new branch
// in ruleForSubject.
const SUBJECT_RULE_BUILDERS = {
  vocational: vocationalSubjectRule,
  language: languageSubjectRule,
  academic: academicSubjectRule,
};

/**
 * Turn one subject's policy into a Rule -- the shape depends on its type.
 *
 * @param {object} policy - The subject's own policy row.
 * @returns {import("verdict-rules").Rule} An AndRule (written AND
 *   practical) for a vocational subject, an OrRule (written OR exemption)
 *   for a language subject with an exemption path, or a plain FunctionRule
 *   otherwise.
 * @throws {Error} `policy.subjectType` isn't one of the known types.
 */
export function ruleForSubject(policy) {
  const group = policy.isElective ? "elective" : "core";
  const sid = policy.subjectId;

  const builder = SUBJECT_RULE_BUILDERS[policy.subjectType];
  if (builder === undefined) {
    throw new Error(`unknown subjectType ${JSON.stringify(policy.subjectType)} for subject ${JSON.stringify(sid)}`);
  }
  return builder(policy, sid, group);
}

export async function cgpaMet(context) {
  return { ruleName: "cgpa_met", passed: context.cgpa >= context.cgpaFloor };
}

export async function attendanceMet(context) {
  return {
    ruleName: "attendance_met",
    passed: context.attendancePct >= context.attendanceFloor,
  };
}

/**
 * Read the curriculum -- subject policies and the elective-count threshold --
 * from a JSON file.
 *
 * @param {string} path - Path to a JSON object with an `elective_minimum`
 *   integer and a `subjects` array of subject-policy objects.
 * @returns {{ policies: object[], electiveMinimum: number }} One
 *   subjectPolicy per entry, missing optional fields filled with their
 *   defaults, and the minimum number of electives required to graduate.
 */
export function loadCurriculum(path) {
  return curriculumFromObject(JSON.parse(readFileSync(path, "utf8")));
}

/**
 * Convert an already-parsed curriculum object (the shared `{ elective_minimum,
 * subjects }` shape) into `{ policies, electiveMinimum }`.
 *
 * @param {{ elective_minimum: number, subjects: object[] }} curriculum
 * @returns {{ policies: object[], electiveMinimum: number }}
 */
export function curriculumFromObject(curriculum) {
  const policies = curriculum.subjects.map((row) =>
    subjectPolicy({
      subjectId: row.subject_id,
      subjectType: row.subject_type,
      writtenMinPct: row.written_min_pct,
      practicalMinPct: row.practical_min_pct,
      exemptionAllowed: row.exemption_allowed ?? false,
      isElective: row.is_elective ?? false,
    }),
  );
  return { policies, electiveMinimum: curriculum.elective_minimum };
}

/**
 * Read the batch of student records from a JSON file, converting each
 * student's shared snake_case fixture shape into the camelCase context
 * shape this module's rules read.
 *
 * @param {string} path - Path to a JSON object keyed by student id.
 * @returns {Record<string, object>} One context per student, plus the
 *   fixture's own `note` and `expected` fields.
 */
export function loadStudents(path) {
  const raw = JSON.parse(readFileSync(path, "utf8"));
  return Object.fromEntries(
    Object.entries(raw).map(([studentId, row]) => [studentId, studentContext(row)]),
  );
}

/**
 * Convert a bare, shared-fixture-shaped context object (the `scores`/`cgpa`/
 * `attendance_*` fields the rules actually read, with no `note`/`expected`
 * wrapper) into this module's camelCase context shape.
 *
 * @param {object} row
 * @returns {object}
 */
export function contextFromObject(row) {
  const scores = Object.fromEntries(
    Object.entries(row.scores).map(([subjectId, s]) => [
      subjectId,
      {
        writtenPct: s.written_pct,
        ...(s.practical_pct !== undefined ? { practicalPct: s.practical_pct } : {}),
        ...(s.has_exemption !== undefined ? { hasExemption: s.has_exemption } : {}),
      },
    ]),
  );
  return {
    scores,
    cgpa: row.cgpa,
    cgpaFloor: row.cgpa_floor,
    attendancePct: row.attendance_pct,
    attendanceFloor: row.attendance_floor,
  };
}

function studentContext(row) {
  return { ...contextFromObject(row), note: row.note, expected: row.expected };
}

/**
 * Build both structures from one policy list: a diagnostic engine and a fast verdict.
 *
 * @param {object[]} policies - Every subject's own policy.
 * @param {number} electiveMinimum - How many electives must pass.
 * @returns {{ engine: RulesEngine, graduates: AndRule }} A pair built from
 *   the same underlying Rule objects -- `engine` serves
 *   runNamed/runGroup/runAll lookups, `graduates` is the
 *   short-circuiting pass/fail composite.
 */
export function buildGraduationCheck(policies, electiveMinimum) {
  const subjectRules = policies.map((p) => ruleForSubject(p));
  const engine = new RulesEngine(subjectRules);

  const coreRules = subjectRules.filter((r) => r.group === "core");
  const electiveRules = subjectRules.filter((r) => r.group === "elective");

  const graduates = new AndRule("graduates", [
    new AndRule("all_core_subjects_pass", coreRules),
    new AtLeastNRule("elective_requirement", electiveRules, electiveMinimum),
    new FunctionRule("cgpa_met", cgpaMet),
    new FunctionRule("attendance_met", attendanceMet),
  ]);
  return { engine, graduates };
}

async function demo() {
  const { policies, electiveMinimum } = loadCurriculum(join(FIXTURES, "policies.json"));
  const students = loadStudents(join(FIXTURES, "students.json"));
  const { engine, graduates } = buildGraduationCheck(policies, electiveMinimum);

  console.log("=== Graduation Requirement Verdict -- Demo ===");
  console.log(`Built once from policies.json: ${policies.length} subjects\n`);

  const alice = students.alice;
  console.log("--- Detailed lookup for 'alice' ---");
  const named = await engine.runNamed("WORKSHOP201", alice);
  console.log(`runNamed('WORKSHOP201'): ${named.passed ? "PASS" : "FAIL"}`);
  const core = await engine.runGroup("core", alice);
  console.log(
    "runGroup('core'):     " + core.results.map((r) => `${r.ruleName}=${r.passed ? "PASS" : "FAIL"}`).join(", "),
  );
  const elective = await engine.runGroup("elective", alice);
  console.log(
    "runGroup('elective'): " + elective.results.map((r) => `${r.ruleName}=${r.passed ? "PASS" : "FAIL"}`).join(", "),
  );
  const full = await engine.runAll(alice);
  console.log(`runAll(): ${full.results.length} subjects reported\n`);

  console.log("--- Batch verdict across all students (same built engine, no rebuild) ---");
  for (const [studentId, context] of Object.entries(students)) {
    const verdict = await graduates.evaluate(context);
    const status = verdict.passed ? "GRADUATES" : "DOES NOT GRADUATE";
    const reason = verdict.passed ? "" : `  (${verdict.detail})`;
    console.log(`${studentId.padEnd(10)}: ${status}${reason}`);
  }
}

// Runs the demo only when this file is executed directly.
if (import.meta.url === `file://${process.argv[1]}`) {
  await demo();
}
