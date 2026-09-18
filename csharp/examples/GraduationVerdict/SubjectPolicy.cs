namespace GraduationVerdict;

/// <summary>
/// One subject's grading policy -- read from wherever a real curriculum stores it.
/// </summary>
/// <param name="SubjectId">Unique subject code (e.g. "MATH101").</param>
/// <param name="SubjectType">
/// "academic" | "vocational" | "language" -- decides which <see cref="VerdictRules.IRule"/>
/// shape <see cref="GraduationCheck.RuleForSubject"/> builds.
/// </param>
/// <param name="WrittenMinPct">
/// Minimum passing percentage on the written score every subject type checks.
/// </param>
/// <param name="PracticalMinPct">
/// Minimum passing percentage on the practical score -- vocational subjects
/// only, <see langword="null"/> otherwise.
/// </param>
/// <param name="ExemptionAllowed">
/// Whether an approved exemption can substitute for the written score --
/// language subjects only.
/// </param>
/// <param name="IsElective">
/// Whether this subject counts toward the elective requirement rather than
/// the core requirement.
/// </param>
public sealed record SubjectPolicy(
    string SubjectId,
    string SubjectType,
    double WrittenMinPct,
    double? PracticalMinPct = null,
    bool ExemptionAllowed = false,
    bool IsElective = false);
