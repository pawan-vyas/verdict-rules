import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

import 'test_helpers.dart';

/// `toString()` coverage for every rule and engine type -- the debugger/log
/// line each now carries, matching the same text other verdict SDKs show for
/// the same shape (see the C# implementation of this issue for the shared
/// format).
void main() {
  test('FunctionRule shows its name', () {
    final rule = pass('over_18');
    expect(rule.toString(), 'FunctionRule "over_18"');
  });

  test('FunctionRule shows its group when present', () {
    final rule = pass('over_18', group: 'age');
    expect(rule.toString(), 'FunctionRule "over_18" (age)');
  });

  test('AndRule shows its name and sub-rule count', () {
    final rule = AndRule<Context>('all', [pass('a'), pass('b')]);
    expect(rule.toString(), 'AndRule "all" — 2 sub-rule(s)');
  });

  test('AndRule shows its group when present', () {
    final rule = AndRule<Context>('all', [pass('a')], group: 'checkout');
    expect(rule.toString(), 'AndRule "all" (checkout) — 1 sub-rule(s)');
  });

  test('an empty AndRule shows zero sub-rules', () {
    final rule = AndRule<Context>('all', const []);
    expect(rule.toString(), 'AndRule "all" — 0 sub-rule(s)');
  });

  test('OrRule shows its name and sub-rule count', () {
    final rule = OrRule<Context>('any', [failing('a'), pass('b')]);
    expect(rule.toString(), 'OrRule "any" — 2 sub-rule(s)');
  });

  test('OrRule shows its group when present', () {
    final rule = OrRule<Context>('any', [pass('a')], group: 'checkout');
    expect(rule.toString(), 'OrRule "any" (checkout) — 1 sub-rule(s)');
  });

  test('RulesEngine shows its rule and group counts', () {
    final engine = RulesEngine<Context>([
      pass('a', group: 'g1'),
      pass('b', group: 'g2'),
      pass('c'),
    ]);
    expect(engine.toString(), 'RulesEngine — 3 rule(s), 2 group(s)');
  });

  test('an empty RulesEngine shows zero rules and groups', () {
    final engine = RulesEngine<Context>(const []);
    expect(engine.toString(), 'RulesEngine — 0 rule(s), 0 group(s)');
  });
}
