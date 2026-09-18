import { FunctionRule } from "../dist/index.js";

/** A rule that always passes, mirroring Python's own `_pass` test helper. */
export function pass(name, group, data) {
  return new FunctionRule(
    name,
    async () => ({ ruleName: name, passed: true, ...(data !== undefined ? { data } : {}) }),
    group,
  );
}

/** A rule that always fails, mirroring Python's own `_fail` test helper. */
export function fail(name, group, detail) {
  return new FunctionRule(
    name,
    async () => ({ ruleName: name, passed: false, ...(detail !== undefined ? { detail } : {}) }),
    group,
  );
}

/**
 * A rule that records every evaluation, so short-circuiting can be proven by
 * what actually ran rather than by the final boolean alone. A port that
 * evaluated concurrently would return the same boolean and fail only here.
 */
export function counting(name, passes, log, group) {
  return new FunctionRule(
    name,
    async () => {
      log.push(name);
      return { ruleName: name, passed: passes };
    },
    group,
  );
}
