// A runnable example. pub.dev looks for `example/` and surfaces it on the
// package page, so this doubles as the first thing a reader sees.
import 'package:verdict_rules/verdict_rules.dart';

/// Builds a rule asserting that a numeric field clears a floor.
FunctionRule atLeast(String name, String field, num floor) => FunctionRule(
      name,
      (ctx) async {
        final value = ctx[field]! as num;
        return RuleResult(
          ruleName: name,
          passed: value >= floor,
          detail: '$value vs $floor',
        );
      },
    );

Future<void> main() async {
  final eligible = AndRule('eligible', [
    atLeast('age_ok', 'age', 18),
    atLeast('score_ok', 'score', 60),
  ]);

  // Short-circuits: score_ok is never reached when age_ok fails.
  final tooYoung = await eligible.evaluate({'age': 16, 'score': 90});
  print('${tooYoung.passed} — ${tooYoung.detail}');
  print('rules actually evaluated: ${(tooYoung.data! as List).length}\n');

  final failedScore = await eligible.evaluate({'age': 21, 'score': 55});
  print('${failedScore.passed} — ${failedScore.detail}');
  print('rules actually evaluated: ${(failedScore.data! as List).length}\n');

  // The engine is diagnostic: it never short-circuits.
  final engine = RulesEngine([
    atLeast('age_ok', 'age', 18),
    atLeast('score_ok', 'score', 60),
  ]);
  final everything = await engine.runAll({'age': 16, 'score': 55});
  print('runAll reported ${everything.results.length} rules, '
      'passed=${everything.passed}');
}
