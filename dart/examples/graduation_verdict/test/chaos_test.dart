/// Chaos/differential testing: does the engine ever disagree with an independent oracle?
///
/// graduation_verdict_test.dart proves 8 hand-picked, hand-verified
/// scenarios come out right. That doesn't say anything about the enormous
/// space of curricula and students nobody hand-picked. This file checks a
/// much wider space by comparing two independent implementations against
/// each other instead of against a fixed expected value:
///
/// - `buildGraduationCheck` -- the real, verdict_rules-based implementation
///   this whole project exists to demonstrate.
/// - `expectedGraduates` -- a plain, verdict_rules-free re-implementation,
///   deliberately dumb so it's trustworthy by inspection.
///
/// If they ever disagree, one of them is wrong -- and because every case
/// is generated from a *seeded* `Random` (never a shared or ambient
/// instance), that disagreement is exactly reproducible: the same seed
/// always regenerates the exact same case. "The chaos suite didn't cause
/// any breakdown" is therefore a real, re-checkable claim across runs, not
/// a one-off observation about whatever numbers came up this time.
///
/// See docs/testing.md for the full design reasoning.
import 'dart:math';

import 'package:graduation_verdict/graduation_verdict.dart';
import 'package:test/test.dart';

// Pinned. Do not change without a deliberate reason -- bumping this
// reshuffles every case's data, silently trading which edge cases get
// covered for which. If you need *more* coverage, raise numCases instead;
// that's strictly additive; changing chaosSeed is not.
const _chaosSeed = 20260907;
const _numCases = 500;

void main() {
  group('engine agrees with an independent oracle', () {
    for (var caseIndex = 0; caseIndex < _numCases; caseIndex++) {
      test('case $caseIndex', () async {
        // Each case is seeded from chaosSeed + caseIndex -- not from a
        // single shared generator advanced across all cases --
        // specifically so any one failing case reproduces on its own:
        // re-run Random(chaosSeed + caseIndex) through generateCase and
        // you get the exact same policies/context/electiveMinimum that
        // failed, with no need to replay every earlier case first.
        final rng = Random(_chaosSeed + caseIndex);
        final (policies, context, electiveMinimum) = generateCase(rng);

        final (_, graduates) = buildGraduationCheck(policies, electiveMinimum);
        final actual = (await graduates.evaluate(context)).passed;
        final expected = expectedGraduates(policies, context, electiveMinimum);

        expect(
          actual,
          expected,
          reason: 'caseIndex=$caseIndex (seed=${_chaosSeed + caseIndex}): '
              'engine said $actual, oracle said $expected\n'
              'electiveMinimum=$electiveMinimum',
        );
      });
    }
  });
}
