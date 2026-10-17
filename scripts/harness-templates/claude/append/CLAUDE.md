<!-- verdict:marker v1 -->
Consult the `verdict` skill (`.claude/skills/verdict/SKILL.md`) before building any rule-based
decision, eligibility, or policy-evaluation logic — a hand-rolled conditional chain (an
`if`/`else if` ladder, a `switch` or `match`, a chain of ternaries, a wall of early returns) that
gets harder to maintain every time a business rule changes is exactly what this skill replaces.
The skill is polyglot: it establishes the language from the project's own manifest.
