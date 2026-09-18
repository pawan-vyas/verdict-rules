namespace GraduationVerdict;

/// <summary>
/// Small extensions giving <see cref="Random"/> the surface this project's
/// generators reach for.
/// </summary>
public static class RandomExtensions
{
    /// <summary>A double uniformly distributed in [min, max).</summary>
    public static double Uniform(this Random rng, double min, double max) => min + rng.NextDouble() * (max - min);

    /// <summary>One element chosen uniformly from <paramref name="items"/>.</summary>
    public static T Choice<T>(this Random rng, IReadOnlyList<T> items) => items[rng.Next(items.Count)];
}

/// <summary>
/// Deterministic, randomized (policy, student) case generation for the chaos suite.
/// </summary>
/// <remarks>
/// Every method takes a <see cref="Random"/> instance explicitly. Every
/// value generated stays within the schema's valid domain.
/// </remarks>
public static class ChaosData
{
    private static readonly string[] SubjectTypes = ["academic", "vocational", "language"];

    // (PracticalMinPct, ExemptionAllowed) per SubjectType.
    private static readonly Dictionary<string, Func<Random, (double? PracticalMinPct, bool ExemptionAllowed)>> PolicyExtrasBySubjectType = new()
    {
        ["vocational"] = rng => (rng.Uniform(0, 100), false),
        ["language"] = rng => (null, rng.Choice([true, false])),
        ["academic"] = _ => (null, false),
    };

    /// <summary>Build one randomized, but schema-valid, subject policy.</summary>
    /// <param name="rng">The seeded generator to draw from.</param>
    /// <param name="subjectId">Identifier for the generated subject.</param>
    /// <returns>A policy whose type, thresholds, and flags are all randomized independently.</returns>
    public static SubjectPolicy RandomPolicy(Random rng, string subjectId)
    {
        var subjectType = rng.Choice(SubjectTypes);
        var (practicalMinPct, exemptionAllowed) = PolicyExtrasBySubjectType[subjectType](rng);
        return new SubjectPolicy(
            SubjectId: subjectId,
            SubjectType: subjectType,
            WrittenMinPct: rng.Uniform(0, 100),
            PracticalMinPct: practicalMinPct,
            ExemptionAllowed: exemptionAllowed,
            IsElective: rng.Choice([true, false]));
    }

    // Extra score-entry fields per SubjectType.
    private static readonly Dictionary<string, Action<Random, Dictionary<string, object?>>> ContextExtrasBySubjectType = new()
    {
        ["vocational"] = (rng, entry) => entry["practical_pct"] = rng.Uniform(0, 100),
        ["language"] = (rng, entry) => entry["has_exemption"] = rng.Choice([true, false]),
        ["academic"] = (_, _) => { },
    };

    /// <summary>Build one randomized student context matching the given policies.</summary>
    /// <param name="rng">The seeded generator to draw from.</param>
    /// <param name="policies">The curriculum this context's scores must cover.</param>
    /// <returns>
    /// A context in exactly the shape <see cref="GraduationCheck"/>'s rules
    /// and <see cref="Oracle.ExpectedGraduates"/> both expect.
    /// </returns>
    public static Dictionary<string, object?> RandomContext(Random rng, IReadOnlyList<SubjectPolicy> policies)
    {
        var scores = new Dictionary<string, object?>();
        foreach (var policy in policies)
        {
            var entry = new Dictionary<string, object?> { ["written_pct"] = rng.Uniform(0, 100) };
            ContextExtrasBySubjectType[policy.SubjectType](rng, entry);
            scores[policy.SubjectId] = entry;
        }

        return new Dictionary<string, object?>
        {
            ["scores"] = scores,
            ["cgpa"] = rng.Uniform(0, 10),
            ["cgpa_floor"] = rng.Uniform(0, 10),
            ["attendance_pct"] = rng.Uniform(0, 100),
            ["attendance_floor"] = rng.Uniform(0, 100),
        };
    }

    /// <summary>Build one complete, self-consistent randomized case.</summary>
    /// <param name="rng">The seeded generator to draw from.</param>
    /// <param name="numSubjects">How many subjects the generated curriculum has.</param>
    /// <returns>
    /// A tuple ready to hand to both <see cref="GraduationCheck.BuildGraduationCheck"/>
    /// and <see cref="Oracle.ExpectedGraduates"/>. The elective minimum is
    /// bounded by how many electives were actually generated.
    /// </returns>
    public static (IReadOnlyList<SubjectPolicy> Policies, Dictionary<string, object?> Context, int ElectiveMinimum)
        GenerateCase(Random rng, int numSubjects = 7)
    {
        var policies = Enumerable.Range(0, numSubjects).Select(i => RandomPolicy(rng, $"SUBJ{i}")).ToList();
        var context = RandomContext(rng, policies);
        var electiveCount = policies.Count(p => p.IsElective);
        var electiveMinimum = rng.Next(0, electiveCount + 1);
        return (policies, context, electiveMinimum);
    }
}
