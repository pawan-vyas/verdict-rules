namespace GraduationVerdict;

/// <summary>
/// A second, independent, verdict-rules-free implementation of the graduation decision.
/// </summary>
/// <remarks>
/// Used as ground truth by the chaos suite's differential testing. Never
/// reference <c>VerdictRules</c> here.
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
    /// <param name="context">One student's scores, cgpa, and attendance.</param>
    /// <param name="electiveMinimum">How many electives must pass.</param>
    /// <returns>Whether this student graduates.</returns>
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
