<!-- Title: Run Costly Subagent Batches Sequentially -->
# A batch of costly subagents runs one at a time, never in parallel

> Skill evals, documentation sweeps, mutation runs -- any set of subagent
> tasks expensive enough that losing the batch matters -- are launched
> **one at a time**, each awaited before the next starts. Parallel
> launches risk a rate limit landing mid-batch, which burns tokens on
> partial work across many agents at once instead of failing early on
> one.

## Why it is worth the wall-clock cost

A rate-limited batch is the expensive failure, not a slow batch. Ten
agents stopped halfway have each consumed their context and produced
nothing usable; one agent stopped halfway has cost one agent. Sequential
execution also means a defect in the prompt shows up after the first
agent rather than after all ten, which is when it is still cheap to fix.

The ordering has a second benefit for this repo specifically: a later
agent can act on what an earlier one found. During the documentation
sweep, the Python group reported that the JS quickstart carried the same
stale claim it had just fixed, and the C# group reported a defect in the
Python and JS example sources -- cross-group findings that a parallel fan-out
produces only as conflicting simultaneous edits, if at all.

## How to apply

In a `Workflow` script, `await` each `agent()` call inside a plain loop
rather than reaching for `parallel()` or `pipeline()`. The orchestration
primitives default to concurrency, so sequencing is the thing you have to
ask for:

```js
for (const group of GROUPS) {
  const result = await agent(promptFor(group), { label: `audit:${group.key}` })
  results.push(result)
}
```

Outside a workflow, launch one `Agent` call, wait for its completion
notification, then launch the next -- never several in one turn.

## Scope

This is the repo-local application of a rule that is not specific to
verdict-rules; the cross-project statement of it lives in the harness's
own global memory store, which is the one kind of fact that legitimately
belongs there rather than here. Recorded in the repo as well because
nothing in the harness store travels to another machine, another tool, or
a fresh clone -- and this repo's own doc sweeps and skill evals are
exactly the batches it governs. See
[`skill-evals-are-additive-per-target`](skill-evals-are-additive-per-target.md)
for what those eval batches are, and
[`doc-hygiene-audit-protocol`](doc-hygiene-audit-protocol.md) for the
sweep they run.
