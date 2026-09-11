<!-- Title: Graduation Verdict Fixture Contract -->
# Graduation verdict — shared fixture contract

> The single set of inputs and expected outcomes that **every** language
> port of verdict must reproduce. It is the evidence behind the claim
> that this is the same engine in another language, rather than
> something that merely resembles it. A port is not at `0.1.0` until it
> passes this. See
> [`../../docs/adding-a-language.md`](../../docs/adding-a-language.md).

## Why this is shared and pinned

A rule engine can return the **right verdict for every student while
having destroyed short-circuiting** — the boolean is identical whether
sub-rules are evaluated one at a time or all at once. That is the single
easiest guarantee in this package to break silently, and it is exactly
what a language's "run these concurrently" primitive does
(`Promise.all`, `Task.WhenAll`, `Future.wait`).

So the fixture pins more than the verdict. It records **how many rules
actually ran**, which makes that failure visible.

## Files

| File | Contents |
| :-- | :-- |
| `policies.json` | The curriculum: subjects, their thresholds, which are electives, and how many electives are required |
| `students.json` | Eight students, their scores, and the expected outcome for each |
| `edge_cases.json` | Degenerate curricula proving vacuous-truth polarity |

## What each expectation proves

Every student carries an `expected` block:

| Field | Proves |
| :-- | :-- |
| `passed` | The verdict itself |
| `rules_evaluated` | **Short-circuiting is real.** Counts only the top-level sub-rules that actually ran |
| `failing_rule` | The correct rule is blamed, not merely *a* failure |
| `failing_chain` | `RuleResult.data` is **never flattened** — the path is preserved through nesting |
| `run_all` | `run_all` never short-circuits: it reports every registered rule regardless of failure |
| `groups` | Group registration and dispatch work, including that a group's verdict is an "all passed" over its members |

### The contrasts worth understanding

**`bob` and `gita` both fail.** Identical booleans. `bob` evaluated
**1** rule, `gita` evaluated **4** — because bob fails the first
requirement and gita fails the last. A port that lost short-circuiting
would report 4 for both and still "pass" a boolean-only fixture.

**`run_all` is always 7.** Every student, regardless of outcome, while
the composite evaluates between 1 and 4. That constant against that
variation *is* the documented difference between the two run modes.

**`harish` graduates but his elective group fails.** Not a
contradiction: `run_group("elective")` reports whether *all* electives
passed, while the composite's requirement is *at least two of three*.
A port that conflated "group verdict" with "at-least-N verdict" would
disagree here and nowhere else.

**`farhan` and `gita` fail while `run_all` passes.** Also correct: the
engine holds only the per-subject rules, and the cgpa and attendance
requirements live on the composite. The two structures are built from
the same rule objects but answer different questions.

## Rule names are part of the contract

`failing_rule` and `failing_chain` pin these names, so every port must
build the same composite structure:

- `all_core_subjects_pass` — an all-must-pass composite over core subjects
- `elective_requirement` — an at-least-N composite over electives
- `cgpa_met`, `attendance_met` — single predicate rules
- Per-subject rules named by `subject_id`, with compound subjects
  nesting a `<subject_id>:practical` sub-rule

That structure is the thing being proven portable, so pinning it is
deliberate.

## What is deliberately *not* pinned

- **The `detail` string.** `'ENG101' failed: 30 vs 40` is idiomatic
  phrasing; each language should word it naturally.
- **The example program's output.** Its *shape* follows the established
  pattern — a detailed lookup, then a batch verdict from one engine
  built once — but the printing is each language's own.
- **The oracle and chaos generators.** No two languages produce
  identical pseudo-random sequences from one seed, so each port writes
  its own, seeded and independently reproducible.

## Regenerating

The expectations were **observed from a passing implementation**, never
hand-written. If a deliberate behavioural change makes them stale,
regenerate them from the reference implementation rather than editing
by hand — and treat any unexplained diff as a regression, not a fixture
problem.
