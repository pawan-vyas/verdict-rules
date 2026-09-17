/// One subject's grading policy -- read from wherever a real curriculum stores it.
///
/// - [subjectId]: Unique subject code (e.g. "MATH101").
/// - [subjectType]: "academic" | "vocational" | "language" -- decides which
///   `Rule` shape `ruleForSubject` builds.
/// - [writtenMinPct]: Minimum passing percentage on the written score every
///   subject type checks.
/// - [practicalMinPct]: Minimum passing percentage on the practical score --
///   vocational subjects only, `null` otherwise.
/// - [exemptionAllowed]: Whether an approved exemption can substitute for
///   the written score -- language subjects only.
/// - [isElective]: Whether this subject counts toward the elective
///   requirement rather than the core requirement.
class SubjectPolicy {
  final String subjectId;
  final String subjectType;
  final double writtenMinPct;
  final double? practicalMinPct;
  final bool exemptionAllowed;
  final bool isElective;

  const SubjectPolicy({
    required this.subjectId,
    required this.subjectType,
    required this.writtenMinPct,
    this.practicalMinPct,
    this.exemptionAllowed = false,
    this.isElective = false,
  });

  /// Returns a copy with the given fields replaced -- used by the test suite
  /// to build a deliberately-invalid policy for the "unknown subject type"
  /// case without repeating every other field.
  SubjectPolicy copyWith({String? subjectType}) => SubjectPolicy(
        subjectId: subjectId,
        subjectType: subjectType ?? this.subjectType,
        writtenMinPct: writtenMinPct,
        practicalMinPct: practicalMinPct,
        exemptionAllowed: exemptionAllowed,
        isElective: isElective,
      );
}
