# ADR-0121 | Exclude a value from any generator by redrawing

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0121-exclude-a-value-from-any-generator-by-redrawing.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

Every typed builder carries `DifferentFrom(value)`, an exclusion in the builder's own specification: it is checked
when it is declared, `Any.Int32().OneOf(5).DifferentFrom(5)` is refused at once naming both constraints, and the
analyzers that reason about exclusions (JD023, JD034) see it at build time. Constraints of that kind are
constructive; ADR-0033 also admits rejective ones, a bounded redraw that fails loudly rather than looping.

A generator built by `As` or `Combine`, a scaffolded generator, or one a user wrote is seen only as an `IAny<T>`, and
has no `DifferentFrom`. The second developer-UX trial met six contexts where a constructor refuses two equal
parameters or a test needs a second value differing from a first. Two independent draws collided on rare seeds —
one in 315 for one type, 71 in 2,000 for another — and users wrote redraw loops by hand in scaffolded generators.

C# resolves a call to an instance member before an extension of the same name, from the receiver's static type.

## Decision

Every `IAny<T>` gains a `DifferentFrom(value)` extension that redraws while the drawn value equals the given one under
the type's default equality, a bounded number of times, and fails with its seed when the budget is spent; the typed
builders keep their constructive `DifferentFrom`, which the receiver's static type selects, and the analyzers reason
only about that one.

## Rationale

**The trial's need is one call, and only a rejective exclusion can serve every generator.** An opaque generator
exposes no specification to narrow; the only exclusion it admits is to look at what it drew. A bounded redraw is the
form ADR-0033 already accepts, and it fails loudly with the seed that replays it, which is what the hand-written loops
did without the bound.

**Two guarantees under one name, written down.** The constructive exclusion refuses a contradiction at declaration;
the rejective one sees nothing until it draws. Giving them different names would make the scaffolded generator read
differently from the builder it wraps; keeping one name and recording the difference keeps the call site uniform and
the guarantee knowable, from the static type the reader can see.

**The analyzers stay on firm ground.** A rule that reasoned about the rejective exclusion would have to predict a
bounded redraw's outcome; reading only the constructive one keeps their verdicts exact, and their pages say which
they read.

**Bounded, so a pinned generator fails at once.** A generator yielding only the excluded value spends the budget in
its first call and reports it, rather than looping.

## Alternatives Considered

### Let `any` read an `a.Equals(b)` guard and emit the redraw itself

Considered because the scaffolder already reads guards. Rejected: once the core says it in one call, widening the
guard reader for this case adds a mechanism nobody needs.

### A different name for the rejective exclusion

Considered because it would make the guarantee visible in the call. Rejected: the same intent would read two ways
depending on how the generator was obtained, which is the inconsistency the trial reported.

## Consequences

### Positive

* Any generator, scaffolded or hand-written, can be told to differ from a value in one call.
* A redraw loop no longer needs writing, and a generator that yields nothing else fails with its seed.

### Negative

* `IAny<int> g = Any.Int32(); g.DifferentFrom(5)` takes the rejective exclusion, with nothing at the call site to
  say so.

### Risks

* A generator whose domain holds the excluded value most of the time may exhaust the budget although another value
  exists; the message states an exhausted budget, not an empty domain.

## Follow-up Actions

* JD023's and JD034's pages state that they read only the constructive `DifferentFrom`.

## References

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — constructive and rejective
  constraints.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bounded redraws that fail explicitly.
* Issue #395; CORE-4 of the second developer-UX trial.
