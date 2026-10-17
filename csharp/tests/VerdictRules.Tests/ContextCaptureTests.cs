using Xunit;

namespace VerdictRules.Tests;

/// <summary>
/// Every <c>await</c> inside this package uses
/// <c>ConfigureAwait(false)</c>, so no continuation resumes on the caller's
/// synchronization context.
/// </summary>
/// <remarks>
/// <para>
/// This is library-author hygiene with teeth: a consumer on a platform that
/// installs a synchronization context -- classic ASP.NET, WinForms, WPF --
/// who calls into this package from a sync-over-async wrapper deadlocks if a
/// continuation tries to resume on a context that caller is still blocking.
/// </para>
/// <para>
/// Nothing else in this suite can observe it. <c>ConfigureAwait</c> changes
/// only where a continuation runs, never the value computed, so every
/// assertion on a <see cref="RuleResult"/> passes identically either way --
/// which is exactly why mutating <c>false</c> to <c>true</c> at each of the
/// nine await sites survived the mutation run until these tests existed.
/// Installing a real context and counting what it was asked to run is the
/// only way to see the difference.
/// </para>
/// </remarks>
public class ContextCaptureTests
{
    /// <summary>
    /// Records every continuation posted to it, without being a real
    /// scheduler: continuations still run inline so a test cannot hang, but
    /// the count proves whether the package tried to come back here.
    /// </summary>
    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public int PostCount { get; private set; }

        public override void Post(SendOrPostCallback d, object? state)
        {
            PostCount++;
            d(state);
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            PostCount++;
            d(state);
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/> with a recording context installed and
    /// returns how many continuations were posted back to it.
    /// </summary>
    /// <remarks>
    /// The await inside forces at least one real continuation: a predicate
    /// that completes synchronously would let the compiler skip the
    /// continuation machinery entirely, and the test would pass without
    /// proving anything.
    /// </remarks>
    private static int PostsDuring(Func<Task> work)
    {
        var original = SynchronizationContext.Current;
        var recording = new RecordingSynchronizationContext();
        try
        {
            SynchronizationContext.SetSynchronizationContext(recording);
            work().GetAwaiter().GetResult();
            return recording.PostCount;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(original);
        }
    }

    /// <summary>
    /// A rule whose predicate hands back a task that is genuinely incomplete
    /// when the package awaits it, so the await cannot be optimized away.
    /// </summary>
    /// <remarks>
    /// Deliberately <c>Task.Run</c> rather than an <c>async</c> predicate
    /// containing <c>await Task.Yield()</c>. An <c>async</c> predicate's own
    /// await is consumer code, which captures the context legitimately and
    /// posts one continuation of its own -- every assertion below would then
    /// be measuring the test's predicate instead of the package.
    /// </remarks>
    private static FunctionRule Yielding(string name, bool passed = true) =>
        new(name, (_, _) => Task.Run(() => new PredicateOutcome(passed)));

    private static Dictionary<string, object?> EmptyContext() => [];

    [Fact]
    public void TheRecordingContextSeesContinuationsThatDoNotOptOut()
    {
        // The control. Without this, every assertion below would also pass
        // against a context that is never consulted by anything at all, and
        // the suite would be measuring its own harness rather than the
        // package.
        var posts = PostsDuring(async () =>
        {
            await Task.Yield();
        });

        Assert.True(posts > 0, "the harness itself must be able to observe a captured context");
    }

    [Fact]
    public void FunctionRuleDoesNotResumeOnTheCallersContext()
    {
        var posts = PostsDuring(() => Yielding("a").EvaluateAsync(EmptyContext()));

        Assert.Equal(0, posts);
    }

    [Fact]
    public void NotRuleDoesNotResumeOnTheCallersContext()
    {
        var posts = PostsDuring(() => new NotRule("not_a", Yielding("a")).EvaluateAsync(EmptyContext()));

        Assert.Equal(0, posts);
    }

    [Fact]
    public void AndRuleDoesNotResumeOnTheCallersContext()
    {
        var and = new AndRule("and1", [Yielding("a"), Yielding("b")]);

        Assert.Equal(0, PostsDuring(() => and.EvaluateAsync(EmptyContext())));
    }

    [Fact]
    public void OrRuleDoesNotResumeOnTheCallersContext()
    {
        // Every branch fails, so the evaluator runs to exhaustion rather than
        // stopping at the first sub-rule -- more awaits, all of which must
        // still opt out.
        var or = new OrRule("or1", [Yielding("a", passed: false), Yielding("b", passed: false)]);

        Assert.Equal(0, PostsDuring(() => or.EvaluateAsync(EmptyContext())));
    }

    [Fact]
    public void SequentialEvaluatorDoesNotResumeOnTheCallersContext()
    {
        var evaluator = new SequentialEvaluator<IReadOnlyDictionary<string, object?>>(
            (latest, soFar, total) => soFar.Count == total ? latest.Passed : null,
            vacuousResult: true);

        var posts = PostsDuring(() => evaluator.EvaluateAsync(
            "custom", [Yielding("a"), Yielding("b")], EmptyContext()));

        Assert.Equal(0, posts);
    }

    [Fact]
    public void EveryEngineRunModeDoesNotResumeOnTheCallersContext()
    {
        // All five run modes, including both try-prefixed forms, since each
        // has its own await site rather than sharing one.
        var engine = new RulesEngine([Yielding("a", passed: false), Yielding("b")]);
        var context = EmptyContext();

        Assert.Equal(0, PostsDuring(() => engine.RunAllAsync(context)));
        Assert.Equal(0, PostsDuring(() => engine.RunNamedAsync("a", context)));
        Assert.Equal(0, PostsDuring(() => engine.TryRunNamedAsync("a", context)));
    }

    [Fact]
    public void GroupRunModesDoNotResumeOnTheCallersContext()
    {
        // Both group forms separately: TryRunGroupAsync is what RunGroupAsync
        // is built on, but each has its own await site rather than sharing one.
        var engine = new RulesEngine([
            new FunctionRule("a", (_, _) => Task.Run(() => new PredicateOutcome(true)), group: "g"),
        ]);

        Assert.Equal(0, PostsDuring(() => engine.RunGroupAsync("g", EmptyContext())));
        Assert.Equal(0, PostsDuring(() => engine.TryRunGroupAsync("g", EmptyContext())));
    }
}
