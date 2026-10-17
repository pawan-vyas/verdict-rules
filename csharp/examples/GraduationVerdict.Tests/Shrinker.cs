using System.Text.Json;
using System.Text.Json.Nodes;

namespace GraduationVerdict.Tests;

/// <summary>
/// Failing-run-to-fixture shrinking: given a (policies, context,
/// electiveMinimum) case that reproduces some failure, progressively
/// simplify it while re-checking the same failure still reproduces, then
/// write the minimized result out as a standalone debugging fixture.
/// </summary>
/// <remarks>
/// Greedy, not a full delta-debugging search -- each pass tries one
/// simplification at a time and keeps it only if the failure still
/// reproduces, repeating until a full pass makes no further progress. That's
/// enough to turn a 7-subject, many-field chaos case into a small, readable
/// fixture; it doesn't claim to find the globally smallest one.
/// </remarks>
public static class GraduationShrinker
{
    /// <summary>
    /// Whether a given case still reproduces the target failure. Defined by
    /// the caller -- an oracle disagreement, a structural-invariant
    /// violation, anything that can be answered from the three inputs alone.
    /// </summary>
    public delegate Task<bool> FailurePredicate(
        IReadOnlyList<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum);

    /// <summary>
    /// Simplify <paramref name="policies"/>/<paramref name="context"/>/
    /// <paramref name="electiveMinimum"/> while <paramref name="stillFails"/>
    /// keeps returning <see langword="true"/>: fewer subjects, a lower
    /// elective threshold, then each remaining field snapped toward its
    /// simplest value.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The given case doesn't reproduce the failure to begin with.
    /// </exception>
    public static async Task<(IReadOnlyList<SubjectPolicy> Policies, Dictionary<string, object?> Context, int ElectiveMinimum)> Shrink(
        IReadOnlyList<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, FailurePredicate stillFails)
    {
        if (!await stillFails(policies, context, electiveMinimum))
        {
            throw new ArgumentException("the given case does not reproduce the failure it's supposed to be shrunk from");
        }

        var currentPolicies = policies.ToList();
        var currentContext = CloneContext(context);
        var currentMinimum = electiveMinimum;

        currentPolicies = await ShrinkSubjectList(currentPolicies, currentContext, currentMinimum, stillFails);
        currentMinimum = await ShrinkElectiveMinimum(currentPolicies, currentContext, currentMinimum, stillFails);
        currentPolicies = await SimplifyPolicyFields(currentPolicies, currentContext, currentMinimum, stillFails);
        currentContext = await SimplifyContextFields(currentPolicies, currentContext, currentMinimum, stillFails);

        return (currentPolicies, currentContext, currentMinimum);
    }

    /// <summary>Repeatedly try dropping one subject (and its matching score entry); keep the drop if the failure still reproduces.</summary>
    private static async Task<List<SubjectPolicy>> ShrinkSubjectList(
        List<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, FailurePredicate stillFails)
    {
        var current = policies;
        bool progress;
        do
        {
            progress = false;
            for (var i = current.Count - 1; i >= 0; i--)
            {
                var candidate = current.Where((_, idx) => idx != i).ToList();
                var candidateContext = WithoutSubjectScore(context, current[i].SubjectId);
                if (candidate.Count > 0 && await stillFails(candidate, candidateContext, electiveMinimum))
                {
                    current = candidate;
                    context.Clear();
                    foreach (var (key, value) in candidateContext) context[key] = value;
                    progress = true;
                }
            }
        } while (progress);
        return current;
    }

    /// <summary>Binary-search electiveMinimum down toward 0, the simplest possible threshold.</summary>
    private static async Task<int> ShrinkElectiveMinimum(
        List<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, FailurePredicate stillFails)
    {
        var low = 0;
        var high = electiveMinimum;
        if (high == low || !await stillFails(policies, context, low))
        {
            return high; // 0 itself doesn't reproduce it (or already 0); nothing to shrink
        }
        while (low < high)
        {
            var mid = low + (high - low) / 2;
            if (await stillFails(policies, context, mid))
            {
                high = mid;
            }
            else
            {
                low = mid + 1;
            }
        }
        return low;
    }

    /// <summary>For each policy, try snapping each of its own fields to its simplest value; keep each snap that preserves the failure.</summary>
    private static async Task<List<SubjectPolicy>> SimplifyPolicyFields(
        List<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, FailurePredicate stillFails)
    {
        var current = policies.ToList();
        for (var i = 0; i < current.Count; i++)
        {
            current[i] = await TrySnap(current[i], p => p with { WrittenMinPct = 0.0 }, current, context, electiveMinimum, stillFails);
            current[i] = await TrySnap(current[i], p => p with { PracticalMinPct = p.PracticalMinPct is null ? null : 0.0 }, current, context, electiveMinimum, stillFails);
            current[i] = await TrySnap(current[i], p => p with { ExemptionAllowed = false }, current, context, electiveMinimum, stillFails);
            current[i] = await TrySnap(current[i], p => p with { IsElective = false }, current, context, electiveMinimum, stillFails);
        }
        return current;

        async Task<SubjectPolicy> TrySnap(
            SubjectPolicy policy, Func<SubjectPolicy, SubjectPolicy> snap,
            List<SubjectPolicy> all, Dictionary<string, object?> ctx, int minimum, FailurePredicate predicate)
        {
            var snapped = snap(policy);
            if (Equals(snapped, policy))
            {
                return policy;
            }
            var candidateList = all.Select(p => p.SubjectId == policy.SubjectId ? snapped : p).ToList();
            return await predicate(candidateList, ctx, minimum) ? snapped : policy;
        }
    }

    /// <summary>For each numeric/boolean field in the context, try snapping it to its simplest value; keep each snap that preserves the failure.</summary>
    private static async Task<Dictionary<string, object?>> SimplifyContextFields(
        List<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, FailurePredicate stillFails)
    {
        var current = CloneContext(context);

        foreach (var topLevelField in new[] { "cgpa", "cgpa_floor", "attendance_pct", "attendance_floor" })
        {
            await TrySnapTopLevel(topLevelField, 0.0);
        }

        var scores = (Dictionary<string, object?>)current["scores"]!;
        foreach (var subjectId in scores.Keys.ToList())
        {
            var entry = (Dictionary<string, object?>)scores[subjectId]!;
            foreach (var field in entry.Keys.ToList())
            {
                if (entry[field] is double)
                {
                    await TrySnapScoreField(subjectId, field, 0.0);
                }
                else if (entry[field] is bool)
                {
                    await TrySnapScoreField(subjectId, field, false);
                }
            }
        }

        return current;

        async Task TrySnapTopLevel(string field, object simplestValue)
        {
            var original = current[field];
            if (Equals(original, simplestValue))
            {
                return;
            }
            current[field] = simplestValue;
            if (!await stillFails(policies, current, electiveMinimum))
            {
                current[field] = original;
            }
        }

        async Task TrySnapScoreField(string subjectId, string field, object simplestValue)
        {
            var entry = (Dictionary<string, object?>)((Dictionary<string, object?>)current["scores"]!)[subjectId]!;
            var original = entry[field];
            if (Equals(original, simplestValue))
            {
                return;
            }
            entry[field] = simplestValue;
            if (!await stillFails(policies, current, electiveMinimum))
            {
                entry[field] = original;
            }
        }
    }

    private static Dictionary<string, object?> WithoutSubjectScore(Dictionary<string, object?> context, string subjectId)
    {
        var clone = CloneContext(context);
        var scores = (Dictionary<string, object?>)clone["scores"]!;
        scores.Remove(subjectId);
        return clone;
    }

    private static Dictionary<string, object?> CloneContext(Dictionary<string, object?> context)
    {
        var clone = new Dictionary<string, object?>(context);
        var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
        var clonedScores = new Dictionary<string, object?>();
        foreach (var (subjectId, entry) in scores)
        {
            clonedScores[subjectId] = new Dictionary<string, object?>((IReadOnlyDictionary<string, object?>)entry!);
        }
        clone["scores"] = clonedScores;
        return clone;
    }

    /// <summary>
    /// Write a shrunk (policies, context, electiveMinimum) case out as a
    /// standalone JSON fixture, in the same <c>{ curriculum, student }</c>
    /// shape the shared cross-language <c>edge_cases.json</c> fixture uses,
    /// so it can be reloaded with <see cref="GraduationCheck.CurriculumFromJson"/>
    /// and <see cref="GraduationCheck.ContextFromJson"/> directly.
    /// </summary>
    public static void WriteFixture(
        string path, IReadOnlyList<SubjectPolicy> policies, Dictionary<string, object?> context, int electiveMinimum, string note)
    {
        var subjects = new JsonArray(policies.Select(p =>
        {
            var row = new JsonObject
            {
                ["subject_id"] = p.SubjectId,
                ["subject_type"] = p.SubjectType,
                ["written_min_pct"] = p.WrittenMinPct,
            };
            if (p.PracticalMinPct is { } practical) row["practical_min_pct"] = practical;
            if (p.ExemptionAllowed) row["exemption_allowed"] = true;
            if (p.IsElective) row["is_elective"] = true;
            return (JsonNode)row;
        }).ToArray());

        var scores = (IReadOnlyDictionary<string, object?>)context["scores"]!;
        var scoresNode = new JsonObject(scores.Select(kv =>
        {
            var entry = (IReadOnlyDictionary<string, object?>)kv.Value!;
            var row = new JsonObject { ["written_pct"] = (double)entry["written_pct"]! };
            if (entry.TryGetValue("practical_pct", out var pp)) row["practical_pct"] = (double)pp!;
            if (entry.TryGetValue("has_exemption", out var he)) row["has_exemption"] = (bool)he!;
            return new KeyValuePair<string, JsonNode?>(kv.Key, row);
        }));

        var fixture = new JsonObject
        {
            ["note"] = note,
            ["curriculum"] = new JsonObject { ["elective_minimum"] = electiveMinimum, ["subjects"] = subjects },
            ["student"] = new JsonObject
            {
                ["scores"] = scoresNode,
                ["cgpa"] = (double)context["cgpa"]!,
                ["cgpa_floor"] = (double)context["cgpa_floor"]!,
                ["attendance_pct"] = (double)context["attendance_pct"]!,
                ["attendance_floor"] = (double)context["attendance_floor"]!,
            },
        };

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, fixture.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
