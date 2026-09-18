/**
 * A second, independent, verdict-rules-free implementation of the graduation decision.
 *
 * This is deliberately the "naive way" from
 * docs/samples/graduation-requirement-verdict/README.md, generalized to
 * score *any* policy list rather than the fixed 7-subject curriculum -- its
 * entire job is to be obviously correct by inspection, so it can serve as
 * ground truth for test/chaos.test.js's differential testing. See that
 * file's own doc comment and docs/testing/README.md for the full
 * reasoning: two independently written implementations (this plain loop,
 * and the verdict-rules-based engine in graduation-verdict.js) must agree
 * on every input, or one of them is wrong.
 *
 * Never import from "verdict-rules" here -- an oracle that shares a bug
 * with the system it's checking proves nothing.
 */

/** Whether one subject's own scores clear its policy's bar -- plain if/else, no Rule involved. */
function subjectPasses(policy, context) {
  const scores = context.scores[policy.subjectId];

  if (policy.subjectType === "vocational") {
    return scores.writtenPct >= policy.writtenMinPct && scores.practicalPct >= policy.practicalMinPct;
  }

  if (policy.subjectType === "language" && policy.exemptionAllowed) {
    return scores.writtenPct >= policy.writtenMinPct || (scores.hasExemption ?? false);
  }

  // academic, or a language subject with no exemption path
  return scores.writtenPct >= policy.writtenMinPct;
}

/**
 * Compute the graduation decision directly, with no verdict-rules involved at all.
 *
 * @param {object[]} policies - Every subject's own policy (core and elective alike).
 * @param {object} context - One student's scores, cgpa, and attendance --
 *   the same shape `buildGraduationCheck`'s composite expects.
 * @param {number} electiveMinimum - How many electives must pass.
 * @returns {boolean} Whether this student graduates, computed
 *   independently of `ruleForSubject`/`buildGraduationCheck` -- the two
 *   must always agree.
 */
export function expectedGraduates(policies, context, electiveMinimum) {
  const corePolicies = policies.filter((p) => !p.isElective);
  const electivePolicies = policies.filter((p) => p.isElective);

  if (!corePolicies.every((p) => subjectPasses(p, context))) {
    return false;
  }

  const passedElectives = electivePolicies.filter((p) => subjectPasses(p, context)).length;
  if (passedElectives < electiveMinimum) {
    return false;
  }

  if (context.cgpa < context.cgpaFloor) return false;
  if (context.attendancePct < context.attendanceFloor) return false;

  return true;
}
