/// Fuzzes `curriculumFromJson`/`loadCurriculum` -- the readers that turn
/// policies.json's own JSON shape into typed `SubjectPolicy` objects --
/// with deliberately malformed input.
///
/// The only acceptable outcomes are: (a) a successful parse into a valid
/// policy list, or (b) the one error type these readers' own `as` casts
/// and `jsonDecode` already document for malformed input -- never an
/// unrelated crash. See docs/testing.md.
import 'dart:convert';
import 'dart:io';
import 'dart:math';

import 'package:graduation_verdict/graduation_verdict.dart';
import 'package:test/test.dart';

// Independent of chaosSeed/CHAOS_SEED on purpose -- this suite samples a
// different space (malformed JSON shapes, not valid randomized curricula)
// and shouldn't be coupled to the chaos suite's own pinned sequence.
const _fuzzSeed = 20260907;
const _numStructuralCases = 150;
const _numTruncationCases = 30;

void main() {
  group('curriculumFromJson rejects malformed shapes safely', () {
    final mutatorNames = curriculumMutators.keys.toList();

    for (var i = 0; i < _numStructuralCases; i++) {
      test('fuzz case $i', () {
        final rng = Random(_fuzzSeed + i);
        final mutatorName = mutatorNames[rng.nextInt(mutatorNames.length)];
        final mutate = curriculumMutators[mutatorName]!;
        // Round-tripped through JSON so a mutated value has exactly the
        // shape `jsonDecode` would actually hand the reader from a real
        // malformed file, not whatever Dart object the mutator built.
        final mutated = jsonDecode(jsonEncode(mutate(rng, validCurriculum())))
            as Map<String, Object?>;

        try {
          final (policies, electiveMinimum) = curriculumFromJson(mutated);
          // Outcome (a): a successful parse. Expected from
          // `empty_subjects_array` and `extra_unexpected_key`, and
          // possible from others depending on which field a mutator
          // happened to touch.
          expect(policies, isA<List<SubjectPolicy>>());
          expect(electiveMinimum, isA<int>());
        } on TypeError catch (_) {
          // Outcome (b): the documented error curriculumFromJson's own
          // `as` casts raise for a field of the wrong shape or a missing
          // required key.
        } catch (e) {
          fail('mutator=$mutatorName seed=${_fuzzSeed + i}: unrelated crash '
              '${e.runtimeType}: $e');
        }
      });
    }
  });

  group('loadCurriculum rejects a truncated JSON document safely', () {
    for (var i = 0; i < _numTruncationCases; i++) {
      test('truncated case $i', () {
        final rng = Random(_fuzzSeed + 10000 + i);
        final text = truncatedCurriculumJson(rng, validCurriculum());
        final tempFile = File(
            '${Directory.systemTemp.path}/verdict_rules_fuzz_truncated_$i.json')
          ..writeAsStringSync(text);
        addTearDown(() {
          if (tempFile.existsSync()) tempFile.deleteSync();
        });

        try {
          loadCurriculum(tempFile.path);
          // Outcome (a): this particular cut point happened to still
          // leave a syntactically complete JSON document (e.g. trailing
          // whitespace was all that got trimmed).
        } on FormatException catch (_) {
          // Outcome (b): jsonDecode's own documented error for invalid
          // JSON text.
        } on TypeError catch (_) {
          // Outcome (b): a cut that still parses as valid JSON but no
          // longer has the expected shape (e.g. truncated to `{}`).
        } catch (e) {
          fail('seed=${_fuzzSeed + 10000 + i}: unrelated crash '
              '${e.runtimeType}: $e');
        }
      });
    }
  });
}
