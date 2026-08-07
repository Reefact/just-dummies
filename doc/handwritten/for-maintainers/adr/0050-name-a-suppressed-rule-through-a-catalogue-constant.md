# ADR-0050 | Name a suppressed rule through a catalogue constant, not a string literal

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0050-name-a-suppressed-rule-through-a-catalogue-constant.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-02
**Accepted:** 2026-08-02
**Decision Makers:** Reefact

## Context

This repository carries **83 `[SuppressMessage]` attributes**, and no `#pragma warning disable` at
all: the attribute is how a rule is silenced here. Each one names its rule as two string literals — a
category and an id — that nothing verifies.

The literals are not fragile in the obvious way. A mistyped id leaves the rule active, the diagnostic
reports, and the CI warning ratchet turns it into an error: that failure is loud. What is silent is the
opposite case. When a vendor renames or retires a rule, the attribute keeps compiling, silences
nothing, and no build says so. Dead suppressions accumulate, and the codebase claims to be silencing
something it no longer silences.

The exposure is concentrated rather than spread thin: **13 distinct rules account for 67 of the 83**
suppressions — `S3267` alone appears 14 times, `S107` 9, `S2436` and `S2325` and `CA1822` 7 each. One
vendor rename therefore touches many files at once, in projects whose authors have no reason to be
looking.

`DiagnosticCatalog` publishes the rules of an analyzer package as `const string` members generated from
the analyzer's own descriptors, so `[SuppressMessage]` can take references the compiler resolves. Its
`DiagnosticCatalog.Sonar` catalogue mirrors **SonarAnalyzer.CSharp 10.31.0.145097** — the exact version
this repository pins — and `DiagnosticCatalog.NetAnalyzers` mirrors the SDK's `CA` rules. Between them
they describe **every** `S` and `CA` rule suppressed here.

## Decision

A suppression names its rule through a catalogue constant. `DiagnosticCatalog.Sonar` and
`DiagnosticCatalog.NetAnalyzers` are referenced for every project, as build-time-only assets, and the
`DCAT` analyzers they carry keep the rule enforced at their default severity.

The product's own `JD` diagnostics stay literals: no catalogue describes them yet.

## Rationale

**It converts a silent failure into a build failure.** A retired rule currently leaves an attribute
that reads as a suppression and is not one. A constant does not survive its rule's removal from the
catalogue, so the next catalogue upgrade reports it. That is the whole value; typo protection is not,
since a typo is already loud here.

**The exposure justifies the dependency.** 67 of 83 suppressions concentrate in 13 rules, which is the
case where a vendor rename is a multi-file event rather than a one-line fix. Measured, not assumed.

**The catalogue is pinned to the analyzer this repository actually runs.** The Sonar catalogue mirrors
the same `10.31.0.145097` pinned in `Directory.Packages.props`, so the constants describe the rules
this build reports rather than a nearby version's.

**Central, not per project, because a gap is not visible.** Every project here runs these analyzers, so
every project can suppress one of their rules. A project left without the catalogue is one where a new
literal lands unchecked, and nothing distinguishes it from a converted one by reading it.

**It costs the published artifact nothing, and that is checked rather than argued.** The references are
build-time only; the packed `.nuspec` declares no dependency and the package carries no catalogue file.
The emitted assembly is unchanged: `SuppressMessageAttribute` is conditional on `CODE_ANALYSIS` and is
never emitted, and a byte comparison of the library built before and after the conversion differs only
in the module identity (MVID), the two deterministic timestamps, and the PDB signature and checksum —
72 bytes, all derived from the source text, none of them code.

**No adoption ramp was needed, which is itself the argument for doing it in one commit.** The `DCAT`
rules ship as errors, and the guide for existing codebases expects a temporary `.editorconfig`
downgrade. Neither diagnostic needed one here: all 83 suppressions already carried a `Justification`,
and the conversion landed at once. Adopting at the default severity is what makes a new literal
unmergeable from the first commit.

## Alternatives Considered

### Keep the literals

Rejected: it keeps the silent case. Nothing reports a suppression that has stopped suppressing, and
with 13 rules spread across 67 attributes the day a rename lands is the day several files quietly stop
meaning what they say.

### Reference the catalogues only in the projects that suppress today

The minimal reading of "no dependency without a reason". Rejected: the gap it leaves is invisible. A
project without the catalogue accepts a new literal silently, and a reader cannot tell which projects
are covered without opening every `.csproj`.

### Convert progressively behind an `.editorconfig` downgrade

The path the catalogue's own adoption guide describes, for codebases that cannot convert at once.
Rejected as unnecessary here rather than wrong: this codebase met both default severities on arrival,
so a downgrade would have bought a slower migration and a window in which new literals could land.

### Write our own constants

A file of `const string` per rule, no dependency. Rejected: it is the same maintenance the vendor
already does, done worse — a hand-written constant cannot notice that its rule was retired, which is
the failure this record is about.

## Consequences

### Positive

* A rule that disappears from an analyzer upgrade is reported instead of leaving a dead suppression.
* "Where is this rule suppressed, and why?" is *Find All References*, not a text search.
* A new literal suppression cannot merge, in any project, from the first commit.

### Negative

* Two build-time dependencies where there were none, on packages from the same author as this
  repository. Dogfooding is the honest word for it, and it cuts both ways: a defect in the catalogue is
  a defect this repository meets first.
* An analyzer upgrade now wants its catalogue bumped alongside, or the constants describe a version the
  build no longer runs. The version pairing is written where both are declared.

### Risks

* The catalogues are generated from a vendor's descriptors and are unofficial. A rule the generator
  misses is a rule that cannot be referenced; the literal form still compiles, so the fallback is the
  status quo rather than a wall.
* `DCAT0006` and `DCAT0014` are errors. A future analyzer upgrade that introduces suppressions faster
  than the catalogue covers them would block a build; the same `.editorconfig` downgrade the adoption
  guide describes is the release valve, and it was deliberately not needed to get here.

## Follow-up Actions

* The 7 `JD` suppressions stay literals. Publishing a catalogue for this product's own `JD`
  diagnostics would close that gap **and** give consumers checked suppressions of the rules
  JustDummies ships — a product question, to weigh against
  [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) after 1.0.

## References

* [ADR-0003](0003-host-dummies-as-a-standalone-package.md) — the standalone requirement this record
  had to satisfy, and which `tools/packaging/pack.sh` asserts on the produced artifact.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the rule this record was
  weighed against, and the one the follow-up above must answer to.
* `Directory.Build.props` — where the references and the global usings are declared, and why there.
