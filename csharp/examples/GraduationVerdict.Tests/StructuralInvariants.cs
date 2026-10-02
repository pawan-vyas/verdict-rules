using VerdictRules;
using Xunit;

namespace GraduationVerdict.Tests;

/// <summary>
/// Walks a built <c>graduates</c> tree's real <see cref="IRule"/> objects
/// together with the <see cref="RuleResult"/> tree they produced, and
/// asserts the short-circuit/no-short-circuit/leaf shape each concrete rule
/// type promises.
/// </summary>
/// <remarks>
/// <see cref="AndRule"/>/<see cref="OrRule"/> don't expose their own
/// sub-rules publicly (by design -- see their <c>SubRules</c> debugger-only
/// property in <c>src/VerdictRules</c>), so this walker carries the known
/// sub-rule objects alongside each node explicitly via <see cref="Node"/>
/// rather than reflecting into the rule itself. The one place that's
/// unavailable -- the written/practical/exemption <see cref="FunctionRule"/>
/// a per-subject <see cref="AndRule"/>/<see cref="OrRule"/> builds internally
/// inside <see cref="GraduationCheck.RuleForSubject"/> -- doesn't need to be:
/// <c>rule_for_subject</c>'s own docstring guarantees those children are
/// always leaves, so <see cref="Node.Children"/> being <see langword="null"/>
/// there just means "confirm every child is a leaf," which is exactly the
/// contract being proven.
/// </remarks>
public static class StructuralInvariants
{
    /// <summary>A rule paired with the known <see cref="IRule"/> objects of its own children, when known.</summary>
    /// <param name="Rule">The real rule object this node checks.</param>
    /// <param name="Children">
    /// This rule's own sub-rules, in the same order <see cref="RuleResult.SubResults"/>
    /// will list their results -- or <see langword="null"/> when the concrete
    /// child objects aren't reachable from outside <see cref="GraduationCheck"/>,
    /// in which case every child is asserted to be a leaf result instead of
    /// being recursed into.
    /// </param>
    public readonly record struct Node(IRule Rule, IReadOnlyList<Node>? Children);

    // Closed set of concrete rule shapes this example ever builds -- a new
    // shape is a new row here, never an edit to the dispatch itself.
    private static readonly Dictionary<Type, Action<Node, RuleResult>> NodeCheckers = new()
    {
        [typeof(AndRule)] = (node, result) => CheckAndOr(node, result, isAnd: true),
        [typeof(OrRule)] = (node, result) => CheckAndOr(node, result, isAnd: false),
        [typeof(AtLeastNRule)] = CheckAtLeastN,
        [typeof(FunctionRule)] = (_, result) => AssertLeafShape(result),
    };

    /// <summary>
    /// Assert this node's own short-circuit/no-short-circuit/leaf contract,
    /// then recurse into every child whose real rule object is known.
    /// </summary>
    public static void AssertNode(Node node, RuleResult result)
    {
        if (!NodeCheckers.TryGetValue(node.Rule.GetType(), out var check))
        {
            throw new InvalidOperationException(
                $"no structural-invariant checker registered for rule type {node.Rule.GetType()}");
        }
        check(node, result);
    }

    /// <summary>
    /// <see cref="AndRule"/>: a failing result's sub-results are all-pass-but-
    /// the-last (proves it stopped at exactly the first failure). A passing
    /// result's sub-results are all-pass (proves nothing was skipped).
    /// <see cref="OrRule"/>: the mirror image -- a passing result is
    /// all-fail-but-the-last; a failing result is all-fail (proves a full
    /// failure means every child really ran, since <see cref="OrRule"/> only
    /// stops on a pass).
    /// </summary>
    private static void CheckAndOr(Node node, RuleResult result, bool isAnd)
    {
        var data = SubResultsOf(result);
        var passPolarity = isAnd ? result.Passed : !result.Passed;
        if (passPolarity)
        {
            Assert.All(data, r => Assert.True(isAnd ? r.Passed : !r.Passed));
        }
        else
        {
            Assert.True(data.Count > 0, "a non-vacuous AndRule/OrRule outcome needs at least one sub-result");
            for (var i = 0; i < data.Count - 1; i++)
            {
                Assert.True(isAnd ? data[i].Passed : !data[i].Passed);
            }
            Assert.True(isAnd ? !data[^1].Passed : data[^1].Passed);
        }
        RecurseOrAssertLeaves(node.Children, data);
    }

    /// <summary>
    /// <see cref="AtLeastNRule"/> never short-circuits: its result always has
    /// exactly one entry per sub-rule, regardless of pass/fail.
    /// </summary>
    private static void CheckAtLeastN(Node node, RuleResult result)
    {
        var data = SubResultsOf(result);
        if (node.Children is { } children)
        {
            Assert.Equal(children.Count, data.Count);
        }
        RecurseOrAssertLeaves(node.Children, data);
    }

    /// <summary>
    /// Recurse into each child whose real rule object is known, pairing it by
    /// index with its own result -- <paramref name="data"/> may hold fewer
    /// entries than <paramref name="children"/> for an <see cref="AndRule"/>/
    /// <see cref="OrRule"/> that short-circuited, but never more.
    /// </summary>
    private static void RecurseOrAssertLeaves(IReadOnlyList<Node>? children, IReadOnlyList<RuleResult> data)
    {
        if (children is null)
        {
            Assert.All(data, AssertLeafShape);
            return;
        }
        Assert.True(data.Count <= children.Count, "more sub-results than this node has known sub-rules");
        for (var i = 0; i < data.Count; i++)
        {
            AssertNode(children[i], data[i]);
        }
    }

    /// <summary>A leaf result has no sub-results of its own.</summary>
    private static void AssertLeafShape(RuleResult result) =>
        Assert.True(result.SubResults.Count == 0, $"'{result.RuleName}' expected to be a leaf result");

    /// <summary>
    /// Unlike the old <c>Data</c>-duck-typing this replaced, <see cref="RuleResult.SubResults"/>
    /// is unambiguous by construction -- no throwing guard is needed here
    /// anymore, including for a vacuous composite's legitimately empty list.
    /// </summary>
    private static IReadOnlyList<RuleResult> SubResultsOf(RuleResult result) => result.SubResults;

    /// <summary>
    /// Build the <see cref="Node"/> mirror of the tree
    /// <see cref="GraduationCheck.BuildGraduationCheck"/> assembles, for the
    /// exact same policies -- same rule objects' shapes, same order, so a
    /// <see cref="RuleResult"/> that tree produced can be walked by this one.
    /// </summary>
    public static Node BuildGraduatesMirror(IReadOnlyList<SubjectPolicy> policies, int electiveMinimum)
    {
        var subjectRules = policies.Select(GraduationCheck.RuleForSubject).ToList();
        var coreRules = subjectRules.Where(r => r.Group == "core").ToList();
        var electiveRules = subjectRules.Where(r => r.Group == "elective").ToList();

        var allCoreSubjectsPass = new AndRule("all_core_subjects_pass", coreRules);
        var electiveRequirement = new AtLeastNRule("elective_requirement", electiveRules, electiveMinimum);
        var cgpaMet = new FunctionRule("cgpa_met", GraduationCheck.CgpaMet);
        var attendanceMet = new FunctionRule("attendance_met", GraduationCheck.AttendanceMet);
        var graduates = new AndRule("graduates", [allCoreSubjectsPass, electiveRequirement, cgpaMet, attendanceMet]);

        return new Node(graduates,
        [
            new Node(allCoreSubjectsPass, coreRules.Select(r => new Node(r, null)).ToList()),
            new Node(electiveRequirement, electiveRules.Select(r => new Node(r, null)).ToList()),
            new Node(cgpaMet, null),
            new Node(attendanceMet, null),
        ]);
    }

    /// <summary>
    /// Deep-equality check for two <see cref="RuleResult"/> trees -- every
    /// field, recursively, not just the top-level <see cref="RuleResult.Passed"/>.
    /// </summary>
    public static void AssertResultTreesEqual(RuleResult a, RuleResult b)
    {
        Assert.Equal(a.RuleName, b.RuleName);
        Assert.Equal(a.Passed, b.Passed);
        Assert.Equal(a.Detail, b.Detail);
        Assert.Equal(a.Data, b.Data);
        Assert.Equal(a.SubResults.Count, b.SubResults.Count);
        for (var i = 0; i < a.SubResults.Count; i++)
        {
            AssertResultTreesEqual(a.SubResults[i], b.SubResults[i]);
        }
    }
}
