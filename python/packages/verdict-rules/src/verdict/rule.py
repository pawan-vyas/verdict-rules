"""Rule definitions — the unit of evaluation this package is built around.

A :class:`Rule` has a ``name``, an optional ``group``, and an
``evaluate(context)`` coroutine returning a :class:`~verdict.result.RuleResult`.
:class:`FunctionRule` wraps a plain predicate (returning a
:class:`PredicateOutcome`, not a :class:`~verdict.result.RuleResult` directly
— see its own docstring for why). :class:`AndRule`/:class:`OrRule`/
:class:`NotRule` compose other rules; ``AndRule``/``OrRule`` short-circuit the
same way a boolean ``and``/``or`` expression does, by composing a shared
:class:`ShortCircuitEvaluator`, itself built on the more general
:class:`SequentialEvaluator` a custom composite can compose directly.

``Rule`` is generic over the context it reads from (``TContext``), erased at
runtime. ``Rule[dict[str, Any]]`` is the dict-context spelling; the bare
``Rule`` erases to ``Rule[Any]``.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Awaitable, Callable, Generic, Protocol, Sequence, TypeAlias, TypeVar, runtime_checkable

from verdict.result import RuleResult

TContext = TypeVar("TContext")


@runtime_checkable
class Rule(Protocol[TContext]):
    """Structural interface every rule (plain or composite) satisfies.

    Attributes:
        name: Unique identifier for this rule within a
            :class:`~verdict.engine.RulesEngine` — used for
            :meth:`~verdict.engine.RulesEngine.run_named` lookups and to
            label this rule's own entry in a
            :class:`~verdict.result.RunResult`.
        group: Optional group label — rules sharing a group can be run
            together via :meth:`~verdict.engine.RulesEngine.run_group`.
            ``None`` if this rule doesn't belong to any group.
    """

    name: str
    group: str | None

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate this rule against ``context``.

        Args:
            context: Data the rule's condition reads from.

        Returns:
            The outcome of evaluating this rule.
        """
        ...


@dataclass(frozen=True)
class PredicateOutcome:
    """What a predicate reports back to the :class:`FunctionRule` wrapping it.

    A predicate used to construct its own :class:`~verdict.result.RuleResult`
    directly, including its own ``rule_name`` — completely decoupled from
    whatever name the ``FunctionRule`` wrapping it was constructed with, and
    nothing kept the two in sync. The fix is structural, not a runtime
    check: a predicate returns this instead, and only
    :meth:`FunctionRule.evaluate` ever builds the final
    :class:`~verdict.result.RuleResult`, from the one name already fixed at
    construction.

    Deliberately has **no** ``name`` field — restating a name that's already
    fixed on the wrapping ``FunctionRule`` is never legitimate, not just
    inconvenient, so the capability doesn't exist. **No ``group`` field
    either, permanently** — ``RuleResult`` doesn't carry a ``group`` today;
    if that ever changes, ``group`` stays exclusively sourced from
    ``FunctionRule.group``, the same way ``name`` already is, never from the
    predicate. This generalizes beyond the one bug it was fixed for: any
    future rule-owned field on ``RuleResult`` gets the same guarantee for
    free, because a predicate has no path to set it at all.

    Attributes:
        passed: Whether the predicate's condition was satisfied.
        detail: Optional human-readable explanation of the outcome. Empty
            string when there's nothing to say beyond the boolean.
        data: Optional payload a caller can attach; opaque to this package.
    """

    passed: bool
    detail: str = ""
    data: object | None = None


RulePredicate: TypeAlias = Callable[[TContext], Awaitable[PredicateOutcome]]
"""An async predicate a :class:`FunctionRule` wraps: inspects ``context``
and reports a :class:`PredicateOutcome`, never a
:class:`~verdict.result.RuleResult` directly."""


class FunctionRule(Generic[TContext]):
    """Wraps a plain async predicate as a :class:`Rule`.

    ``TContext`` is inferred from the wrapped predicate's own type
    annotation. An untyped predicate infers ``Any``.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(
        self,
        name: str,
        predicate: RulePredicate[TContext],
        group: str | None = None,
    ) -> None:
        """Initialise with a name and the predicate to run.

        Args:
            name: Unique identifier for this rule.
            predicate: Async callable that inspects ``context`` and
                reports a :class:`PredicateOutcome` — never constructs a
                :class:`~verdict.result.RuleResult` itself.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._predicate = predicate

    async def evaluate(self, context: TContext) -> RuleResult:
        """Run the wrapped predicate and build this rule's own result.

        No longer a straight pass-through — the predicate only reports a
        :class:`PredicateOutcome`, so this is the one place that owns
        ``rule_name``, built fresh from ``self.name`` on every call.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` carrying this rule's own
            ``name`` and whatever the predicate reported.

        Raises:
            TypeError: ``predicate`` returned something other than a
                :class:`PredicateOutcome` — including a
                :class:`~verdict.result.RuleResult` built directly, the
                pre-``PredicateOutcome`` calling convention this package
                used to have. ``RulePredicate``'s own type annotation is
                only ever checked by a type checker, never enforced at
                runtime; an externally-authored predicate (not written
                against this package's own tests) can still return the
                wrong shape, and ``RuleResult`` happens to duck-type close
                enough to silently "work" by accident if this weren't
                checked explicitly.
        """
        outcome = await self._predicate(context)
        if not isinstance(outcome, PredicateOutcome):
            raise TypeError(
                f"FunctionRule {self.name!r}: predicate must return a PredicateOutcome, "
                f"got {type(outcome).__name__!r} ({outcome!r}) instead"
            )
        return RuleResult(
            rule_name=self.name,
            passed=outcome.passed,
            detail=outcome.detail,
            data=outcome.data,
        )

    def __repr__(self) -> str:
        suffix = f" ({self.group})" if self.group else ""
        return f'FunctionRule "{self.name}"{suffix}'


StepDecider: TypeAlias = Callable[[RuleResult, Sequence[RuleResult], int], bool | None]
"""Signature for the decision a :class:`SequentialEvaluator` delegates to:
given the latest sub-result, every sub-result gathered so far (including the
latest), and the total number of sub-rules, return ``True``/``False`` to
stop evaluating right now with that verdict, or ``None`` to keep going.
Invoked once per step (per sub-rule evaluated), not a one-shot classifier of
the whole run. Not parameterized by ``TContext`` — it only ever touches
``RuleResult``/counts, never the evaluation context itself."""


class SequentialEvaluator(Generic[TContext]):
    """Evaluates a list of sub-rules sequentially against one context,
    letting a :data:`StepDecider` choose when to stop.

    Composable — hold one as a field and delegate to it, the same way
    :class:`FunctionRule` holds a predicate. Plain class, no inheritance
    anywhere: every composite's dependency is a visible constructor
    argument, and each piece is unit-testable in isolation.
    """

    def __init__(self, decider: StepDecider, vacuous_result: bool) -> None:
        """Initialise with the stopping decision and the empty-list verdict.

        Args:
            decider: Called once per sub-rule evaluated — see
                :data:`StepDecider`.
            vacuous_result: What to report when ``rules`` is empty, or when
                ``decider`` never resolves to anything but ``None`` even
                after every sub-rule has been evaluated.
        """
        self._decider = decider
        self._vacuous_result = vacuous_result

    async def evaluate(
        self, name: str, rules: Sequence[Rule[TContext]], context: TContext
    ) -> RuleResult:
        """Evaluate ``rules`` in order against ``context``, stopping when
        ``decider`` says to.

        Args:
            name: The name the resulting :class:`~verdict.result.RuleResult`
                carries — the composite's own name, not any sub-rule's.
            rules: Sub-rules evaluated in order until ``decider`` decides,
                or the list is exhausted.
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for the composite itself,
            with ``sub_results`` holding every sub-result gathered before
            stopping.
        """
        # Checked before the loop, not derived from a post-loop fallback
        # indexing the last evaluated result -- an empty list has no last
        # element, so this must be an independent branch, not a special
        # case of the one below.
        if not rules:
            return RuleResult(rule_name=name, passed=self._vacuous_result, sub_results=())

        so_far: list[RuleResult] = []
        for rule in rules:
            latest = await rule.evaluate(context)
            so_far.append(latest)
            early = self._decider(latest, so_far, len(rules))
            if early is not None:
                return RuleResult(rule_name=name, passed=early, sub_results=tuple(so_far))
        final = self._decider(so_far[-1], so_far, len(rules))
        passed = final if final is not None else self._vacuous_result
        return RuleResult(rule_name=name, passed=passed, sub_results=tuple(so_far))


class ShortCircuitEvaluator(Generic[TContext]):
    """Stops at the first sub-rule whose own ``passed`` equals ``stop_on`` —
    the shape :class:`AndRule` and :class:`OrRule` both are.

    Wraps :class:`SequentialEvaluator` internally; callers never need to
    know that type exists unless they need more than this covers.
    """

    def __init__(self, stop_on: bool) -> None:
        """Initialise with the ``passed`` value that ends evaluation early.

        Only takes ``stop_on`` — unlike :class:`SequentialEvaluator`, where
        ``vacuous_result`` is a genuinely independent fact, here the
        exhaustion case already resolves to ``not stop_on``, and an empty
        list is just that same exhaustion with zero iterations. A second,
        independently-set argument could only ever restate that fact
        correctly or contradict it — never add real information — so it's
        derived, not accepted. Named ``stop_on`` rather than ``passed``:
        ``passed`` would read as a verdict already decided before
        evaluation ever runs; ``stop_on`` reads as what it is, a stopping
        condition.

        Args:
            stop_on: The sub-result ``passed`` value that ends evaluation
                immediately with that same verdict — ``False`` for
                :class:`AndRule` (stop on the first failure), ``True`` for
                :class:`OrRule` (stop on the first pass).
        """
        def decider(latest: RuleResult, so_far: Sequence[RuleResult], total: int) -> bool | None:
            if latest.passed == stop_on:
                return stop_on
            if len(so_far) == total:
                return not stop_on
            return None

        self._inner: SequentialEvaluator[TContext] = SequentialEvaluator(
            decider=decider, vacuous_result=not stop_on
        )

    async def evaluate(
        self, name: str, rules: Sequence[Rule[TContext]], context: TContext
    ) -> RuleResult:
        """See :meth:`SequentialEvaluator.evaluate` — forwards unchanged."""
        return await self._inner.evaluate(name, rules, context)


class AndRule(Generic[TContext]):
    """Composite rule that passes only if every sub-rule passes.

    Short-circuits on the first failing sub-rule. Composes a single,
    shared :class:`ShortCircuitEvaluator` — construction syntax,
    ``__repr__``, and type identity are unchanged from before this class
    held an evaluator instead of a hand-rolled loop.

    Every sub-rule must share the same ``TContext``.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    VACUOUS_RESULT = True
    """Pinned fact: ``AndRule([]).evaluate(...)`` always passes — not wired
    into construction (:class:`ShortCircuitEvaluator` derives this
    internally from ``stop_on``), kept here as a directly-readable
    guarantee."""

    _evaluator: ShortCircuitEvaluator[TContext] = ShortCircuitEvaluator(stop_on=False)

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one fails (or all
                pass). An empty list vacuously passes.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate sub-rules in order, stopping at the first failure.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite rule
            itself — ``passed`` is ``True`` only if every sub-rule
            evaluated passed; ``sub_results`` carries every per-sub-rule
            result gathered before stopping.
        """
        return await self._evaluator.evaluate(self.name, self._rules, context)

    def __repr__(self) -> str:
        suffix = f" ({self.group})" if self.group else ""
        return f'AndRule "{self.name}"{suffix} — {len(self._rules)} sub-rule(s)'

    @staticmethod
    def failed(result: RuleResult) -> RuleResult | None:
        """The sole sub-result that decided a failed ``AndRule``, or
        ``None`` if ``result`` passed (or didn't come from an ``AndRule``
        at all).

        Because :class:`ShortCircuitEvaluator` stops the instant the
        outcome is decided, the last entry in ``sub_results`` is always
        the one that decided it — for a failed ``AndRule``, the sole
        failure (everything before it passed). Reads only
        ``result.sub_results``, so nesting composes for free:
        ``AndRule.failed(result).failing_leaves`` drills further when the
        decisive sub-result is itself a composite. No guard against
        passing an ``OrRule``'s result in — that mistake is visible at the
        call site, unlike the duck-typed ``data`` bug this design replaces.
        """
        if result.sub_results and not result.sub_results[-1].passed:
            return result.sub_results[-1]
        return None

    @staticmethod
    def passing(result: RuleResult) -> Sequence[RuleResult]:
        """Every sub-result that passed on the way to ``result``'s own
        outcome — all of ``sub_results`` if the ``AndRule`` passed, all but
        the last (the failure returned by :meth:`failed`) if it didn't."""
        failed = AndRule.failed(result)
        return result.sub_results if failed is None else result.sub_results[:-1]


class OrRule(Generic[TContext]):
    """Composite rule that passes if any sub-rule passes.

    Short-circuits on the first passing sub-rule. The same
    same-``TContext`` requirement across sub-rules applies here too.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    VACUOUS_RESULT = False
    """Pinned fact: ``OrRule([]).evaluate(...)`` always fails — not wired
    into construction, kept here as a directly-readable guarantee. See
    :attr:`AndRule.VACUOUS_RESULT`."""

    _evaluator: ShortCircuitEvaluator[TContext] = ShortCircuitEvaluator(stop_on=True)

    def __init__(self, name: str, rules: list[Rule[TContext]], group: str | None = None) -> None:
        """Initialise with the ordered sub-rules to combine.

        Args:
            name: Unique identifier for this composite rule.
            rules: Sub-rules evaluated in order until one passes (or all
                fail). An empty list vacuously fails.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rules = rules

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate sub-rules in order, stopping at the first pass.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite rule
            itself — ``passed`` is ``True`` as soon as any sub-rule passes;
            ``sub_results`` carries every per-sub-rule result gathered
            before stopping.
        """
        return await self._evaluator.evaluate(self.name, self._rules, context)

    def __repr__(self) -> str:
        suffix = f" ({self.group})" if self.group else ""
        return f'OrRule "{self.name}"{suffix} — {len(self._rules)} sub-rule(s)'

    @staticmethod
    def passed(result: RuleResult) -> RuleResult | None:
        """The sole sub-result that decided a passed ``OrRule``, or
        ``None`` if ``result`` failed (or didn't come from an ``OrRule`` at
        all).

        Mirrors :meth:`AndRule.failed` with the opposite polarity — the
        last entry in ``sub_results`` is always the one that decided it,
        here the sole pass (everything before it failed)."""
        if result.sub_results and result.sub_results[-1].passed:
            return result.sub_results[-1]
        return None

    @staticmethod
    def failing(result: RuleResult) -> Sequence[RuleResult]:
        """Every sub-result that failed before ``result``'s own outcome was
        decided — all of ``sub_results`` if the ``OrRule`` failed, all but
        the last (the pass returned by :meth:`passed`) if it didn't."""
        passed = OrRule.passed(result)
        return result.sub_results if passed is None else result.sub_results[:-1]


class NotRule(Generic[TContext]):
    """Composite rule that passes exactly when its one wrapped rule fails.

    No :class:`SequentialEvaluator`/:class:`ShortCircuitEvaluator` composed
    in — one child, no sequence to iterate, so that machinery would be
    indirection for nothing it uses.

    Attributes:
        name: See :class:`Rule`.
        group: See :class:`Rule`.
    """

    def __init__(self, name: str, rule: Rule[TContext], group: str | None = None) -> None:
        """Initialise with the single rule to negate.

        Args:
            name: Unique identifier for this composite rule.
            rule: The rule whose outcome is inverted.
            group: Optional group label, see :class:`Rule`.
        """
        self.name = name
        self.group = group
        self._rule = rule

    async def evaluate(self, context: TContext) -> RuleResult:
        """Evaluate the wrapped rule and invert its verdict.

        Args:
            context: See :meth:`Rule.evaluate`.

        Returns:
            A :class:`~verdict.result.RuleResult` for this composite rule
            itself — ``passed`` is the logical negation of the wrapped
            rule's own ``passed``. ``sub_results`` is the one-element
            ``(inner,)``, truthfully — never flattened away. An empty
            ``sub_results`` has to mean *only* "this is a leaf," never also
            "this is a composite hiding its own structure" — that
            guarantee is what makes ``Leaves``/``FailingLeaves`` safe to
            call on any ``RuleResult`` at all, so ``NotRule`` doesn't get
            to special-case it away just because a failed ``NotRule``'s own
            ``failing_leaves`` can otherwise read as misleadingly empty
            (the cause is a pass, not a failure) — :meth:`negated` is the
            dedicated accessor that answers "why," the same role
            :meth:`AndRule.failed`/:meth:`OrRule.passed` play for their own
            families.
        """
        inner = await self._rule.evaluate(context)
        return RuleResult(rule_name=self.name, passed=not inner.passed, sub_results=(inner,))

    def __repr__(self) -> str:
        suffix = f" ({self.group})" if self.group else ""
        return f'NotRule "{self.name}"{suffix}'

    @staticmethod
    def negated(result: RuleResult) -> RuleResult:
        """The one inner result this ``NotRule`` negated to produce
        ``result``.

        Non-nullable, unlike :meth:`AndRule.failed`/:meth:`OrRule.passed` —
        fixed arity means the inner result is never absent.
        """
        return result.sub_results[0]
