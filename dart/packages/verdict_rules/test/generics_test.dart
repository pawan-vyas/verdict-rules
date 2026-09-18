import 'package:test/test.dart';
import 'package:verdict_rules/verdict_rules.dart';

/// Tests for Rule's generic context parameter (TContext).
///
/// Unlike Python/JS's erased generics, Dart's generics are checked (though
/// not fully reified the way C#'s are) -- the analyzer enforces
/// `AndRule<TContext>`/`OrRule<TContext>`'s same-context requirement at
/// compile time. This file proves the runtime half of the contract: a
/// typed, non-dict context runs through every primitive -- FunctionRule,
/// AndRule, OrRule, RulesEngine, all three run modes -- exactly the way a
/// dict-context (`Context`) rule always has. Deliberately separate from
/// rule_test.dart/engine_test.dart: those prove the language-agnostic
/// contract 1:1 against every other SDK; dart_idioms_test.dart proves the
/// breaking-migration fact itself; this file proves the new generic
/// mechanics work correctly end to end.
class OrderContext {
  final double total;
  final bool isMember;
  const OrderContext({required this.total, required this.isMember});
}

Future<RuleResult> orderTotalMet(OrderContext context) async =>
    RuleResult(ruleName: 'order_total_met', passed: context.total >= 50);

Future<RuleResult> isMember(OrderContext context) async =>
    RuleResult(ruleName: 'is_member', passed: context.isMember);

void main() {
  group('a typed, non-dict context runs through every primitive', () {
    test('FunctionRule evaluates a typed context', () async {
      final rule = FunctionRule<OrderContext>('order_total_met', orderTotalMet);
      final result =
          await rule.evaluate(const OrderContext(total: 75, isMember: false));
      expect(result.passed, isTrue);
    });

    test('AndRule composes typed sub-rules', () async {
      final rule = AndRule<OrderContext>('eligible', [
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
        FunctionRule<OrderContext>('is_member', isMember),
      ]);
      final result =
          await rule.evaluate(const OrderContext(total: 75, isMember: true));
      expect(result.passed, isTrue);
    });

    test('AndRule short-circuits on a typed context too', () async {
      final log = <String>[];
      final tracked = FunctionRule<OrderContext>('tracked', (ctx) async {
        log.add('tracked');
        return RuleResult(ruleName: 'tracked', passed: true);
      });
      final rule = AndRule<OrderContext>('eligible', [
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
        tracked,
      ]);
      await rule.evaluate(
          const OrderContext(total: 10, isMember: false)); // fails first
      expect(log,
          isEmpty); // 'tracked' never reached -- short-circuiting survives typed contexts
    });

    test('OrRule composes typed sub-rules', () async {
      final rule = OrRule<OrderContext>('eligible', [
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
        FunctionRule<OrderContext>('is_member', isMember),
      ]);
      final result =
          await rule.evaluate(const OrderContext(total: 10, isMember: true));
      expect(result.passed, isTrue);
    });

    test('RulesEngine.runAll evaluates a typed context', () async {
      final engine = RulesEngine<OrderContext>([
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
        FunctionRule<OrderContext>('is_member', isMember),
      ]);
      final result =
          await engine.runAll(const OrderContext(total: 75, isMember: true));
      expect(result.passed, isTrue);
      expect(result.results, hasLength(2));
    });

    test('RulesEngine.runNamed evaluates a typed context', () async {
      final engine = RulesEngine<OrderContext>([
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
      ]);
      final result = await engine.runNamed(
          'order_total_met', const OrderContext(total: 75, isMember: false));
      expect(result.passed, isTrue);
    });

    test('RulesEngine.runGroup evaluates a typed context', () async {
      final engine = RulesEngine<OrderContext>([
        FunctionRule<OrderContext>('order_total_met', orderTotalMet,
            group: 'checkout'),
      ]);
      final result = await engine.runGroup(
          'checkout', const OrderContext(total: 75, isMember: false));
      expect(result.passed, isTrue);
    });

    test('RulesEngine try-prefixed forms work with a typed context', () async {
      final engine = RulesEngine<OrderContext>([
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
      ]);
      final present = await engine.tryRunNamed(
          'order_total_met', const OrderContext(total: 75, isMember: false));
      final absent = await engine.tryRunNamed(
          'nope', const OrderContext(total: 75, isMember: false));
      expect(present, isNotNull);
      expect(present!.passed, isTrue);
      expect(absent, isNull);
    });

    test('RulesEngine unknown name raises ArgumentError', () async {
      final engine = RulesEngine<OrderContext>([
        FunctionRule<OrderContext>('order_total_met', orderTotalMet),
      ]);
      expect(
        () => engine.runNamed(
            'missing', const OrderContext(total: 0, isMember: false)),
        throwsArgumentError,
      );
    });
  });
}
