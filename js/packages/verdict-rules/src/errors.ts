/**
 * Thrown when a lookup names a rule or group that does not exist.
 *
 * Every other SDK has a built-in type for this — Python raises `KeyError`,
 * C# `KeyNotFoundException`, Dart `ArgumentError`. JavaScript has no
 * equivalent, and a bare `Error` would leave callers matching on message text,
 * which breaks the moment a message is reworded. So this is exported instead:
 * `instanceof` is stable, and {@link UnknownLookupError.kind} and
 * {@link UnknownLookupError.key} say what was missing without parsing prose.
 *
 * ```ts
 * try {
 *   await engine.runGroup("cor", ctx);
 * } catch (err) {
 *   if (err instanceof UnknownLookupError && err.kind === "group") {
 *     // a typo, not a failed evaluation
 *   }
 * }
 * ```
 *
 * Reaching for this in a `catch` is usually a sign the check belongs earlier:
 * `RulesEngine.ruleNames` and `groupNames` let a caller ask before calling.
 */
export class UnknownLookupError extends Error {
  /** Whether the missing thing was a rule name or a group label. */
  readonly kind: "rule" | "group";

  /** The name or label that matched nothing. */
  readonly key: string;

  constructor(kind: "rule" | "group", key: string) {
    super(
      kind === "rule"
        ? `No rule named '${key}' in this engine`
        : `No rules in group '${key}' in this engine`,
    );
    this.name = "UnknownLookupError";
    this.kind = kind;
    this.key = key;
  }
}
