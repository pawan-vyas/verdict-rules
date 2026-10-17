/// Structural invariants on the result *tree*, not just the final boolean
/// verdict -- complements chaos_test.dart's differential check, which only
/// ever compares `passed`.
///
/// Walks the exact shape `buildGraduationCheck` documents its own
/// docstring as building: `AndRule("graduates", [AndRule(core),
/// AtLeastNRule(elective), FunctionRule(cgpa), FunctionRule(attendance)])`,
/// where each per-subject rule is itself whatever `ruleForSubject` built
/// for that subject's `subjectType`. See docs/testing.md.
import 'dart:math';

import 'package:graduation_verdict/graduation_verdict.dart';
import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

// Mirrors chaos_test.dart's own pinned seed and case count exactly, so
// this suite walks the identical generated cases rather than a second,
// drifting sample. See chaos_test.dart for why this is pinned.
const _chaosSeed = 20260907;
const _numCases = 500;

void _checkAndRuleInvariant(RuleResult result) {
  final subResults = result.subResults;
  if (subResults.isEmpty) {
    // Vacuous truth: AndRule([]) always passes with no sub-results.
    expect(result.passed, isTrue,
        reason: '${result.ruleName}: an empty AndRule must pass vacuously');
    return;
  }
  if (result.passed) {
    expect(subResults.every((r) => r.passed), isTrue,
        reason: '${result.ruleName}: passed but a sub-result was skipped or '
            'failed');
  } else {
    expect(subResults.sublist(0, subResults.length - 1).every((r) => r.passed),
        isTrue,
        reason: '${result.ruleName}: failed somewhere before its own last '
            'evaluated sub-result');
    expect(subResults.last.passed, isFalse,
        reason: '${result.ruleName}: failed, but the sub-result it stopped '
            'on actually passed');
  }
}

void _checkOrRuleInvariant(RuleResult result) {
  final subResults = result.subResults;
  if (subResults.isEmpty) {
    // Vacuous truth: OrRule([]) always fails with no sub-results.
    expect(result.passed, isFalse,
        reason: '${result.ruleName}: an empty OrRule must fail vacuously');
    return;
  }
  if (result.passed) {
    expect(subResults.sublist(0, subResults.length - 1).every((r) => !r.passed),
        isTrue,
        reason: '${result.ruleName}: passed after an earlier sub-result had '
            'already passed');
    expect(subResults.last.passed, isTrue,
        reason: '${result.ruleName}: passed, but the sub-result it stopped '
            'on did not actually pass');
  } else {
    expect(subResults.every((r) => !r.passed), isTrue,
        reason: '${result.ruleName}: failed, but some sub-result actually '
            'passed -- OrRule only stops on a pass, so a full failure means '
            'every one of them really ran');
  }
}

/// AtLeastNRule stops exactly when its own [minimum] is mathematically
/// decided -- as soon as enough sub-rules have passed to guarantee it, or
/// too many have failed for it to still be reachable -- never one sub-rule
/// earlier (the call wouldn't have been decidable yet) and never one later
/// (an already-decided rule kept evaluating).
///
/// `result.subResults`' length *is* the number of sub-rules actually
/// called -- read back from the result tree `buildGraduationCheck` already
/// produced for this chaos case, the same signal a dedicated `calls` list
/// would give a call-counter test. [total] is the full elective sub-rule
/// count (not reachable from `result` alone once short-circuited), and
/// [minimum] is `electiveMinimum` -- both already in scope at every call
/// site below, so neither needs reading off the private `AtLeastNRule`
/// instance itself.
void _checkAtLeastNRuleInvariant(RuleResult result, int total, int minimum) {
  final subResults = result.subResults;

  var passedSoFar = 0;
  for (var index = 0; index < subResults.length; index++) {
    if (subResults[index].passed) passedSoFar++;
    final remainingAfter = total - (index + 1);
    final decidedTrue = passedSoFar >= minimum;
    final decidedFalse = passedSoFar + remainingAfter < minimum;
    final isLastEvaluated = index == subResults.length - 1;

    if (isLastEvaluated) {
      expect(decidedTrue || decidedFalse, isTrue,
          reason: '${result.ruleName}: AtLeastNRule stopped after '
              '${subResults.length} of $total sub-rule(s) before its minimum '
              '($minimum) was mathematically decided either way');
    } else {
      expect(decidedTrue || decidedFalse, isFalse,
          reason: '${result.ruleName}: AtLeastNRule kept evaluating past '
              'sub-rule $index even though its minimum ($minimum) was '
              'already decided there');
    }
  }

  expect(result.passed, passedSoFar >= minimum,
      reason: "${result.ruleName}: AtLeastNRule's own passed verdict "
          'disagrees with the pass count over the sub-rules it actually '
          'evaluated');
}

void _checkLeafInvariant(RuleResult result) {
  expect(result.subResults, isEmpty,
      reason: '${result.ruleName}: a FunctionRule leaf is never recursed '
          'into');
}

/// Per-rule-type structural invariant, keyed by the built rule's own
/// concrete runtime type -- adding a fifth composite kind to
/// graduation_check.dart is a new entry here, never a new branch anywhere
/// in this file.
final Map<Type, void Function(RuleResult)> _invariantByRuleType = {
  AndRule<Context>: _checkAndRuleInvariant,
  OrRule<Context>: _checkOrRuleInvariant,
  FunctionRule<Context>: _checkLeafInvariant,
};

/// Check the invariant for whichever concrete [Rule] subtype
/// `ruleForSubject` builds for [policy] -- `ruleForSubject` is pure, so
/// calling it again here is just a second, independent instance of the
/// exact rule `buildGraduationCheck` already built; it's used only for its
/// runtime type, since AndRule/OrRule never expose their own sub-rules
/// publicly.
void _checkSubjectInvariant(SubjectPolicy policy, RuleResult subjectResult) {
  final rule = ruleForSubject(policy);
  final checker = _invariantByRuleType[rule.runtimeType];
  if (checker == null) {
    fail('no structural invariant registered for ${rule.runtimeType} '
        '(subject ${policy.subjectId})');
  }
  checker(subjectResult);
}

/// Walk one generated case's full result tree and assert every structural
/// invariant, plus the `runAll` count invariant using the same built
/// engine.
Future<void> _checkStructuralInvariants(List<SubjectPolicy> policies,
    Map<String, Object?> context, int electiveMinimum) async {
  final (engine, graduates) = buildGraduationCheck(policies, electiveMinimum);
  final result = await graduates.evaluate(context);

  // graduates is always AndRule("graduates", [all_core_subjects_pass,
  // elective_requirement, cgpa_met, attendance_met]) -- fixed by
  // buildGraduationCheck's own construction, not data-dependent.
  _checkAndRuleInvariant(result);
  final topSubResults = result.subResults;

  final corePolicies = policies.where((p) => !p.isElective).toList();
  final electivePolicies = policies.where((p) => p.isElective).toList();

  if (topSubResults.isNotEmpty) {
    final coreResult = topSubResults[0];
    _checkAndRuleInvariant(coreResult);
    final coreSubResults = coreResult.subResults;
    for (var i = 0; i < coreSubResults.length; i++) {
      _checkSubjectInvariant(corePolicies[i], coreSubResults[i]);
    }
  }

  if (topSubResults.length > 1) {
    final electiveResult = topSubResults[1];
    final electiveSubResults = electiveResult.subResults;
    // AtLeastNRule stops as soon as electiveMinimum is mathematically
    // decided -- unlike AndRule/OrRule, that isn't simply "the first
    // failure/pass", so the dedicated checker above re-derives exactly
    // when that point was reached instead of assuming either "every
    // sub-rule ran" or "stops at the first decisive result".
    _checkAtLeastNRuleInvariant(
        electiveResult, electivePolicies.length, electiveMinimum);
    for (var i = 0; i < electiveSubResults.length; i++) {
      _checkSubjectInvariant(electivePolicies[i], electiveSubResults[i]);
    }
  }
  // topSubResults[2]/topSubResults[3] (cgpa_met/attendance_met), when
  // reached, are FunctionRule leaves -- nothing further to walk.

  // Structural count, not a verdict: runAll reports exactly one result
  // per registered rule, since it never short-circuits.
  final runAll = await engine.runAll(context);
  expect(runAll.results.length, policies.length,
      reason: 'runAll must report exactly one result per registered rule');
}

/// Deep-compares two result trees field by field -- `RuleResult` has no
/// `==` override, so comparing top-level `passed` alone would miss a
/// divergence buried in `subResults`.
bool _resultTreesIdentical(RuleResult a, RuleResult b) {
  if (a.ruleName != b.ruleName ||
      a.passed != b.passed ||
      a.detail != b.detail) {
    return false;
  }
  if (a.subResults.length != b.subResults.length) return false;
  for (var i = 0; i < a.subResults.length; i++) {
    if (!_resultTreesIdentical(a.subResults[i], b.subResults[i])) return false;
  }
  return a.data == b.data;
}

void main() {
  group('structural invariants hold for every generated case', () {
    for (var caseIndex = 0; caseIndex < _numCases; caseIndex++) {
      test('case $caseIndex', () async {
        final rng = Random(_chaosSeed + caseIndex);
        final (policies, context, electiveMinimum) = generateCase(rng);
        await _checkStructuralInvariants(policies, context, electiveMinimum);
      });
    }
  });

  group('purity: evaluating the same case twice is identical in every field',
      () {
    // A handful of the same 500 generated cases, not a new sample.
    for (var caseIndex = 0; caseIndex < _numCases; caseIndex += 50) {
      test('case $caseIndex evaluated twice matches', () async {
        final rng = Random(_chaosSeed + caseIndex);
        final (policies, context, electiveMinimum) = generateCase(rng);

        final (_, graduatesA) = buildGraduationCheck(policies, electiveMinimum);
        final (_, graduatesB) = buildGraduationCheck(policies, electiveMinimum);
        final resultA = await graduatesA.evaluate(context);
        final resultB = await graduatesB.evaluate(context);

        expect(_resultTreesIdentical(resultA, resultB), isTrue,
            reason: 'case $caseIndex: two evaluations of the identical '
                'inputs produced different result trees');
      });
    }
  });
}
