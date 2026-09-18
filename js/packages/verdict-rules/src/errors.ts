/**
 * Thrown when a lookup names a rule or group that does not exist.
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
