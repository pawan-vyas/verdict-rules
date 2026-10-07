using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Fuzzes <see cref="GraduationCheck.CurriculumFromJson"/> -- the manual
/// validation <see cref="GraduationCheck.LoadCurriculum"/> runs after
/// <see cref="JsonDocument.Parse(string, JsonDocumentOptions)"/> -- with
/// deliberately malformed variants of <c>policies.json</c>'s shape.
/// </summary>
/// <remarks>
/// Every variant is generated from a seeded <see cref="Random"/>, so a
/// disagreement is exactly reproducible from its case index. See
/// <see cref="CurriculumFuzz"/> for the mutation strategies and the accepted
/// outcome contract.
/// </remarks>
public class CurriculumFuzzTests
{
    private const int FuzzSeed = 20260907;
    private const int NumFuzzCases = 150;

    public static IEnumerable<object[]> FuzzCaseIndexes() =>
        Enumerable.Range(0, NumFuzzCases).Select(i => new object[] { i });

    /// <summary>
    /// Every malformed variant either parses into a valid policy list or
    /// raises one of the reader's own documented error types -- never an
    /// unrelated crash (a raw <see cref="NullReferenceException"/>, an
    /// <see cref="IndexOutOfRangeException"/>, or a silent wrong-but-
    /// plausible policy list).
    /// </summary>
    [Theory]
    [MemberData(nameof(FuzzCaseIndexes))]
    public void MalformedCurriculumNeverCrashesOddlyOrSilentlyProducesGarbage(int caseIndex)
    {
        var rng = new Random(FuzzSeed + caseIndex);
        var json = CurriculumFuzz.GenerateMalformedVariant(rng);

        CurriculumFuzz.Outcome outcome;
        try
        {
            var (policies, electiveMinimum) = GraduationCheck.CurriculumFromJson(JsonDocument.Parse(json).RootElement);
            outcome = CurriculumFuzz.Outcome.ValidParse;
            // A successful parse must actually be a usable policy list --
            // the "silent wrong-but-plausible" failure mode this guards
            // against is a reader that returns without raising but hands
            // back something a consumer can't use.
            Assert.NotNull(policies);
            _ = electiveMinimum; // a parsed int, nothing further to validate about its shape
            foreach (var policy in policies)
            {
                Assert.False(string.IsNullOrEmpty(policy.SubjectId));
                Assert.False(string.IsNullOrEmpty(policy.SubjectType));
            }
        }
        catch (Exception ex) when (CurriculumFuzz.IsDocumentedErrorType(ex))
        {
            outcome = CurriculumFuzz.Outcome.DocumentedError;
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"caseIndex={caseIndex} (seed={FuzzSeed + caseIndex}): undocumented exception type {ex.GetType()}: {ex.Message}\n" +
                $"input: {json}");
            return;
        }

        Assert.True(
            outcome is CurriculumFuzz.Outcome.ValidParse or CurriculumFuzz.Outcome.DocumentedError,
            $"caseIndex={caseIndex}: neither a valid parse nor a documented error");
    }
}

/// <summary>
/// Deterministic generation of malformed <c>policies.json</c>-shaped JSON
/// text, plus the accepted-outcome contract <see cref="CurriculumFuzzTests"/>
/// checks every generated variant against.
/// </summary>
public static class CurriculumFuzz
{
    /// <summary>What happened when a malformed variant was fed to the reader.</summary>
    public enum Outcome
    {
        /// <summary>It parsed into a usable policy list.</summary>
        ValidParse,

        /// <summary>It raised one of the reader's own documented error types.</summary>
        DocumentedError,
    }

    /// <summary>
    /// Exception types <see cref="System.Text.Json"/> parsing and
    /// <see cref="GraduationCheck.CurriculumFromJson"/>'s own manual field
    /// access already raise for malformed input -- confirmed empirically
    /// against this fuzz harness, not a new validation layer the reader
    /// doesn't otherwise have. <see cref="JsonException"/> covers a
    /// truncated/unparseable document; <see cref="KeyNotFoundException"/> a
    /// dropped required key; <see cref="InvalidOperationException"/> a field
    /// of the wrong JSON value kind (a string where a number or object was
    /// expected, and vice versa); <see cref="FormatException"/> a number
    /// that doesn't fit the target numeric type.
    /// </summary>
    private static readonly IReadOnlyList<Type> DocumentedErrorTypes =
    [
        typeof(JsonException),
        typeof(KeyNotFoundException),
        typeof(InvalidOperationException),
        typeof(FormatException),
    ];

    public static bool IsDocumentedErrorType(Exception ex) =>
        DocumentedErrorTypes.Any(t => t.IsInstanceOfType(ex));

    private const string BaselineJson = """
        {
          "elective_minimum": 2,
          "subjects": [
            { "subject_id": "MATH101", "subject_type": "academic", "written_min_pct": 50.0 },
            { "subject_id": "WORKSHOP201", "subject_type": "vocational", "written_min_pct": 50.0, "practical_min_pct": 50.0 },
            { "subject_id": "FRENCH101", "subject_type": "language", "written_min_pct": 50.0, "exemption_allowed": true },
            { "subject_id": "ELECTIVE_ART", "subject_type": "academic", "written_min_pct": 50.0, "is_elective": true }
          ]
        }
        """;

    private static readonly string[] RequiredTopLevelKeys = ["elective_minimum", "subjects"];
    private static readonly string[] RequiredSubjectKeys = ["subject_id", "subject_type", "written_min_pct"];

    // Each variant takes an already-cloned, mutable baseline and corrupts it
    // one way. Adding a new malformed shape is a new row, never an edit to
    // an existing one or to the dispatch that picks among them.
    private static readonly IReadOnlyList<Action<Random, JsonObject>> NodeMutations =
    [
        WrongTypeTopLevelField,
        WrongTypeSubjectField,
        DropTopLevelKey,
        DropSubjectKey,
        AddUnexpectedTopLevelKey,
        AddUnexpectedSubjectKey,
        NestedJunkInPlaceOfScalar,
        EmptySubjectsArray,
        NullSubjects,
        NullSubjectEntry,
    ];

    // Whole-document variants that aren't well-formed JSON at all, so they
    // can't be expressed as a JsonNode mutation -- generated as raw text.
    private static readonly IReadOnlyList<Func<Random, string>> DocumentMutations =
    [
        _ => BaselineJson[..(BaselineJson.Length / 2)], // truncated mid-document
        _ => BaselineJson[..(BaselineJson.Length / 3)], // truncated earlier still
        _ => string.Empty, // empty document
        _ => "null", // JSON null in place of the whole curriculum object
        _ => "[]", // a JSON array in place of the curriculum object
        _ => "42", // a bare scalar in place of the curriculum object
        _ => "{", // unterminated object
    ];

    /// <summary>
    /// Build one deterministically-random malformed <c>policies.json</c>-shaped
    /// document, chosen from the full space of whole-document and field-level
    /// mutation strategies.
    /// </summary>
    public static string GenerateMalformedVariant(Random rng)
    {
        // Mostly field-level mutations (the richer, more realistic space),
        // occasionally a whole-document one.
        if (rng.Next(0, 5) == 0)
        {
            return rng.Choice(DocumentMutations)(rng);
        }

        var curriculum = JsonNode.Parse(BaselineJson)!.AsObject();
        rng.Choice(NodeMutations)(rng, curriculum);
        return curriculum.ToJsonString();
    }

    private static JsonArray SubjectsOf(JsonObject curriculum) => curriculum["subjects"]!.AsArray();

    private static JsonObject RandomSubject(Random rng, JsonObject curriculum)
    {
        var subjects = SubjectsOf(curriculum);
        return subjects[rng.Next(subjects.Count)]!.AsObject();
    }

    private static void WrongTypeTopLevelField(Random rng, JsonObject curriculum) =>
        curriculum["elective_minimum"] = rng.Choice<JsonNode?>(
            [JsonValue.Create("not_a_number"), JsonValue.Create(true), new JsonArray(1, 2)]);

    private static void WrongTypeSubjectField(Random rng, JsonObject curriculum)
    {
        var subject = RandomSubject(rng, curriculum);
        var field = rng.Choice(RequiredSubjectKeys);
        subject[field] = field switch
        {
            "written_min_pct" => JsonValue.Create("not_a_number"),
            "subject_id" => JsonValue.Create(12345),
            "subject_type" => JsonValue.Create(false),
            _ => throw new InvalidOperationException($"no wrong-type mutation registered for subject field '{field}'"),
        };
    }

    private static void DropTopLevelKey(Random rng, JsonObject curriculum) =>
        curriculum.Remove(rng.Choice(RequiredTopLevelKeys));

    private static void DropSubjectKey(Random rng, JsonObject curriculum) =>
        RandomSubject(rng, curriculum).Remove(rng.Choice(RequiredSubjectKeys));

    private static void AddUnexpectedTopLevelKey(Random rng, JsonObject curriculum) =>
        curriculum[$"unexpected_field_{rng.Next(1000)}"] = "surprise";

    private static void AddUnexpectedSubjectKey(Random rng, JsonObject curriculum) =>
        RandomSubject(rng, curriculum)[$"unexpected_field_{rng.Next(1000)}"] = 99;

    private static void NestedJunkInPlaceOfScalar(Random rng, JsonObject curriculum)
    {
        var subject = RandomSubject(rng, curriculum);
        var field = rng.Choice(RequiredSubjectKeys);
        subject[field] = new JsonObject { ["nested"] = new JsonObject { ["deeper"] = new JsonArray(1, 2, 3) } };
    }

    private static void EmptySubjectsArray(Random rng, JsonObject curriculum) =>
        curriculum["subjects"] = new JsonArray();

    private static void NullSubjects(Random rng, JsonObject curriculum) =>
        curriculum["subjects"] = null;

    private static void NullSubjectEntry(Random rng, JsonObject curriculum)
    {
        var subjects = SubjectsOf(curriculum);
        subjects[rng.Next(subjects.Count)] = null;
    }
}
