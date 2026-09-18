using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Chaos/differential testing: does the engine ever disagree with an independent oracle?
/// </summary>
/// <remarks>
/// <see cref="SharedFixtureContractTests"/> proves 8 hand-picked,
/// hand-verified scenarios come out right. That doesn't say anything about
/// the enormous space of curricula and students nobody hand-picked. This
/// class checks a much wider space by comparing two independent
/// implementations against each other instead of against a fixed expected
/// value:
/// <list type="bullet">
/// <item><see cref="GraduationCheck.BuildGraduationCheck"/> -- the real,
/// verdict-rules-based implementation this whole project exists to
/// demonstrate.</item>
/// <item><see cref="Oracle.ExpectedGraduates"/> -- a plain,
/// verdict-rules-free re-implementation, deliberately dumb so it's
/// trustworthy by inspection.</item>
/// </list>
/// If they ever disagree, one of them is wrong -- and because every case
/// is generated from a *seeded* <see cref="Random"/> (never a shared or
/// ambient instance), that disagreement is exactly reproducible: the same
/// seed always regenerates the exact same case. "The chaos suite didn't
/// cause any breakdown" is therefore a real, re-checkable claim across
/// runs, not a one-off observation about whatever numbers came up this
/// time.
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
    /// Each case is seeded from <c>ChaosSeed + caseIndex</c> -- not from a
    /// single shared generator advanced across all cases -- specifically
    /// so any one failing case reproduces on its own: re-run
    /// <c>new Random(ChaosSeed + caseIndex)</c> through
    /// <see cref="ChaosData.GenerateCase"/> and you get the exact same
    /// policies/context/electiveMinimum that failed, with no need to
    /// replay every earlier case first.
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
}
