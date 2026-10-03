/// Deliberately malformed variants of the curriculum/policy JSON shape,
/// for fuzzing `curriculumFromJson`/`loadCurriculum`.
///
/// Every mutator here takes a `Random` instance explicitly, never a shared
/// or ambient generator, same discipline as chaos_data.dart -- a mutant
/// built from a given seed is exactly reproducible.
library;

import 'dart:convert';
import 'dart:math';

/// One subject of each `subjectType`, enough to exercise every per-type
/// field (`practical_min_pct`, `exemption_allowed`) at least once before
/// a mutator corrupts it.
Map<String, Object?> validCurriculum() => {
      'elective_minimum': 1,
      'subjects': [
        {
          'subject_id': 'MATH101',
          'subject_type': 'academic',
          'written_min_pct': 40.0,
        },
        {
          'subject_id': 'WORKSHOP201',
          'subject_type': 'vocational',
          'written_min_pct': 40.0,
          'practical_min_pct': 50.0,
        },
        {
          'subject_id': 'FRENCH101',
          'subject_type': 'language',
          'written_min_pct': 40.0,
          'exemption_allowed': true,
          'is_elective': true,
        },
      ],
    };

/// Deep-copies a JSON-shaped value (nested Map/List of JSON-safe
/// primitives) so mutating one case never corrupts a shared baseline
/// another fuzz iteration still needs.
Object? deepCopyJson(Object? value) {
  if (value is Map<String, Object?>) {
    return value.map((key, v) => MapEntry(key, deepCopyJson(v)));
  }
  if (value is List) {
    return value.map(deepCopyJson).toList();
  }
  return value;
}

Map<String, Object?> _asCurriculum(Object? value) =>
    value as Map<String, Object?>;

List<Object?> _subjectsOf(Map<String, Object?> curriculum) =>
    curriculum['subjects'] as List<Object?>;

Map<String, Object?> _wrongTypeField(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  final subjects = _subjectsOf(mutated);
  final row = subjects[rng.nextInt(subjects.length)] as Map<String, Object?>;
  // A string where a number is expected, a number where a bool is
  // expected, a string where a bool is expected.
  final replacementByField = <String, Object?>{
    'written_min_pct': 'not-a-number',
    'exemption_allowed': 42,
    'is_elective': 'yes',
  };
  final field =
      replacementByField.keys.elementAt(rng.nextInt(replacementByField.length));
  row[field] = replacementByField[field];
  return mutated;
}

Map<String, Object?> _droppedRequiredKey(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  if (rng.nextBool()) {
    mutated.remove('elective_minimum');
    return mutated;
  }
  final subjects = _subjectsOf(mutated);
  final row = subjects[rng.nextInt(subjects.length)] as Map<String, Object?>;
  const requiredKeys = ['subject_id', 'subject_type', 'written_min_pct'];
  row.remove(requiredKeys[rng.nextInt(requiredKeys.length)]);
  return mutated;
}

Map<String, Object?> _extraUnexpectedKey(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  if (rng.nextBool()) {
    mutated['unexpected_top_level_field'] = 'surprise';
    return mutated;
  }
  final subjects = _subjectsOf(mutated);
  final row = subjects[rng.nextInt(subjects.length)] as Map<String, Object?>;
  row['unexpected_subject_field'] = 'surprise';
  return mutated;
}

Map<String, Object?> _nestedJunkScalar(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  final subjects = _subjectsOf(mutated);
  final row = subjects[rng.nextInt(subjects.length)] as Map<String, Object?>;
  row['written_min_pct'] = {
    'deeply': {
      'nested': [
        1,
        2,
        {'junk': true},
      ],
    },
  };
  return mutated;
}

Map<String, Object?> _emptySubjectsArray(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  mutated['subjects'] = <Object?>[];
  return mutated;
}

Map<String, Object?> _nullInPlaceOfObject(
    Random rng, Map<String, Object?> curriculum) {
  final mutated = _asCurriculum(deepCopyJson(curriculum));
  if (rng.nextBool()) {
    mutated['subjects'] = null;
    return mutated;
  }
  final subjects = _subjectsOf(mutated);
  subjects[rng.nextInt(subjects.length)] = null;
  return mutated;
}

/// One malformation strategy per entry, keyed by a readable name so a
/// failing fuzz case's message names exactly which kind of malformation
/// triggered it -- adding a new malformation kind is a new entry here,
/// never a new branch in whatever drives the fuzz loop.
final Map<String, Map<String, Object?> Function(Random, Map<String, Object?>)>
    curriculumMutators = {
  'wrong_type_field': _wrongTypeField,
  'dropped_required_key': _droppedRequiredKey,
  'extra_unexpected_key': _extraUnexpectedKey,
  'nested_junk_scalar': _nestedJunkScalar,
  'empty_subjects_array': _emptySubjectsArray,
  'null_in_place_of_object': _nullInPlaceOfObject,
};

/// A syntactically truncated/incomplete JSON document -- cut at a random
/// point strictly before the end, so it never happens to still be the
/// complete, valid document. Exercises `loadCurriculum`'s own `jsonDecode`
/// call rather than `curriculumFromJson`, which only ever sees an
/// already-parsed [Map].
String truncatedCurriculumJson(Random rng, Map<String, Object?> curriculum) {
  final text = jsonEncode(curriculum);
  final cutPoint = 1 + rng.nextInt(text.length - 1);
  return text.substring(0, cutPoint);
}
