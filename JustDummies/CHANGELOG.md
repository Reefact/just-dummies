# Changelog

All notable, user-facing changes to **JustDummies** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).
Releases are cut from the `lib` train (see [CONTRIBUTING.md](../CONTRIBUTING.md)).

## [Unreleased]

### Added

- **See what your constraints left of a pool you supplied.** When a value set is declared beside constraints, a
  value the constraints refuse leaves the domain silently — only an emptied domain is reported. That is fine
  until the value set is a *catalogue* you maintain, at which point you cannot tell whether to widen the
  invariant or fix the catalogue. `IPoolInspection<T>` answers it: `GetSurvivors()` for the values still
  drawable, `GetRejections()` for the ones refused, each naming every `DeclaredConstraint` that refuses it — a
  name and its rendered arguments kept apart, so you can group and filter instead of parsing text. It is carried
  by the generators whose pool you supply whole (`Any.String().OneOf(...)`, `Any.OneOf(...)`/`Any.ElementOf(...)`)
  and implemented **explicitly**, so it never appears among the constraints while you write them; reach it with a
  cast, and test the cast, since the interface is optional. Nothing here draws or consumes randomness, so an
  inspection between two draws leaves a seeded run replaying identically. The library reports and does not judge:
  it never warns that a pool was narrowed, because narrowing a shared catalogue at one call site is what the
  composition is for. New guide: *Inspecting a pool* ([ADR-0067](../doc/handwritten/for-maintainers/adr/0067-report-a-filtered-pool-through-an-explicit-interface.md)).

- **JD029 tells you at build time when a value you wrote into a pool can never be drawn.** A value set composes
  with the constraints declared beside it, and a value they refuse leaves the domain in silence — so a pool can
  read as five values and draw from three. The new rule reports each such value where it is written, naming the
  constraint that refuses it. It is the dual of JD024: that one reports a constraint narrowing nothing, this one
  a value nothing lets through. **Info, not a warning**: narrowing a shared pool at one call site is what the
  composition is *for*, so this states a fact to weigh rather than a verdict. It reads only what is written at
  the call site — a pool held in a variable, which is what a catalogue always is, is answered instead at run
  time by `IPoolInspection<T>`. The claim is deliberately one-sided: a constraint whose argument does not fold
  is skipped rather than guessed at, so the rule can under-report but never accuse a value it has not tested.

### Documentation

- **How to get a NaN when you actually need one.** `Any.Double()`, `Any.Single()` and `Any.Half()` refuse a
  non-finite value as an *argument* as well as a draw, so `Any.Double().Except(double.NaN)` throws — which read
  as a missing feature, because nothing said what to do instead. The packaged readme now carries the rule, its
  reason, and the exit (`Any.OneOf(double.NaN, ...)`, whose pool the library does not judge), plus when to use a
  literal rather than a pool, the `Equals`/`==` asymmetry that makes a pooled `NaN` deduplicate, and why
  `decimal` is not part of the subject at all. The refusal message names the exit too, and the three builders'
  XML docs carry it, so the answer is reachable from IntelliSense at the moment the caller is blocked. The
  recipe is locked in by tests: a documented exit that quietly stopped working would be worse than no
  documentation.

### Fixed

- **A decimal exclusion declared twice no longer empties a satisfiable grid.** On `Any.Decimal()` with
  `WithScale`, the engine counts excluded grid points to decide satisfiability and cardinality, and it counted a
  duplicated value as many times as it was declared — so `Except(0.01m).DifferentFrom(0.01m)` (restating an
  exclusion already in force) or `Except(0.01m, 0.01m)` could refuse a declaration as exhausted while values
  were still drawable, with a message claiming every grid value was forbidden. Excluded values are now
  deduplicated at construction, as the integer engines always did. A genuinely exhausted grid still conflicts
  eagerly; generation, draw counts and the public surface are unchanged.
- **A conflict caused by an exclusion now names that exclusion.** On `Any.Enum<T>()` and `Any.Guid()`, a
  constraint that emptied the domain reported the constraint it had emptied instead — so
  `Any.Enum<T>().OneOf(a, b).Except(a, b)` said *"no value OneOf(a, b) allows remains available"*, naming the
  victim and leaving the cause to be guessed, and an excluded pin said only *"which the exclusions forbid"*,
  naming none of them. Both now read like the interval generators, which were fixed first: *"it forbids every
  value OneOf(a, b) allows"*, and *"Empty() already pins the value to 00000000-… and NonEmpty() forbids it"*.
  Only exclusions that actually removed something are named, since one whose values were never drawable caused
  nothing. Generation, conflict detection and the public surface are unchanged — only the wording of the
  message a failing declaration carries.

## [1.0.0-preview.1] - 2026-08-07

**Why the jump from `0.1.0-preview.1`.** Not because the surface grew — it did not change at all
between the two, and `PublicAPI.Unshipped.txt` is still where it is declared. Because the number was
understating the intent. `0.1.0` reads as an early sketch inviting nobody; this library has been in
use inside another repository for its whole life, and what the preview is waiting for is an outside
consumer, not more design. A `1.0.0-preview` says what is actually true: this is the surface offered
for 1.0, and the preview exists so it can be contradicted before it freezes.

A preview still promises nothing about the surface. What it does now promise is the seed.

### Added

- **A seed replays across patch and minor versions.** Within a major version, a given seed draws the
  same values; the mapping may change on a major
  ([ADR-0049](../doc/handwritten/for-maintainers/adr/0049-replay-a-seed-across-patch-and-minor-versions.md)).
  This matters because a pinned seed is usually committed: without the promise an upgrade would not
  break such a test, it would leave it green while it quietly stopped covering the case it was pinned
  for. The promise is enforced rather than stated — a golden master pins, for each factory at a fixed
  seed, both the values produced and the number of draws consumed, the latter because a single
  sequential stream is shared by the whole scope, so a generator that changes how much it consumes
  shifts every value drawn after it.

### Changed

- The package carries an icon, shared by every package this repository publishes.
- The packaged readme's links point at this repository rather than the one JustDummies was extracted
  from.

## [0.1.0-preview.1] - 2026-07-31

First published version. The library itself is not new — it was developed inside
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) and
[extracted into this repository](../doc/handwritten/for-maintainers/adr/0044-extract-justdummies-into-its-own-repository.md)
with its full history on 2026-07-31. This is the first time it reaches nuget.org.

**A preview on purpose.** The public surface is large and has never been exercised by an outside
consumer. It is declared in `PublicAPI/<tfm>/PublicAPI.Unshipped.txt` rather than
`PublicAPI.Shipped.txt`, which is the honest state: nothing here is promised yet, and a stable
release is what will freeze it.

### Added

- **The `Any` generator surface** — a fluent DSL producing arbitrary yet valid test values.
  Constraints express the invariants a value must satisfy, never what the test asserts. Scalars,
  strings, collections, dictionaries, sets, enums, GUIDs, temporal types and URIs, plus composition
  through `As`, `Combine` and `OrNull`.
- **Fail-fast conflict detection.** Contradictory constraints are refused at declaration with a
  message naming both sides, rather than looping or silently drawing a value that satisfies neither.
- **Reproducibility.** `Any.Reproducibly` pins a seed for the run and reports it when the body
  throws, so a red test says how to replay itself; `Any.ReproduciblyAsync` covers `async` bodies,
  and `Any.UseSeed` opens an explicit scope.
- **28 first-party analyzers** (`JD001`–`JD028`), bundled in this package under
  `analyzers/dotnet/cs`. They guard the recipe-versus-value boundary where the type system cannot
  reach — a generator rendered as text, a discarded result, a draw outside the pinned scope,
  constraints that admit no value.
- **Two target frameworks.** `netstandard2.0` for the widest reach, and `net8.0` which additionally
  carries the generators for types that do not exist downlevel: `DateOnly`, `TimeOnly`, `Int128`,
  `UInt128` and `Half`. The supported .NET Framework floor is 4.7.2, and CI runs the suites on it.
- **Package hardening**: embedded SPDX SBOM, SourceLink, symbol package, deterministic build, and a
  build-provenance attestation on the release artifact.

### Notes

Commit messages older than 2026-07-31 cite issue and pull-request numbers of
`Reefact/first-class-errors`, and ADR numbers this repository has since renumbered. The mapping is
in [ADR-0045](../doc/handwritten/for-maintainers/adr/0045-renumber-the-decision-base.md); the full
migration record is under
[`doc/handwritten/for-maintainers/migration/`](../doc/handwritten/for-maintainers/migration/).

[Unreleased]: https://github.com/Reefact/just-dummies/compare/lib-v1.0.0-preview.1...HEAD
[1.0.0-preview.1]: https://github.com/Reefact/just-dummies/compare/lib-v0.1.0-preview.1...lib-v1.0.0-preview.1
[0.1.0-preview.1]: https://github.com/Reefact/just-dummies/releases/tag/lib-v0.1.0-preview.1
