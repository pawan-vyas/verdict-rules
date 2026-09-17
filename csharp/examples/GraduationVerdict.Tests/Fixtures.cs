using VerdictRules;

namespace GraduationVerdict.Tests;

/// <summary>
/// Locates the shared, cross-language fixture directory at the repo root --
/// see fixtures/graduation_verdict/README.md for the contract every
/// language's own port reproduces.
/// </summary>
internal static class Fixtures
{
    public static readonly string Directory = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "fixtures", "graduation_verdict");

    public static string PathTo(string fileName) => Path.Combine(Directory, fileName);

    /// <summary>
    /// Walk the first failing branch down, collecting rule names. This is
    /// what proves a result's <see cref="RuleResult.Data"/> is never
    /// flattened: a nested failure has to still be reachable by following
    /// <c>Data</c> downward.
    /// </summary>
    public static List<string> FailingChain(RuleResult result)
    {
        var chain = new List<string>();
        var node = result;
        while (node.Data is IReadOnlyList<RuleResult> subs && subs.Count > 0)
        {
            var next = subs.FirstOrDefault(sub => !sub.Passed);
            if (next is null) break;
            chain.Add(next.RuleName);
            node = next;
        }
        return chain;
    }
}
