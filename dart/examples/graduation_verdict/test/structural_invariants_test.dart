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
  final data = result.data;
  expect(data, isA<List<RuleResult>>(),
      reason: '${result.ruleName}: an AndRule result.data must list its '
          'sub-results');
  final subResults = data as List<RuleResult>;
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
  final data = result.data;
  expect(data, isA<List<RuleResult>>(),
      reason: '${result.ruleName}: an OrRule result.data must list its '
          'sub-results');
  final subResults = data as List<RuleResult>;
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

void _checkLeafInvariant(RuleResult result) {
  expect(result.data, isNot(isA<List<RuleResult>>()),
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
  final topData = result.data as List<RuleResult>;

  final corePolicies = policies.where((p) => !p.isElective).toList();
  final electivePolicies = policies.where((p) => p.isElective).toList();

  if (topData.isNotEmpty) {
    final coreResult = topData[0];
    _checkAndRuleInvariant(coreResult);
    final coreData = coreResult.data as List<RuleResult>;
    for (var i = 0; i < coreData.length; i++) {
      _checkSubjectInvariant(corePolicies[i], coreData[i]);
    }
  }

  if (topData.length > 1) {
    final electiveResult = topData[1];
    final electiveData = electiveResult.data;
    expect(electiveData, isA<List<RuleResult>>(),
        reason: 'elective_requirement result.data must list every elective '
            "subject's own result");
    final electiveSubResults = electiveData as List<RuleResult>;
    // AtLeastNRule evaluates every sub-rule unconditionally -- no
    // short-circuit at all, unlike AndRule/OrRule above.
    expect(electiveSubResults.length, electivePolicies.length,
        reason: 'elective_requirement must evaluate every elective subject '
            'regardless of pass/fail');
    for (var i = 0; i < electiveSubResults.length; i++) {
      _checkSubjectInvariant(electivePolicies[i], electiveSubResults[i]);
    }
  }
  // topData[2]/topData[3] (cgpa_met/attendance_met), when reached, are
  // FunctionRule leaves -- nothing further to walk.

  // Structural count, not a verdict: runAll reports exactly one result
  // per registered rule, since it never short-circuits.
  final runAll = await engine.runAll(context);
  expect(runAll.results.length, policies.length,
      reason: 'runAll must report exactly one result per registered rule');
}

/// Deep-compares two result trees field by field -- `RuleResult` has no
/// `==` override, so comparing top-level `passed` alone would miss a
/// divergence buried in `data`.
bool _resultTreesIdentical(RuleResult a, RuleResult b) {
  if (a.ruleName != b.ruleName ||
      a.passed != b.passed ||
      a.detail != b.detail) {
    return false;
  }
  final aData = a.data;
  final bData = b.data;
  if (aData is List<RuleResult> || bData is List<RuleResult>) {
    if (aData is! List<RuleResult> || bData is! List<RuleResult>) return false;
    if (aData.length != bData.length) return false;
    for (var i = 0; i < aData.length; i++) {
      if (!_resultTreesIdentical(aData[i], bData[i])) return false;
    }
    return true;
  }
  return aData == bData;
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
