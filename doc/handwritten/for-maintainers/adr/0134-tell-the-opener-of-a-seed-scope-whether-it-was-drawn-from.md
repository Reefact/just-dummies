# ADR-0134 | Tell the opener of a seed scope whether it was drawn from

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0134-tell-the-opener-of-a-seed-scope-whether-it-was-drawn-from.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-05
**Accepted:** 2026-11-05
**Decision Makers:** Reefact

## Context

[ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.md) opened the ambient seed scope to test-framework adapters as
a public, disposable handle, `Any.UseSeed(...)`, typed `IDisposable`. Whoever opens the scope owns telling the reader
which seed to replay, and the xUnit adapter does so when a test fails.

The adapter's rule is never to name a seed no draw used: an outer level that a more specific one superseded prints
nothing, since replaying its seed would reproduce nothing. The third developer-UX trial met the case that rule did not
reach, recorded in seventeen findings (XU-1): a test that failed without drawing at all, on a literal, was still told a seed,
and sent looking for a drawn value that did not exist. Under `[assembly: Reproducible]`, a body wrapped in
`Any.Reproducibly(...)` printed two seed lines, one naming the attribute's seed that nothing used.

The library already knows the answer. Every seeded source counts its primitive draws, the quantity the seed golden
master pins ([ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md)). The handle `UseSeed` returns does not
expose it, and `Any.Reproducibly` reported on any failure too.

`UseSeed` has not shipped in a stable release: its signatures are still in the unshipped public API baseline.

## Decision

`Any.UseSeed(...)` returns a public `SeedScope` handle, still disposable, that also says whether any value was drawn
from its seed (`HasDrawn`); the xUnit adapter and `Any.Reproducibly` report a seed only when it was.

## Rationale

**The rule already stated, applied to the case it missed.** A seed is named so that replaying it reproduces the
failure. A seed nothing drew from reproduces nothing, whether a more specific level took the draws or the test never
drew; the second case is the first one's rule, not a new one.

**On the handle ADR-0017 made public, so every adapter has it.** The seam was opened so that an adapter for any
framework could be written against the public surface. The answer belongs on that seam rather than behind a grant to
one package.

**A yes or no, not a count.** A number of primitive draws is not a number of values — a string takes several — and
publishing it would make the draw pattern a contract. The only question an adapter has is whether replaying the seed
would reproduce anything.

## Alternatives Considered

### Grant `JustDummies.Xunit` access to the library's internals

Considered because it adds no public surface before the 1.0 freeze. Rejected: it serves only the first-party adapter,
which contradicts the reason ADR-0017 made the scope public, and an internal member read across packages ties the
adapter to one exact library version where the trains are independent
([ADR-0047](0047-declare-the-adapters-library-dependency-independently.md)).

### Keep the behaviour and document it

Considered because the case is low in severity. Rejected: seventeen findings record the line read as pointing at a
drawn value, and the fix is one read-only property on a handle that already exists.

### Expose the number of draws

Considered because the count is what the source holds. Rejected for the reason above: it would publish the draw
pattern, which no caller needs.

## Consequences

### Positive

* A test that failed without drawing is no longer told a seed, by the adapter or by `Any.Reproducibly`.
* Under an assembly-level attribute, a body wrapped in `Any.Reproducibly(...)` prints only the seed it drew from.
* Any adapter can apply the same rule through the public handle.

### Negative

* One more public type and member to keep after 1.0.
* `UseSeed`'s return type changes from `IDisposable` to `SeedScope`: source written against it still compiles, but
  code already compiled against a preview has to be recompiled.

### Risks

* Draws made outside the scope's flow — a thread started without the execution context, an explicit
  `Any.WithSeed(...)` context — do not count, so a test that drew only there is not told the seed. Replaying that
  seed would not reproduce those draws either, so silence is the correct answer.

## Follow-up Actions

* None beyond the implementation and its documentation.

## References

* [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.md) — the decision this amends. It keeps its status; the
  handle it made public now also says whether its seed served a draw.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) — the draw count this reads.
* [UX trial 3, XU-1](../../../../tools/ux-trial/REPORT-3.md#xu-1).
* Issue [#440](https://github.com/Reefact/just-dummies/issues/440).
