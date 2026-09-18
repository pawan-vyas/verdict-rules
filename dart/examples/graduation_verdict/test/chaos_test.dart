/// Chaos/differential testing: compares `buildGraduationCheck` against
/// `expectedGraduates` across a wide, randomly-generated space of curricula
/// and students.
///
/// Every case is generated from a seeded `Random`, never a shared or
/// ambient instance, so a disagreement is exactly reproducible from its
/// seed.
///
/// See docs/testing.md for the full design.
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
        // Each case is seeded from chaosSeed + caseIndex, not a single
        // shared generator advanced across all cases, so a failing case
        // reproduces on its own via Random(chaosSeed + caseIndex) through
        // generateCase.
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
