using System.Text.Json;
using VerdictRules;

namespace GraduationVerdict;

/// <summary>
/// Graduation requirement verdict -- the flagship verdict-rules example, as real code.
/// </summary>
/// <remarks>
/// See docs/samples/graduation-requirement-verdict/README.md for the full
/// design -- the naive-way contrast, both diagrams, and the reasoning
/// behind every choice below. See fixtures/graduation_verdict/README.md
/// for how to extend this project, and docs/testing/README.md for why its
/// own test suite doubles as a regression net for verdict-rules itself.
/// </remarks>
public static class GraduationCheck
{
    /// <summary>
    /// Turn one subject's policy into a rule -- the shape depends on its type.
    /// </summary>
    /// <param name="policy">The subject's own policy row.</param>
    /// <returns>
    /// An <see cref="AndRule"/> (written AND practical) for a vocational
    /// subject, an <see cref="OrRule"/> (written OR exemption) for a
    /// language subject with an exemption path, or a plain
    /// <see cref="FunctionRule"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="policy"/>'s <see cref="SubjectPolicy.SubjectType"/>
    /// isn't one of the known types -- deliberately loud rather than
    /// silently building a vacuously-passing rule for an unrecognized
    /// policy.
    /// </exception>
    public static IRule RuleForSubject(SubjectPolicy policy)
    {
        var group = policy.IsElective ? "elective" : "core";
        var sid = policy.SubjectId;

        if (policy.SubjectType == "vocational")
        {
            return new AndRule(
                sid,
                [WrittenRule(policy, $"{sid}:written"), PracticalRule(policy, $"{sid}:practical")],
                group);
        }

        if (policy.SubjectType == "language")
        {
            if (policy.ExemptionAllowed)
            {
                return new OrRule(
                    sid,
                    [WrittenRule(policy, $"{sid}:written"), ExemptionRule(policy, $"{sid}:exemption")],
                    group);
            }
            return new FunctionRule(sid, WrittenPredicate(policy, sid), group);
        }

        if (policy.SubjectType == "academic")
        {
            return new FunctionRule(sid, WrittenPredicate(policy, sid), group);
        }

        throw new ArgumentException($"unknown SubjectType '{policy.SubjectType}' for subject '{sid}'", nameof(policy));
    }

    private static RulePredicate WrittenPredicate(SubjectPolicy policy, string name) =>
        (context, _) =>
        {
            var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
            var subject = (IReadOnlyDictionary<string, object?>)scores[policy.SubjectId]!;
            var pct = (double)subject["written_pct"]!;
            return Task.FromResult(new RuleResult(name, pct >= policy.WrittenMinPct, $"{pct} vs {policy.WrittenMinPct}"));
        };

    private static FunctionRule WrittenRule(SubjectPolicy policy, string name) =>
        new(name, WrittenPredicate(policy, name));

    private static FunctionRule PracticalRule(SubjectPolicy policy, string name) =>
        new(name, (context, _) =>
        {
            var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
            var subject = (IReadOnlyDictionary<string, object?>)scores[policy.SubjectId]!;
            var pct = (double)subject["practical_pct"]!;
            return Task.FromResult(new RuleResult(name, pct >= policy.PracticalMinPct, $"{pct} vs {policy.PracticalMinPct}"));
        });

    private static FunctionRule ExemptionRule(SubjectPolicy policy, string name) =>
        new(name, (context, _) =>
        {
            var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
            var subject = (IReadOnlyDictionary<string, object?>)scores[policy.SubjectId]!;
            var exempt = subject.TryGetValue("has_exemption", out var v) && v is true;
            return Task.FromResult(new RuleResult(name, exempt));
        });

    public static Task<RuleResult> CgpaMet(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult("cgpa_met", (double)context["cgpa"]! >= (double)context["cgpa_floor"]!));

    public static Task<RuleResult> AttendanceMet(IReadOnlyDictionary<string, object?> context, CancellationToken ct = default) =>
        Task.FromResult(new RuleResult(
            "attendance_met",
            (double)context["attendance_pct"]! >= (double)context["attendance_floor"]!));

    /// <summary>
    /// Read the curriculum -- subject policies and the elective-count
    /// threshold -- from a JSON file.
    /// </summary>
    /// <param name="path">
    /// Path to a JSON object with an <c>elective_minimum</c> integer and a
    /// <c>subjects</c> array of subject-policy objects.
    /// </param>
    /// <returns>
    /// One <see cref="SubjectPolicy"/> per entry (missing optional fields
    /// filled with their record defaults, the same way a real database
    /// row's NULL columns would be handled), and the minimum number of
    /// electives required to graduate. Both come from data -- neither is a
    /// C# literal anywhere in this project.
    /// </returns>
    public static (IReadOnlyList<SubjectPolicy> Policies, int ElectiveMinimum) LoadCurriculum(string path) =>
        CurriculumFromJson(JsonDocument.Parse(File.ReadAllText(path)).RootElement);

    /// <summary>
    /// Convert an already-parsed curriculum element (the shared
    /// <c>{ elective_minimum, subjects }</c> shape) into policies plus a
    /// threshold -- the same row-conversion <see cref="LoadCurriculum"/>
    /// applies to a file, exposed separately so the edge-case suite can
    /// apply it to edge_cases.json's own inline curricula without
    /// re-parsing anything from disk.
    /// </summary>
    public static (IReadOnlyList<SubjectPolicy> Policies, int ElectiveMinimum) CurriculumFromJson(JsonElement curriculum)
    {
        var policies = curriculum.GetProperty("subjects").EnumerateArray().Select(row => new SubjectPolicy(
            SubjectId: row.GetProperty("subject_id").GetString()!,
            SubjectType: row.GetProperty("subject_type").GetString()!,
            WrittenMinPct: row.GetProperty("written_min_pct").GetDouble(),
            PracticalMinPct: row.TryGetProperty("practical_min_pct", out var p) ? p.GetDouble() : null,
            ExemptionAllowed: row.TryGetProperty("exemption_allowed", out var e) && e.GetBoolean(),
            IsElective: row.TryGetProperty("is_elective", out var i) && i.GetBoolean()
        )).ToList();
        return (policies, curriculum.GetProperty("elective_minimum").GetInt32());
    }

    /// <summary>
    /// Read the batch of student records from a JSON file.
    /// </summary>
    /// <param name="path">Path to a JSON object keyed by student id.</param>
    /// <returns>
    /// One context per student -- each value's own shape already *is* the
    /// context dict verdict-rules expects (plus a <c>note</c> and
    /// <c>expected</c> field the rules themselves never read, used only by
    /// the demo and the test suite, carried alongside as raw
    /// <see cref="JsonElement"/>s for the test suite's own use).
    /// </returns>
    public static Dictionary<string, StudentRecord> LoadStudents(string path)
    {
        var raw = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var result = new Dictionary<string, StudentRecord>();
        foreach (var prop in raw.EnumerateObject())
        {
            result[prop.Name] = new StudentRecord(ContextFromJson(prop.Value), prop.Value.GetProperty("expected"));
        }
        return result;
    }

    /// <summary>
    /// Convert a bare, shared-fixture-shaped context element (the
    /// <c>scores</c>/<c>cgpa</c>/<c>attendance_*</c> fields the rules
    /// actually read, with no <c>note</c>/<c>expected</c> wrapper) into
    /// this project's context dictionary shape. Exposed separately from
    /// <see cref="LoadStudents"/> so the edge-case suite can apply it
    /// directly to edge_cases.json's own inline <c>student</c> objects.
    /// </summary>
    public static Dictionary<string, object?> ContextFromJson(JsonElement row)
    {
        var scores = new Dictionary<string, object?>();
        foreach (var subject in row.GetProperty("scores").EnumerateObject())
        {
            var entry = new Dictionary<string, object?> { ["written_pct"] = subject.Value.GetProperty("written_pct").GetDouble() };
            if (subject.Value.TryGetProperty("practical_pct", out var pp))
            {
                entry["practical_pct"] = pp.GetDouble();
            }
            if (subject.Value.TryGetProperty("has_exemption", out var he))
            {
                entry["has_exemption"] = he.GetBoolean();
            }
            scores[subject.Name] = entry;
        }

        return new Dictionary<string, object?>
        {
            ["scores"] = scores,
            ["cgpa"] = row.GetProperty("cgpa").GetDouble(),
            ["cgpa_floor"] = row.GetProperty("cgpa_floor").GetDouble(),
            ["attendance_pct"] = row.GetProperty("attendance_pct").GetDouble(),
            ["attendance_floor"] = row.GetProperty("attendance_floor").GetDouble(),
        };
    }

    /// <summary>
    /// Build both structures from one policy list: a diagnostic engine and a fast verdict.
    /// </summary>
    /// <param name="policies">Every subject's own policy.</param>
    /// <param name="electiveMinimum">
    /// How many electives must pass -- read from policies.json's own
    /// <c>elective_minimum</c> field, never hardcoded here, so a
    /// curriculum change to this number is a data edit like every other
    /// threshold in this project.
    /// </param>
    /// <returns>
    /// A pair built from the *same* underlying rule objects -- the engine
    /// serves RunNamedAsync/RunGroupAsync/RunAllAsync lookups, the
    /// composite is the fast, short-circuiting pass/fail verdict. See
    /// docs/samples/graduation-requirement-verdict/README.md's second
    /// diagram.
    /// </returns>
    public static (RulesEngine Engine, AndRule Graduates) BuildGraduationCheck(
        IReadOnlyList<SubjectPolicy> policies, int electiveMinimum)
    {
        var subjectRules = policies.Select(RuleForSubject).ToList();
        var engine = new RulesEngine(subjectRules);

        var coreRules = subjectRules.Where(r => r.Group == "core").ToList();
        var electiveRules = subjectRules.Where(r => r.Group == "elective").ToList();

        var graduates = new AndRule("graduates",
        [
            new AndRule("all_core_subjects_pass", coreRules),
            new AtLeastNRule("elective_requirement", electiveRules, electiveMinimum),
            new FunctionRule("cgpa_met", CgpaMet),
            new FunctionRule("attendance_met", AttendanceMet),
        ]);
        return (engine, graduates);
    }
}

/// <summary>
/// One student's context plus the shared fixture's own <c>expected</c>
/// block, carried alongside as a raw <see cref="JsonElement"/> so the test
/// suite can read whichever field it needs without a matching C# type for
/// every fixture shape.
/// </summary>
/// <param name="Context">The context dictionary verdict-rules' rules read.</param>
/// <param name="Expected">The fixture's own expected-outcome block.</param>
public sealed record StudentRecord(Dictionary<string, object?> Context, JsonElement Expected);
