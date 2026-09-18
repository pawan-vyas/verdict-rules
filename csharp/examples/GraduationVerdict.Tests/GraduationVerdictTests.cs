using System.Text.Json;
using VerdictRules;
using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Tests for the graduation-verdict example.
/// </summary>
/// <remarks>
/// These aren't just tests of this example's own logic -- because this
/// project exercises FunctionRule, AndRule, OrRule, a custom Rule shape,
/// and all three RulesEngine run modes together, this suite functions as
/// an integration/e2e regression net for verdict-rules itself. See
/// docs/testing/README.md's "second testing layer" section for the full
/// reasoning behind that claim.
/// </remarks>
public static class SharedFixture
{
    public static readonly (IReadOnlyList<SubjectPolicy> Policies, int ElectiveMinimum) Curriculum =
        GraduationCheck.LoadCurriculum(Fixtures.PathTo("policies.json"));

    public static readonly Dictionary<string, StudentRecord> Students =
        GraduationCheck.LoadStudents(Fixtures.PathTo("students.json"));

    public static readonly JsonElement EdgeCases =
        JsonDocument.Parse(File.ReadAllText(Fixtures.PathTo("edge_cases.json"))).RootElement;

    public static SubjectPolicy Policy(string subjectId) =>
        Curriculum.Policies.First(p => p.SubjectId == subjectId);
}

/// <summary>Each subject_type must produce the Rule shape the sample spec claims. See docs/samples/graduation-requirement-verdict/README.md.</summary>
public class RuleShapeDispatchTests
{
    [Fact]
    public void AnAcademicSubjectIsAPlainFunctionRule()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("MATH101"));
        Assert.IsType<FunctionRule>(rule);
    }

    [Fact]
    public void AVocationalSubjectIsAnAndRuleOfTwo()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("WORKSHOP201"));
        Assert.IsType<AndRule>(rule);
    }

    [Fact]
    public void ALanguageSubjectWithExemptionIsAnOrRule()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("FRENCH101"));
        Assert.IsType<OrRule>(rule);
    }

    [Fact]
    public void CoreVsElectiveGroupTagging()
    {
        Assert.Equal("core", GraduationCheck.RuleForSubject(SharedFixture.Policy("MATH101")).Group);
        Assert.Equal("elective", GraduationCheck.RuleForSubject(SharedFixture.Policy("ELECTIVE_ART")).Group);
    }

    [Fact]
    public void UnknownSubjectTypeThrows()
    {
        var badPolicy = SharedFixture.Policy("MATH101") with { SubjectType = "portfolio" };
        Assert.Throws<ArgumentException>(() => GraduationCheck.RuleForSubject(badPolicy));
    }
}

/// <summary>The AndRule built for a vocational subject needs both scores to pass.</summary>
public class VocationalAndRuleTests
{
    [Fact]
    public async Task FailsIfOnlyWrittenPasses()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("WORKSHOP201"));
        var context = new Dictionary<string, object?>
        {
            ["scores"] = new Dictionary<string, object?>
            {
                ["WORKSHOP201"] = new Dictionary<string, object?> { ["written_pct"] = 50.0, ["practical_pct"] = 45.0 },
            },
        };
        var result = await rule.EvaluateAsync(context);
        Assert.False(result.Passed);
    }

    [Fact]
    public async Task PassesIfBothScoresClearTheirOwnBar()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("WORKSHOP201"));
        var context = new Dictionary<string, object?>
        {
            ["scores"] = new Dictionary<string, object?>
            {
                ["WORKSHOP201"] = new Dictionary<string, object?> { ["written_pct"] = 50.0, ["practical_pct"] = 70.0 },
            },
        };
        var result = await rule.EvaluateAsync(context);
        Assert.True(result.Passed);
    }
}

/// <summary>The OrRule built for a language subject accepts either the paper or an exemption.</summary>
public class LanguageOrRuleTests
{
    [Fact]
    public async Task PassesViaExemptionWhenThePaperFails()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("FRENCH101"));
        var context = new Dictionary<string, object?>
        {
            ["scores"] = new Dictionary<string, object?>
            {
                ["FRENCH101"] = new Dictionary<string, object?> { ["written_pct"] = 20.0, ["has_exemption"] = true },
            },
        };
        var result = await rule.EvaluateAsync(context);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task FailsWhenNeitherThePaperNorAnExemptionApplies()
    {
        var rule = GraduationCheck.RuleForSubject(SharedFixture.Policy("FRENCH101"));
        var context = new Dictionary<string, object?>
        {
            ["scores"] = new Dictionary<string, object?>
            {
                ["FRENCH101"] = new Dictionary<string, object?> { ["written_pct"] = 20.0, ["has_exemption"] = false },
            },
        };
        var result = await rule.EvaluateAsync(context);
        Assert.False(result.Passed);
    }
}

/// <summary>RunNamedAsync/RunGroupAsync/RunAllAsync each serve the specific job the sample spec claims. See docs/samples/graduation-requirement-verdict/README.md.</summary>
public class EngineRunModesTests
{
    [Fact]
    public async Task RunNamedLooksUpOneSubject()
    {
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var result = await engine.RunNamedAsync("WORKSHOP201", SharedFixture.Students["alice"].Context);
        Assert.Equal("WORKSHOP201", result.RuleName);
        Assert.True(result.Passed);
    }

    [Fact]
    public async Task RunGroupCoreReportsEveryCoreSubject()
    {
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var result = await engine.RunGroupAsync("core", SharedFixture.Students["alice"].Context);
        Assert.Equal(
            new HashSet<string> { "MATH101", "ENG101", "WORKSHOP201", "FRENCH101" },
            result.Results.Select(r => r.RuleName).ToHashSet());
    }

    [Fact]
    public async Task RunGroupElectiveReportsEveryElectiveSubject()
    {
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var result = await engine.RunGroupAsync("elective", SharedFixture.Students["alice"].Context);
        Assert.Equal(
            new HashSet<string> { "ELECTIVE_ART", "ELECTIVE_MUSIC", "ELECTIVE_CS" },
            result.Results.Select(r => r.RuleName).ToHashSet());
    }

    [Fact]
    public async Task RunAllReportsEverySubject()
    {
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var result = await engine.RunAllAsync(SharedFixture.Students["alice"].Context);
        Assert.Equal(SharedFixture.Curriculum.Policies.Count, result.Results.Count);
    }

    [Fact]
    public async Task RunGroupNeverShortCircuitsUnlikeTheGraduatesComposite()
    {
        // bob fails ENG101 (core) -- RunGroupAsync must still report every other core subject.
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var result = await engine.RunGroupAsync("core", SharedFixture.Students["bob"].Context);
        Assert.Equal(4, result.Results.Count);
        Assert.False(result.Passed);
    }
}

/// <summary>
/// Every expectation in the shared fixture, asserted. This is the
/// cross-language contract: each port of this example must reproduce
/// these exact numbers. See fixtures/graduation_verdict/README.md for
/// what each field proves and why the counts matter more than the
/// booleans.
/// </summary>
public class SharedFixtureContractTests
{
    public static IEnumerable<object[]> StudentIds() => SharedFixture.Students.Keys.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(StudentIds))]
    public async Task VerdictMatches(string studentId)
    {
        var (_, graduates) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var record = SharedFixture.Students[studentId];
        var expectedPassed = record.Expected.GetProperty("passed").GetBoolean();
        var result = await graduates.EvaluateAsync(record.Context);
        Assert.True(result.Passed == expectedPassed,
            $"{studentId}: expected passed={expectedPassed}, got {result.Passed} ({result.Detail})");
    }

    [Theory]
    [MemberData(nameof(StudentIds))]
    public async Task ShortCircuitCountMatches(string studentId)
    {
        // bob and gita both fail, but bob stops after one rule and gita runs
        // all four. An implementation that evaluated sub-rules concurrently
        // would return both booleans correctly and fail here.
        var (_, graduates) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var record = SharedFixture.Students[studentId];
        var expectedCount = record.Expected.GetProperty("rules_evaluated").GetInt32();
        var result = await graduates.EvaluateAsync(record.Context);
        var actualCount = ((IReadOnlyList<RuleResult>)result.Data!).Count;
        Assert.True(actualCount == expectedCount,
            $"{studentId}: expected {expectedCount} sub-rules to run, got {actualCount}");
    }

    [Theory]
    [MemberData(nameof(StudentIds))]
    public async Task FailingChainMatches(string studentId)
    {
        // Proves nesting survives: deepak's failure is three levels deep.
        var (_, graduates) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var record = SharedFixture.Students[studentId];
        var expectedChain = record.Expected.GetProperty("failing_chain").EnumerateArray().Select(e => e.GetString()!).ToList();
        var expectedFailingRule = record.Expected.GetProperty("failing_rule") is { ValueKind: JsonValueKind.String } fr ? fr.GetString() : null;
        var result = await graduates.EvaluateAsync(record.Context);
        var chain = Fixtures.FailingChain(result);
        Assert.Equal(expectedChain, chain);
        Assert.Equal(expectedFailingRule, chain.Count > 0 ? chain[0] : null);
    }

    [Theory]
    [MemberData(nameof(StudentIds))]
    public async Task RunAllNeverShortCircuits(string studentId)
    {
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var record = SharedFixture.Students[studentId];
        var expected = record.Expected.GetProperty("run_all");
        var result = await engine.RunAllAsync(record.Context);
        Assert.Equal(expected.GetProperty("evaluated").GetInt32(), result.Results.Count);
        Assert.Equal(expected.GetProperty("passed").GetBoolean(), result.Passed);
    }

    [Theory]
    [MemberData(nameof(StudentIds))]
    public async Task GroupResultsMatch(string studentId)
    {
        // harish is the interesting one: he graduates while his elective
        // group "fails", because the group verdict is all-must-pass and the
        // composite's requirement is at-least-two-of-three.
        var (engine, _) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var record = SharedFixture.Students[studentId];
        foreach (var group in record.Expected.GetProperty("groups").EnumerateObject())
        {
            var result = await engine.RunGroupAsync(group.Name, record.Context);
            Assert.True(group.Value.GetProperty("evaluated").GetInt32() == result.Results.Count, $"{studentId}/{group.Name}");
            Assert.True(group.Value.GetProperty("passed").GetBoolean() == result.Passed, $"{studentId}/{group.Name}");
        }
    }
}

/// <summary>
/// The degenerate curricula, from the shared fixture's edge_cases.json.
/// AndRule([]) passing while an at-least-N rule over an empty set fails
/// for N > 0 is deliberately asymmetric, and it is the kind of thing a
/// port gets backwards without noticing -- nothing in the main student
/// set exercises an empty rule list at all.
/// </summary>
public class VacuousTruthEdgeCasesTests
{
    public static IEnumerable<object[]> CaseNames() =>
        SharedFixture.EdgeCases.EnumerateObject().Select(p => new object[] { p.Name });

    [Theory]
    [MemberData(nameof(CaseNames))]
    public async Task EdgeCaseMatchesFixture(string caseName)
    {
        var testCase = SharedFixture.EdgeCases.GetProperty(caseName);
        var (policies, electiveMinimum) = GraduationCheck.CurriculumFromJson(testCase.GetProperty("curriculum"));
        var expected = testCase.GetProperty("expected");
        var student = GraduationCheck.ContextFromJson(testCase.GetProperty("student"));

        var (engine, graduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);
        var result = await graduates.EvaluateAsync(student);

        Assert.True(result.Passed == expected.GetProperty("passed").GetBoolean(), caseName);
        Assert.Equal(expected.GetProperty("rules_evaluated").GetInt32(), ((IReadOnlyList<RuleResult>)result.Data!).Count);
        var chain = Fixtures.FailingChain(result);
        var expectedChain = expected.GetProperty("failing_chain").EnumerateArray().Select(e => e.GetString()!).ToList();
        Assert.Equal(expectedChain, chain);

        var runAll = await engine.RunAllAsync(student);
        Assert.Equal(expected.GetProperty("run_all").GetProperty("evaluated").GetInt32(), runAll.Results.Count);
        Assert.Equal(expected.GetProperty("run_all").GetProperty("passed").GetBoolean(), runAll.Passed);

        // Absence is reported two ways, and both are part of the contract:
        // the strict form throws, the try-prefixed form returns null. A port
        // that shipped one without the other would fail here.
        var lookups = expected.GetProperty("lookups");

        var group = lookups.GetProperty("unknown_group").GetProperty("name").GetString()!;
        if (lookups.GetProperty("unknown_group").GetProperty("run_group_raises").GetBoolean())
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunGroupAsync(group, student));
        }
        if (lookups.GetProperty("unknown_group").GetProperty("try_run_group_returns_null").GetBoolean())
        {
            Assert.Null(await engine.TryRunGroupAsync(group, student));
        }

        var rule = lookups.GetProperty("unknown_rule").GetProperty("name").GetString()!;
        if (lookups.GetProperty("unknown_rule").GetProperty("run_named_raises").GetBoolean())
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => engine.RunNamedAsync(rule, student));
        }
        if (lookups.GetProperty("unknown_rule").GetProperty("try_run_named_returns_null").GetBoolean())
        {
            Assert.Null(await engine.TryRunNamedAsync(rule, student));
        }
    }
}

/// <summary>The "scale" claim: one built engine/composite pair, reused across every student, never rebuilt per lookup.</summary>
public class BuildOnceApplyManyTimesTests
{
    [Fact]
    public async Task SameBuiltObjectsServeEveryStudent()
    {
        var (_, graduates) = GraduationCheck.BuildGraduationCheck(SharedFixture.Curriculum.Policies, SharedFixture.Curriculum.ElectiveMinimum);
        var results = new Dictionary<string, bool>();
        foreach (var (studentId, record) in SharedFixture.Students)
        {
            results[studentId] = (await graduates.EvaluateAsync(record.Context)).Passed;
        }
        var expected = SharedFixture.Students.ToDictionary(kv => kv.Key, kv => kv.Value.Expected.GetProperty("passed").GetBoolean());
        Assert.Equal(expected, results);
    }
}
