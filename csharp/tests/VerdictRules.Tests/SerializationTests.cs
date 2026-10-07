using System.Text.Json;
using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// A result has to be able to leave the process -- into a log line, an audit
/// record, an HTTP response.
/// </summary>
/// <remarks>
/// <para>
/// This is why <see cref="RuleResult.GetLeaves"/> and
/// <see cref="RuleResult.GetFailingLeaves"/> are methods rather than
/// properties. A leaf result's own leaves list is itself, so exposing that as
/// a property gave <c>System.Text.Json</c> a <c>$.Leaves.Leaves.Leaves...</c>
/// path to walk and it died at its 32-level depth limit. Nothing about
/// <c>JsonSerializerOptions</c> fixes that from the consumer's side:
/// <c>IgnoreReadOnlyProperties</c> does not apply to read-only collection
/// properties, and the per-member attribute that would is not in-box for
/// <c>netstandard2.1</c>.
/// </para>
/// <para>
/// Serialization here is one-way by design. These types carry no
/// <c>[JsonConstructor]</c> and make no round-trip promise -- a consumer
/// reading a result back wants their own DTO, not this package's class. What
/// is promised, and what these tests pin, is that writing one out works and
/// loses nothing derivable.
/// </para>
/// </remarks>
public class SerializationTests
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    private static FunctionRule Rule(string name, bool passed = true) =>
        new(name, (_, _) => Task.FromResult(new PredicateOutcome(passed)));

    private static Dictionary<string, object?> EmptyContext() => [];

    [Fact]
    public void ALeafResultSerializes()
    {
        var leaf = new RuleResult("a", false, detail: "too young");

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(leaf, Options));
        var root = document.RootElement;

        Assert.Equal("a", root.GetProperty("RuleName").GetString());
        Assert.False(root.GetProperty("Passed").GetBoolean());
        Assert.Equal("too young", root.GetProperty("Detail").GetString());
        Assert.Empty(root.GetProperty("SubResults").EnumerateArray());
    }

    [Fact]
    public async Task ADeepPartlyFailingTreeSerializesWithItsStructureIntact()
    {
        var a = new AndRule("a", [
            Rule("a1"),
            new OrRule("a2", [Rule("a2x", passed: false), Rule("a2y")]),
            new NotRule("a3", Rule("a3-inner")),
        ]);
        var result = await new AndRule("root", [a, Rule("b")]).EvaluateAsync(EmptyContext());

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, Options));
        var root = document.RootElement;

        // root short-circuited on 'a', so 'b' never ran and must be absent --
        // not present-and-failing.
        var subNames = root.GetProperty("SubResults").EnumerateArray()
            .Select(e => e.GetProperty("RuleName").GetString() ?? string.Empty).ToArray();
        Assert.Equal(["a"], subNames);

        // The real failure, three levels down, survives the encode.
        var a3 = root.GetProperty("SubResults")[0].GetProperty("SubResults")[2];
        Assert.Equal("a3", a3.GetProperty("RuleName").GetString());
        Assert.False(a3.GetProperty("Passed").GetBoolean());
    }

    [Fact]
    public async Task TheDerivedViewsAreAbsentFromTheSerializedForm()
    {
        var result = await new AndRule("root", [Rule("a"), Rule("b", passed: false)])
            .EvaluateAsync(EmptyContext());

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, Options));
        var names = document.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();

        Assert.Equal(["Data", "DecidedByIndices", "Detail", "Passed", "RuleName", "SubResults"], names);
    }

    [Fact]
    public async Task SerializedSizeGrowsWithDepthNotExponentiallyInIt()
    {
        // Forty levels of real nesting. With the deciding children stored as
        // RuleResult objects rather than positions, the same children were
        // reachable under two properties, so each level doubled the output:
        // depth 16 measured 13.6 MB and forty levels threw OutOfMemoryException.
        IRule rule = Rule("leaf", passed: false);
        for (var level = 0; level < 40; level++)
        {
            rule = new AndRule($"level{level}", [rule]);
        }

        var result = await rule.EvaluateAsync(EmptyContext());

        // Past the serializer's own default depth limit of 32, so a consumer
        // nesting this deep raises MaxDepth -- the budget is now spent on
        // genuine nesting rather than on duplicated subtrees.
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { MaxDepth = 128 });

        Assert.True(json.Length < 10_000, $"expected linear growth, got {json.Length} chars");
        Assert.Contains("\"leaf\"", json);
    }

    [Fact]
    public async Task ARunResultSerializes()
    {
        var engine = new RulesEngine([Rule("a"), Rule("b", passed: false)]);
        var run = await engine.RunAllAsync(EmptyContext());

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(run, Options));
        var root = document.RootElement;

        Assert.False(root.GetProperty("Passed").GetBoolean());
        Assert.Equal(["Passed", "Results"], root.EnumerateObject().Select(p => p.Name).Order());
        Assert.Equal(
            ["a", "b"],
            root.GetProperty("Results").EnumerateArray().Select(e => e.GetProperty("RuleName").GetString()));
    }

    [Fact]
    public void AResultBuiltFromAListTheCallerThenAppendedToStillSerializes()
    {
        // The early-define/late-init shape. Copying on construction severs the
        // alias, so the encoder walks a finite tree instead of recursing until
        // it hits its depth limit.
        var kids = new List<RuleResult>();
        var result = new RuleResult("p", false, subResults: kids);
        kids.Add(result);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(result, Options));

        Assert.Empty(document.RootElement.GetProperty("SubResults").EnumerateArray());
    }

    [Fact]
    public void ACyclicPayloadInDataFailsAtTheSerializerNotInsideThePackage()
    {
        // Data is opaque: the package never reads it and never promises it is
        // encodable. A caller putting a cycle of their own in there gets the
        // serializer's own failure, not a package error and not a silently
        // dropped field.
        var result = new RuleResult("a", true, data: new SelfReferential());

        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(result, Options));
    }

    private sealed class SelfReferential
    {
        public SelfReferential Self => this;
    }
}
