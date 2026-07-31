# ADR implementation reference

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](adr-implementation-reference.fr.md)

This document owns implementation details extracted from Architecture Decision Records. ADRs remain the authoritative source for **what was decided and why**; this reference describes the current technical realization and may evolve without changing those decisions.

> Recovered from `Reefact/first-class-errors`, where this reference served two products. Only the sections whose decisions came to this repository were kept — the Request Binder contracts, the GenDoc catalog, `FirstClassErrors.Testing`'s factories and the binder's documentation-only surfaces stayed behind with the code they describe. ADR numbers were remapped to this repository's own, per [ADR-0045](../adr/0045-renumber-the-decision-base.md).

## Analyzer compatibility floor

Related decisions: [ADR-0001](../adr/0001-lock-the-analyzer-roslyn-floor.md).

The analyzer is compiled against the Roslyn floor declared by `RoslynFloorVersion` in `Directory.Build.props`. The package keeps the analyzer under `analyzers/dotnet/cs/`.

The current realization uses complementary guards:

* the analyzer package reference is pinned to the declared floor;
* `RoslynFloorTests` inspects assembly metadata and rejects newer `Microsoft.CodeAnalysis*` references;
* the analyzer workflow packs the real NuGet artifact and builds a sample with the floor SDK, proving both loading and packaging;
* Dependabot ignores automated updates for the floor-defining Roslyn packages.

When the floor changes, update the central property, the floor SDK used by the workflow and floor-check project, and the documented compiler requirement. The architectural change itself requires a new ADR that supersedes ADR-0001.

## ADR pull-request check

Related decision: [ADR-0002](../adr/0002-check-every-pull-request-against-the-adr-base.md).

The ADR check is a maintainer and agent procedure, documented in `AGENTS.md`, that compares a change against accepted decisions and identifies whether it records, supersedes, or conflicts with an ADR.

The current GitHub workflow is manually dispatchable and therefore supports the procedure but does not, by itself, guarantee that every pull request was checked. Any future automated enforcement belongs in the workflow documentation and configuration rather than ADR-0004.

## JustDummies generation contracts

Related decisions: [ADR-0003](../adr/0003-host-dummies-as-a-standalone-package.md), [ADR-0004](../adr/0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md), [ADR-0005](../adr/0005-cap-any-combine-at-arity-eight.md), [ADR-0006](../adr/0006-materialize-dummies-only-through-generate.md).

JustDummies is shipped as a standalone package with no dependency on any error-handling runtime; the boundary is guarded by an architecture test. Generation is unseeded by default; reproducible generation is selected explicitly and exposes the seed needed to replay failures.

Distinct collection generation first compares the requested count against the element generator's cardinality hint, when `ICardinalityHint` can provide one, net of any values pinned outside that domain via `Containing(...)` and any opaque draws requested via `ContainingAny(...)` — both widen what the generator itself must still supply rather than counting against it. A floating-point or decimal range is not treated as cheaply countable, since enumerating its representable values is type-specific bit-arithmetic disproportionate to the dummy use case, so such a generator only participates in the eager check when pinned to an explicit allow-list or a single value (`OneOf`, `Zero`, `Between(x, x)`), never through a wider range. When cardinality is unknown, generation uses a bounded draw and fails explicitly rather than looping forever. The bound is a safety mechanism, not a proof that every foreign or biased generator will succeed whenever enough distinct values theoretically exist. `CollectionState` and `ICardinalityHint` unify cardinality and membership behind one interface, so a generator with a finite domain cannot drift out of the eager perimeter through a comparer.

When cardinality is unknown or large, the draw budget is derived from the request rather than fixed: a domain known to hold at most a million values allows sixty-four draws per value it could yield, a larger or unknown one allows sixty-four per element requested, and the result is raised to a floor of ten thousand. A collision count above that budget ends the fill.

Exhaustion raises `AnyGenerationException`, which carries the seed as a nullable integer alongside a message naming the requested count, which generator produced too few, how many distinct values it reached, and how to replay the run. The replay guidance is qualified rather than promised outright when the culprit is not fully reproducible — a foreign generator, or a composition mixing one with a sourced operand — since a full replay of its elements would be false. `AnyDerivation` owns that determination: it resolves the source behind a composed generator, decides whether the composition is reproducible, and decides whether every draw comes from one source.

`Any.Combine` provides overloads up to arity eight, plus `PairOf` and `TripleOf` for the tuple shapes; each takes one generator per part and a composing function. Higher arities are intentionally outside the supported convenience surface and should use composition or a domain-specific factory. The arity-seven and arity-eight overloads carry a localized parameter-count suppression whose justification names the decision that set the ceiling, and the ceiling itself is documented on the arity-eight overload where a caller reaching the limit will read it.

Materialization occurs only through `Generate()`. Builder operations describe generation and do not produce hidden side effects.

## Maintenance rules

* Change this reference when implementation mechanics change but the decisions remain valid.
* Write a new ADR when the architectural choice, compatibility promise, or accepted trade-off changes.
* Keep links from each affected ADR to the relevant section of this reference.
* Do not move rationale, rejected alternatives, or architectural consequences out of ADRs.
