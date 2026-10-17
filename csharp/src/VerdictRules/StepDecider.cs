namespace VerdictRules;

/// <summary>
/// Decides, after one more sub-rule has been evaluated, whether a
/// <see cref="SequentialEvaluator{TContext}"/> should stop.
/// </summary>
/// <remarks>
/// Invoked once per step -- once per sub-rule evaluated -- never as a
/// one-shot classifier of the whole run. Not parameterized by any context
/// type: it only ever reads <see cref="RuleResult"/>/counts, never the
/// evaluation context itself.
/// </remarks>
/// <param name="latest">The sub-result just produced, already appended to <paramref name="soFar"/>.</param>
/// <param name="soFar">Every sub-result produced so far, in evaluation order, including <paramref name="latest"/>.</param>
/// <param name="total">The total number of sub-rules this evaluation will run, absent an early stop.</param>
/// <returns>
/// <see langword="true"/>/<see langword="false"/> to stop now with that verdict,
/// or <see langword="null"/> to keep evaluating the next sub-rule.
/// </returns>
public delegate bool? StepDecider(RuleResult latest, IReadOnlyList<RuleResult> soFar, int total);
