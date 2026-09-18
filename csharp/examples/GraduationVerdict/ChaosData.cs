namespace GraduationVerdict;

/// <summary>
/// Small extensions giving <see cref="Random"/> the same surface Python's
/// <c>random.Random</c> and this project's own generators reach for --
/// unlike JavaScript's <c>Math.random()</c>, .NET's <see cref="Random"/> is
/// already seedable and deterministic for a given seed within one .NET
/// version, so no custom generator is needed here.
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
/// "Deterministic" is the load-bearing word: every method here takes a
/// <see cref="Random"/> instance explicitly -- never a shared or ambient
/// generator -- so a case built from a given seed is exactly reproducible.
/// The chaos suite seeds one <see cref="Random"/> per case from a pinned
/// constant plus that case's own index, so any single failing case can be
/// regenerated on its own without replaying every case before it.
///
/// Every value generated stays within the schema's valid domain (a real
/// percentage, a real SubjectType, etc.) -- this generates a wide space of
/// *valid* curricula and students, not malformed input. Garbage-input
/// handling is a different, narrower concern this suite doesn't cover.
/// </remarks>
public static class ChaosData
{
    private static readonly string[] SubjectTypes = ["academic", "vocational", "language"];

    // (PracticalMinPct, ExemptionAllowed) per SubjectType -- the two policy
    // fields whose valid range depends on which type generated them. A new
    // subject type is a new entry here.
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

    /// <summary>Build one randomized student context matching the given policies.</summary>
    /// <param name="rng">The seeded generator to draw from.</param>
    /// <param name="policies">
    /// The curriculum this context's scores must cover -- one entry
    /// generated per policy, shaped to that policy's own type.
    /// </param>
    /// <returns>
    /// A context in exactly the shape <see cref="GraduationCheck"/>'s rules
    /// and <see cref="Oracle.ExpectedGraduates"/> both expect.
    /// </returns>
    // Extra score-entry fields per SubjectType, applied onto the shared
    // written_pct base below.
    private static readonly Dictionary<string, Action<Random, Dictionary<string, object?>>> ContextExtrasBySubjectType = new()
    {
        ["vocational"] = (rng, entry) => entry["practical_pct"] = rng.Uniform(0, 100),
        ["language"] = (rng, entry) => entry["has_exemption"] = rng.Choice([true, false]),
        ["academic"] = (_, _) => { },
    };

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
    /// <param name="rng">
    /// The seeded generator to draw from -- the sole source of randomness,
    /// so the same <paramref name="rng"/> state always produces the same case.
    /// </param>
    /// <param name="numSubjects">How many subjects the generated curriculum has.</param>
    /// <returns>
    /// A tuple ready to hand to both <see cref="GraduationCheck.BuildGraduationCheck"/>
    /// and <see cref="Oracle.ExpectedGraduates"/>. The elective minimum is
    /// always achievable (bounded by how many electives were actually
    /// generated), so a mismatch between the two implementations is never
    /// explained away as "an impossible curriculum."
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
