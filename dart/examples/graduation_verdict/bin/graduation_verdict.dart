import 'dart:io';

import 'package:graduation_verdict/graduation_verdict.dart';

Future<void> main() async {
  // Fixture data lives at the repo root, shared by every language's own port
  // of this example -- see fixtures/graduation_verdict/README.md for the
  // contract.
  final scriptDir = File(Platform.script.toFilePath()).parent.path;
  final fixtures =
      Directory('$scriptDir/../../../../fixtures/graduation_verdict')
          .absolute
          .path;

  final (policies, electiveMinimum) = loadCurriculum('$fixtures/policies.json');
  final students = loadStudents('$fixtures/students.json');
  final (engine, graduates) = buildGraduationCheck(policies, electiveMinimum);

  print('=== Graduation Requirement Verdict -- Demo ===');
  print('Built once from policies.json: ${policies.length} subjects\n');

  final alice = students['alice']!.context;
  print("--- Detailed lookup for 'alice' ---");
  final named = await engine.runNamed('WORKSHOP201', alice);
  print("runNamed('WORKSHOP201'): ${named.passed ? 'PASS' : 'FAIL'}");
  final core = await engine.runGroup('core', alice);
  print(
      "runGroup('core'):     ${core.results.map((r) => '${r.ruleName}=${r.passed ? 'PASS' : 'FAIL'}').join(', ')}");
  final elective = await engine.runGroup('elective', alice);
  print(
      "runGroup('elective'): ${elective.results.map((r) => '${r.ruleName}=${r.passed ? 'PASS' : 'FAIL'}').join(', ')}");
  final full = await engine.runAll(alice);
  print('runAll(): ${full.results.length} subjects reported\n');

  print(
      '--- Batch verdict across all students (same built engine, no rebuild) ---');
  for (final entry in students.entries) {
    final verdict = await graduates.evaluate(entry.value.context);
    final status = verdict.passed ? 'GRADUATES' : 'DOES NOT GRADUATE';
    final reason = verdict.passed ? '' : '  (${verdict.detail})';
    print('${entry.key.padRight(10)}: $status$reason');
  }
}
