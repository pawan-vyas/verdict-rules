using GraduationVerdict;

// Fixture data lives at the repo root, shared by every language's own port of
// this example -- see fixtures/graduation_verdict/README.md for the contract.
var fixtures = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "graduation_verdict");

var (policies, electiveMinimum) = GraduationCheck.LoadCurriculum(Path.Combine(fixtures, "policies.json"));
var students = GraduationCheck.LoadStudents(Path.Combine(fixtures, "students.json"));
var (engine, graduates) = GraduationCheck.BuildGraduationCheck(policies, electiveMinimum);

Console.WriteLine("=== Graduation Requirement Verdict -- Demo ===");
Console.WriteLine($"Built once from policies.json: {policies.Count} subjects\n");

var alice = students["alice"].Context;
Console.WriteLine("--- Detailed lookup for 'alice' ---");
var named = await engine.RunNamedAsync("WORKSHOP201", alice);
Console.WriteLine($"RunNamedAsync('WORKSHOP201'): {(named.Passed ? "PASS" : "FAIL")}");
var core = await engine.RunGroupAsync("core", alice);
Console.WriteLine("RunGroupAsync('core'):     " + string.Join(", ", core.Results.Select(r => $"{r.RuleName}={(r.Passed ? "PASS" : "FAIL")}")));
var elective = await engine.RunGroupAsync("elective", alice);
Console.WriteLine("RunGroupAsync('elective'): " + string.Join(", ", elective.Results.Select(r => $"{r.RuleName}={(r.Passed ? "PASS" : "FAIL")}")));
var full = await engine.RunAllAsync(alice);
Console.WriteLine($"RunAllAsync(): {full.Results.Count} subjects reported\n");

Console.WriteLine("--- Batch verdict across all students (same built engine, no rebuild) ---");
foreach (var (studentId, record) in students)
{
    var verdict = await graduates.EvaluateAsync(record.Context);
    var status = verdict.Passed ? "GRADUATES" : "DOES NOT GRADUATE";
    // `graduates` is a composite, and a composite leaves its own `Detail`
    // empty: composing a ShortCircuitEvaluator gives it no channel richer
    // than a boolean to build a descriptive string from. The reason lives in
    // the failing leaves instead, flattened from wherever in the tree the
    // short-circuit actually stopped.
    var reasons = string.Join(
        "; ",
        verdict.GetFailingLeaves()
            .Select(leaf => string.IsNullOrEmpty(leaf.Detail) ? leaf.RuleName : $"{leaf.RuleName}: {leaf.Detail}"));
    var reason = verdict.Passed ? "" : $"  ({reasons})";
    Console.WriteLine($"{studentId,-10}: {status}{reason}");
}
