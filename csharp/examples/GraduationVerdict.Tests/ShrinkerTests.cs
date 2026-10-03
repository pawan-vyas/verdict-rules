using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Proves <see cref="GraduationShrinker"/>'s own simplification algorithm --
/// against a synthetic failure predicate, not the real engine or oracle, so
/// this is a permanent unit test of the mechanism rather than the one-off
/// demonstration described in
/// <c>docs/testing.md</c>'s shrinking section (run by hand against a
/// deliberately injected bug, then reverted -- nothing from that
/// demonstration ships).
/// </summary>
public class ShrinkerTests
{
    [Fact]
    public async Task ShrinksDownToTheSimplestCaseThatStillSatisfiesThePredicate()
    {
        var rng = new Random(20260907);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng, numSubjects: 7);

        // Deliberately has nothing to do with GraduationCheck's real
        // evaluation -- just "at least 2 subjects, and a positive cgpa" --
        // so this exercises the shrinking algorithm itself in isolation.
        Task<bool> StillFails(IReadOnlyList<SubjectPolicy> p, Dictionary<string, object?> ctx, int min) =>
            Task.FromResult(p.Count >= 2 && (double)ctx["cgpa"]! > 0.0);

        var (shrunkPolicies, shrunkContext, shrunkMinimum) =
            await GraduationShrinker.Shrink(policies, context, electiveMinimum, StillFails);

        Assert.True(await StillFails(shrunkPolicies, shrunkContext, shrunkMinimum));
        Assert.Equal(2, shrunkPolicies.Count);
        Assert.Equal(0, shrunkMinimum);
        Assert.All(shrunkPolicies, p =>
        {
            Assert.Equal(0.0, p.WrittenMinPct);
            Assert.False(p.ExemptionAllowed);
            Assert.False(p.IsElective);
        });
        Assert.Equal(0.0, (double)shrunkContext["cgpa_floor"]!);
        Assert.Equal(0.0, (double)shrunkContext["attendance_pct"]!);
        Assert.Equal(0.0, (double)shrunkContext["attendance_floor"]!);
    }

    [Fact]
    public async Task ThrowsIfTheGivenCaseDoesNotActuallyReproduceTheFailure()
    {
        var rng = new Random(1);
        var (policies, context, electiveMinimum) = ChaosData.GenerateCase(rng, numSubjects: 3);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            GraduationShrinker.Shrink(policies, context, electiveMinimum, (_, _, _) => Task.FromResult(false)));
    }
}
