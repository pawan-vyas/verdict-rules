/**
 * A small, seeded, deterministic pseudo-random generator. Not cryptographic.
 * Uses the public-domain mulberry32 algorithm (Tommy Ettinger).
 */
export class Rng {
  #state;

  constructor(seed) {
    this.#state = seed >>> 0;
  }

  /** Next float in [0, 1). */
  random() {
    this.#state = (this.#state + 0x6d2b79f5) | 0;
    let t = this.#state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  }

  /** A float uniformly distributed in [min, max). */
  uniform(min, max) {
    return min + this.random() * (max - min);
  }

  /** An integer uniformly distributed in [min, max], inclusive of both ends. */
  randint(min, max) {
    return min + Math.floor(this.random() * (max - min + 1));
  }

  /** One element chosen uniformly from `items`. */
  choice(items) {
    return items[Math.floor(this.random() * items.length)];
  }
}
