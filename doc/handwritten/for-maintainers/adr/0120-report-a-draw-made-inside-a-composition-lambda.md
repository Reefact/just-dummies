# ADR-0120 | Report a draw made inside a composition lambda

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0120-report-a-draw-made-inside-a-composition-lambda.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-01
**Accepted:** 2026-11-01
**Decision Makers:** Reefact

## Context

The library keeps a recipe and a draw apart: a generator describes what a value must satisfy, and `Generate()`
alone produces one. The first developer-UX trial found users deriving one value from another by calling
`Generate()` on an inner generator inside the lambda passed to `As(...)` or `Combine(...)`. The lambda runs at
generation time, so the inner value is drawn on every value — but from the source the inner generator carries.
Under an explicit `Any.WithSeed(...)` context, an inner generator built from the static `Any` draws from the ambient
seed, and the run replays only partly from the seed it reports (ADR-0102).

Two forms keep such a chain one recipe: an offset drawn alongside the first value and composed through
`Any.Combine`, which holds by construction; and `Then`, which builds the dependent generator from the drawn value
and draws it on the same source (ADR-0119). Which fits depends on the relation, and an offset's unit and bounds are
the developer's to choose.

The library's rules report what the type system cannot see, at the call site, with a message naming the way out
(ADR-0038). A rule recognising JustDummies' own `As` and `Combine` can tell, from the operation tree alone, that a
`Generate()` sits inside a lambda passed to one of them.

## Decision

A warning in the reproducibility family reports a `Generate()` on a generator inside a lambda passed to JustDummies'
`As` or `Combine`, at any depth, and names the two ways out — an offset composed with `Combine`, or `Then` — with no
code fix.

## Rationale

**The mistake is invisible and the cost is the replay.** The chain compiles, draws, and passes under the ambient seed;
it fails only the promise the second fundamental makes, and only under an explicit context. That is the kind of defect
a rule exists for: one the type system accepts and a test run does not show.

**A warning, because the remedy is always better.** Even when the inner generator comes from the same context and
the chain replays, the draw still sits apart from the recipe, and the same two forms state the relation as one
recipe a reader can see. There is no correct spelling the rule would have to tolerate, which is what keeps it from
being information only.

**Both ways out, because the rule cannot tell which applies.** Whether the second value is an offset from the first
or a generator built from it is a fact about the domain. Naming both lets the reader pick; a code fix would have to
choose the unit and bounds of an offset nobody stated.

**Where the draw sits, not where it draws from.** Reading the inner generator's source would need dataflow the rules
deliberately do not follow (a generator held in a local or a field), and would split one mistake into two verdicts.
The place of the call is certain from the tree, so the rule answers exactly that.

## Alternatives Considered

### Report only an inner draw from the static `Any`

Considered because it is the case that breaks replay. Rejected: it needs to resolve each inner generator's source,
which the rules do not follow through locals and fields, and it would leave silent a form the two remedies improve
all the same.

### Information severity

Considered because a same-context inner draw replays. Rejected: the rule has no case where the reported spelling is
the right one to keep, which is the distinction ADR-0077 draws for information rules.

### A code fix rewriting the lambda as `Then`

Considered because `Then` covers every case mechanically. Rejected: where an offset suffices it is the clearer form,
and the rewrite cannot know which the developer means.

## Consequences

### Positive

* A partial replay under an explicit context is reported where it is written.
* Every report names a form that keeps the chain one recipe.

### Negative

* A same-context inner draw, which replays, is reported too.

### Risks

* A lambda passed to another library's `As` or `Combine` is not recognised, by design.

## Follow-up Actions

* None.

## References

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — analyzers guard the recipe-versus-value boundary.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — when a rule reports a correct spelling.
* [ADR-0102](0102-let-a-collection-or-composition-inherit-its-operands-source.md) — the partial-replay case.
* [ADR-0119](0119-draw-a-value-that-depends-on-another-draw.md) — `Then`.
* Issue #358; CORE-C2 of the first developer-UX trial.
