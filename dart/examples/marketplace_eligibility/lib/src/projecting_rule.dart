import 'package:verdict_rules/verdict_rules.dart';

/// Adapts a `Rule<TInner>` to run inside a composite built on `TOuter`.
///
/// Not part of verdict_rules itself -- a consumer-defined adapter, exactly
/// as free to exist as a new rule shape is, with no changes needed on
/// verdict_rules' side to support it. See
/// docs/extending/reusing-a-rule-across-contexts/dart.md for the standalone
/// version of this same pattern.
class ProjectingRule<TOuter, TInner> implements Rule<TOuter> {
  @override
  final String name;

  @override
  final String? group;

  final Rule<TInner> _inner;
  final TInner Function(TOuter) _project;

  ProjectingRule(Rule<TInner> inner, TInner Function(TOuter) project)
      : name = inner.name,
        group = inner.group,
        _inner = inner,
        _project = project;

  @override
  Future<RuleResult> evaluate(TOuter context) =>
      _inner.evaluate(_project(context));
}
