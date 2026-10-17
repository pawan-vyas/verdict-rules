using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Chaos/differential testing: compares
/// <see cref="GraduationCheck.BuildGraduationCheck"/> against
/// <see cref="Oracle.ExpectedGraduates"/> across a wide, randomly-generated
/// space of curricula and students.
/// </summary>
/// <remarks>
/// Every case is generated from a seeded <see cref="Random"/>, never a
/// shared or ambient instance, so a disagreement is exactly reproducible
/// from its seed.
/// </remarks>
public class ChaosTests
{
    // Pinned. Do not change without a deliberate reason -- bumping this
    // reshuffles every case's data, silently trading which edge cases get
    // covered for which. If you need *more* coverage, raise NumCases
    // instead; that's strictly additive; changing ChaosSeed is not.
    private const int ChaosSeed = 20260907;
    private const int NumCases = 500;

    public static IEnumerable<object[]> CaseIndexes() =>
        Enumerable.Range(0, NumCases).Select(i => new object[] { i });

    /// <summary>
    /// For a deterministically-generated, schema-valid random case, the
    /// real engine's verdict and the independent oracle's verdict must
    /// always match.
    /// </summary>
    /// <remarks>
    /// Each case is seeded from <c>ChaosSeed + caseIndex</c>, not a single
    /// shared generator advanced across all cases, so a failing case
    /// reproduces on its own via <c>new Random(ChaosSeed + caseIndex)</c>
    /// through <see cref="ChaosData.GenerateCase"/>.
    /// </remarks>
    [Theory]
    [MemberData(nameof(CaseIndexes))]
    public async Task EngineAgreesWithIndependentOracle(int caseIndex)
    {
        var rng = new Random(ChaosSeed + caseIndex);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng);

        var (_, graduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var actual = (await graduates.EvaluateAsync(context)).Passed;
        var expected = Oracle.ExpectedGraduates(policies, context, electiveMinimum);

        Assert.True(actual == expected,
            $"caseIndex={caseIndex} (seed={ChaosSeed + caseIndex}): engine said {actual}, oracle said {expected}\n" +
            $"electiveMinimum={electiveMinimum}");
    }

    /// <summary>
    /// For every generated case, the short-circuit/no-short-circuit/leaf shape
    /// each concrete rule type promises holds all the way down the tree --
    /// not just at <c>graduates</c>' own four immediate children, but into the
    /// nested <see cref="AndRule"/>/<see cref="OrRule"/> a vocational/language
    /// subject's own policy produces.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseIndexes))]
    public async Task StructuralInvariantsHoldForEveryGeneratedCase(int caseIndex)
    {
        var rng = new Random(ChaosSeed + caseIndex);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng);

        var (_, graduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var result = await graduates.EvaluateAsync(context);

        var mirror = StructuralInvariants.BuildGraduatesMirror(policies, electiveMinimum);
        StructuralInvariants.AssertNode(mirror, result);
    }

    /// <summary>
    /// <c>RunAllAsync</c> reports exactly one result per registered rule --
    /// a structural count, not a verdict -- for every generated case.
    /// </summary>
    [Theory]
    [MemberData(nameof(CaseIndexes))]
    public async Task RunAllReturnsExactlyOneResultPerRegisteredRule(int caseIndex)
    {
        var rng = new Random(ChaosSeed + caseIndex);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng);

        var (engine, _) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var runAll = await engine.RunAllAsync(context);

        Assert.Equal(policies.Count, runAll.Results.Count);
    }

    // A handful of the 500 generated cases, spread across the range rather
    // than clustered at the start -- purity doesn't need all 500 re-checked,
    // just more than one.
    public static IEnumerable<object[]> PurityCaseIndexes() =>
        Enumerable.Range(0, NumCases).Where(i => i % 47 == 0).Select(i => new object[] { i });

    /// <summary>
    /// Evaluating the exact same (policies, context, electiveMinimum) twice
    /// produces two result trees identical in every field, recursively --
    /// not just an equal top-level <see cref="RuleResult.Passed"/>.
    /// </summary>
    [Theory]
    [MemberData(nameof(PurityCaseIndexes))]
    public async Task EvaluatingTheSameCaseTwiceProducesIdenticalResultTrees(int caseIndex)
    {
        var rng = new Random(ChaosSeed + caseIndex);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng);

        var (_, firstGraduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var first = await firstGraduates.EvaluateAsync(context);

        var (_, secondGraduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var second = await secondGraduates.EvaluateAsync(context);

        StructuralInvariants.AssertResultTreesEqual(first, second);
    }
}
