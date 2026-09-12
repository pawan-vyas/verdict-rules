# Verdict

[![PyPI](https://img.shields.io/pypi/v/verdict-rules.svg)](https://pypi.org/project/verdict-rules/)
[![Python Versions](https://img.shields.io/pypi/pyversions/verdict-rules.svg)](https://pypi.org/project/verdict-rules/)
[![Tests](https://github.com/pawan-vyas/verdict-rules/actions/workflows/test-python.yml/badge.svg)](https://github.com/pawan-vyas/verdict-rules/actions/workflows/test-python.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> This project lives as an extension of an idea from my mentor and
> guiding light — [`@SanjayVyas`](https://github.com/SanjayVyas), to
> whom I owe everything I know about building software that lasts; and
> beyond. 🙏

Every system built long enough accumulates the same shape of question:
*given what I know right now, is this allowed?* A rate limiter asks it
of a request. An access check asks it of a caller. A checkout page asks
it of a cart. The question is always the same shape — only the specific
conditions behind it change, and they tend to keep changing long after
the code asking the question has shipped.

Verdict is the small piece of infrastructure that question deserves: a
place to name each condition once, combine named conditions into a
verdict, and run that verdict against whatever facts a caller hands it
— a plain `dict`, nothing more. It has no idea what a rate limit is, or
a permission, or a discount. It only knows how to ask a rule "did you
pass?" and combine the answers honestly.

```python
from verdict import AndRule, FunctionRule, RuleResult

async def under_limit(ctx):
    return RuleResult("under_limit", ctx["used"] < ctx["quota"])

async def in_good_standing(ctx):
    return RuleResult("in_good_standing", ctx["strikes"] == 0)

allowed = AndRule("allowed", [
    FunctionRule("under_limit", under_limit),
    FunctionRule("in_good_standing", in_good_standing),
])

verdict = await allowed.evaluate({"used": 3, "quota": 10, "strikes": 1})
verdict.passed   # False
verdict.detail   # "'in_good_standing' failed"
```

That is the whole library in one screen. What it buys you is not the
composition — you could write that yourself in an afternoon — but the
guarantee underneath it:

```mermaid
graph LR
    Ctx[/"📥 context<br/>(plain dict)"/]
    Comp{"🔀 AndRule"}
    R1("✅ under_limit<br/>passed")
    R2("❌ in_good_standing<br/>failed")
    R3("⏭️ any_later_rule<br/>never evaluated")
    Out("📤 RuleResult<br/>passed=False")

    %% Link 0: context -> AndRule
    Ctx -->|"[1]<br/>facts in"| Comp
    %% Link 1: AndRule -> R1
    Comp -->|"[2]<br/>evaluates"| R1
    %% Link 2: AndRule -> R2
    Comp -->|"[3]<br/>evaluates"| R2
    %% Link 3: AndRule -> R3
    Comp -.->|"[4]<br/>stops here<br/>(never starts)"| R3
    %% Link 4: R2 -> Out
    R2 -->|"[5]<br/>verdict, with the reason"| Out

    style Ctx fill:#D0D0D0,stroke:#999999,stroke-width:2px,color:#000
    style Comp fill:#B47EFF,stroke:#9654E8,stroke-width:3px,color:#000
    style R1 fill:#8CE99A,stroke:#2F9E44,stroke-width:2px,color:#000
    style R2 fill:#FF6B6B,stroke:#C92A2A,stroke-width:2px,color:#000
    style R3 fill:#D0D0D0,stroke:#909090,stroke-width:2px,color:#000
    style Out fill:#51CF66,stroke:#37B24D,stroke-width:2px,color:#000

    %% Link Index:
    %% 0: facts enter as a plain dict
    %% 1: the first sub-rule is evaluated and passes
    %% 2: the second is evaluated and fails
    %% 3: everything after it is never started at all
    %% 4: the verdict carries which rule failed and why
    linkStyle 0 stroke:#E0E0E0,stroke-width:2px
    linkStyle 1 stroke:#A9E8B5,stroke-width:2px
    linkStyle 2 stroke:#FF9999,stroke-width:3px
    linkStyle 3 stroke:#B0B0B0,stroke-width:2px,stroke-dasharray:3 3
    linkStyle 4 stroke:#69DB7C,stroke-width:3px
```

> **Short-circuiting is a contract here, not an optimisation.** Rules are
> evaluated **sequentially, never concurrently**, so work after a decided
> outcome doesn't merely get discarded — it never starts. That matters
> the moment a rule does something: calls an API, takes a lock, writes an
> audit row. A library that evaluated all three concurrently would return
> the same `False` and be silently wrong.
>
> The rest follows from it. Facts are a plain `dict` Verdict never
> inspects. A `Rule` is anything with `name`, `group` and
> `evaluate()` — no base class, no registration. `RulesEngine` is the
> diagnostic counterpart, for when you want every rule's answer rather
> than the fastest one.

## Why it's shaped this way

- **Zero external dependencies.** Nothing to audit, nothing to pin,
  nothing that can break because a transitive package changed underneath
  it.
- **No knowledge of any domain.** Verdict doesn't know what a "rate
  limit" or a "discount" is — that vocabulary lives in a small adapter
  a caller writes, never inside this package. That's what lets the exact
  same engine answer unrelated questions in the same codebase without
  the two ever coupling to each other.
- **Rules are structurally typed, not inherited.** A custom rule never
  imports anything from this package or subclasses anything — it just
  needs a `name`, a `group`, and an `evaluate(context)` coroutine.

## Where to go next

Verdict is a polyglot design — the `Rule`/`FunctionRule`/`AndRule`/
`OrRule`/`RulesEngine` shape and its execution-model guarantees are
meant to exist in more than one language. **Only Python ships today.**
The docs below split the same way the repo does: language-agnostic
design rationale lives at the repo root; anything with directly
runnable code lives under that language's own directory.

| Doc | For |
|---|---|
| [`python/README.md`](python/README.md) | Python quickstart — `pip install verdict-rules`, first rule |
| [`python/docs/quickstart.md`](python/docs/quickstart.md) | Core concepts and a full worked example |
| [`docs/architecture.md`](docs/architecture.md) | Why it's shaped this way, in depth — type structure, the execution model |
| [`docs/extension.md`](docs/extension.md) | Building on top of it from your own code, with no changes here |
| [`docs/maintenance.md`](docs/maintenance.md) | Changing this package itself |
| [`docs/testing.md`](docs/testing.md) | How the test suite is organized, and what a change needs to prove |
| [`docs/future_plan.md`](docs/future_plan.md) | Exploratory feature candidates, and the test used to evaluate one |
| [`python/docs/samples/`](python/docs/samples/1_README.md) | Worked examples — dynamic discounts, fee waivers, tier promotions, moderation routing, data-driven rule sets |
| [`python/examples/`](python/examples/README.md) | Full, tested mini-projects behind the more comprehensive samples — real code, real tests, real docs |
| [`skills/verdict/SKILL.md`](skills/verdict/SKILL.md) | The AI-agent skill for building with Verdict |

## Development

```bash
cd python/
uv sync
uv run pytest
```

## Contributing

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for how to set up a change and
what it needs to prove before it's mergeable, and
[`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md) for community standards.
Licensed under [MIT](LICENSE). See [`CHANGELOG.md`](CHANGELOG.md) for
release history, grouped by language-scoped tag.
