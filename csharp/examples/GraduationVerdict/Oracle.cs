namespace GraduationVerdict;

/// <summary>
/// A second, independent, verdict-rules-free implementation of the graduation decision.
/// </summary>
/// <remarks>
/// This is deliberately the "naive way" from
/// docs/samples/graduation-requirement-verdict/README.md, generalized to
/// score *any* policy list rather than the fixed 7-subject curriculum --
/// its entire job is to be obviously correct by inspection, so it can
/// serve as ground truth for the chaos suite's differential testing. See
/// that suite's own doc comment and docs/testing/README.md for the full
/// reasoning: two independently written implementations (this plain
/// loop, and the verdict-rules-based engine in <see cref="GraduationCheck"/>)
/// must agree on every input, or one of them is wrong.
///
/// Never reference <c>VerdictRules</c> here -- an oracle that shares a
/// bug with the system it's checking proves nothing.
/// </remarks>
public static class Oracle
{
    /// <summary>Whether one subject's own scores clear its policy's bar -- plain if/else, no rule involved.</summary>
    private static bool SubjectPasses(SubjectPolicy policy, IReadOnlyDictionary<string, object?> context)
    {
        var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
        var subject = (IReadOnlyDictionary<string, object?>)scores[policy.SubjectId]!;
        var writtenPct = (double)subject["written_pct"]!;

        if (policy.SubjectType == "vocational")
        {
            var practicalPct = (double)subject["practical_pct"]!;
            return writtenPct >= policy.WrittenMinPct && practicalPct >= policy.PracticalMinPct;
        }

        if (policy.SubjectType == "language" && policy.ExemptionAllowed)
        {
            var hasExemption = subject.TryGetValue("has_exemption", out var v) && v is true;
            return writtenPct >= policy.WrittenMinPct || hasExemption;
        }

        // academic, or a language subject with no exemption path
        return writtenPct >= policy.WrittenMinPct;
    }

    /// <summary>
    /// Compute the graduation decision directly, with no verdict-rules involved at all.
    /// </summary>
    /// <param name="policies">Every subject's own policy (core and elective alike).</param>
    /// <param name="context">
    /// One student's scores, cgpa, and attendance -- the same shape
    /// <see cref="GraduationCheck.BuildGraduationCheck"/>'s composite expects.
    /// </param>
    /// <param name="electiveMinimum">How many electives must pass.</param>
    /// <returns>
    /// Whether this student graduates, computed independently of
    /// <see cref="GraduationCheck.RuleForSubject"/>/
    /// <see cref="GraduationCheck.BuildGraduationCheck"/> -- the two must
    /// always agree.
    /// </returns>
    public static bool ExpectedGraduates(
        IReadOnlyList<SubjectPolicy> policies, IReadOnlyDictionary<string, object?> context, int electiveMinimum)
    {
        var corePolicies = policies.Where(p => !p.IsElective).ToList();
        var electivePolicies = policies.Where(p => p.IsElective).ToList();

        if (!corePolicies.All(p => SubjectPasses(p, context)))
        {
            return false;
        }

        var passedElectives = electivePolicies.Count(p => SubjectPasses(p, context));
        if (passedElectives < electiveMinimum)
        {
            return false;
        }

        var cgpa = (double)context["cgpa"]!;
        var cgpaFloor = (double)context["cgpa_floor"]!;
        if (cgpa < cgpaFloor) return false;

        var attendancePct = (double)context["attendance_pct"]!;
        var attendanceFloor = (double)context["attendance_floor"]!;
        if (attendancePct < attendanceFloor) return false;

        return true;
    }
}
