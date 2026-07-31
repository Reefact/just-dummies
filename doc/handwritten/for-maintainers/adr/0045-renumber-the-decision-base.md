# ADR-0045 | Renumber the decision base into a contiguous sequence

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0045-renumber-the-decision-base.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-31
**Accepted:** 2026-07-31
**Decision Makers:** Reefact

## Context

The 34 decisions this repository inherited from `Reefact/first-class-errors`
([ADR-0044](0044-extract-justdummies-into-its-own-repository.md)) arrived with the numbers they had been
accepted under there — 0011, 0013, 0015, 0020, 0022, 0025, 0030–0033, … — a sequence full of holes, because
the numbers in between belong to decisions about FirstClassErrors that stayed where they were.

ADR-0044 kept those numbers, arguing that renumbering would break the cross-references inside the accepted
texts. That reasoning conflated two things. A number is an **identifier**, not part of the decision:
rewriting `ADR-0045` to `ADR-0024` inside a citation keeps it pointing at the same decision and changes
neither context, decision, rationale, alternatives, consequences, status, dates nor attribution. What
[ADR-0024 (first-class-errors)](https://github.com/Reefact/first-class-errors/blob/main/doc/handwritten/for-maintainers/adr/0024-allow-a-one-time-editorial-refactoring-of-accepted-adrs.md)
forbids as a precedent is changing accepted **decisions** in place. This is indexing.

A second gap surfaced at the same time and is the reason the renumbering is not merely cosmetic. Nine
decisions govern this repository — its analyzer's Roslyn floor, its commit-scope rule, its mutation gate, its
coding rules, its Sonar rule set, its ADR process — and none of them were here. The build enforced them while
the record lived only in `Reefact/first-class-errors`, which meant a supersession over there would silently
change the rules over here.

## Decision

The nine decisions this repository applies are **adopted** into this base, and the whole base is renumbered
into a contiguous **0001–0045**, ordered by the number each decision held in `Reefact/first-class-errors` so
that no existing decision moves relative to another and the adoptions land in their historical place.

An adoption is not a copy. Both records are live from now on, and either repository can supersede its own
without touching the other — which is the correct behaviour for two products that no longer share a build.

Three kinds of provenance are distinguished, in each ADR's header and in the index's **Origin** column:

| Header note | Meaning |
| --- | --- |
| *Originally recorded in `Reefact/first-class-errors` as ADR-NNNN* | the decision **moved** here with its code |
| *Adopted from `Reefact/first-class-errors` ADR-NNNN* | the decision is **live in both** repositories |
| *(none)* | decided here |

A citation of an ADR that exists only in the other repository is written **`ADR-00NN (first-class-errors)`**.
An unqualified number always means this base.

## Consequences

### The git history keeps the old numbers, permanently

420 commit messages cite ADR numbers — `docs: draft ADR-0010 hosting Dummies as a standalone package` is this
repository's ADR-0003. They were not rewritten: `main` is published, and rewriting them would mean a second
`filter-repo` pass over a history other people may already have. `git log --grep ADR-0045` therefore finds
commits about what is now ADR-0024, and nothing will ever change that. The header notes and the table below
are the only decoder, which is why removing either breaks something that cannot be rebuilt.

### Qualifying the foreign citations was not optional

Before the renumbering, this repository's numbers started at 0011 and never collided with the
FirstClassErrors numbers its texts cite. After it, several of those fall inside 0001–0045. Every citation of
a decision that stayed over there was qualified in the same change; leaving them bare would have made
`ADR-0006` mean two different decisions in the same sentence, silently.

### The renumbering reaches well past the ADR texts

Analyzer sources (`Descriptors.cs`, `CollectionConstraintsAdmitNoValueAnalyzer.cs`), the test suites,
`Directory.Build.props`, the project files and the workflows all cite ADR numbers. Those non-Markdown
citations are the ones that would have rotted silently, since nothing compiles a comment.

### Two decisions were deliberately not adopted

`ADR-0002 (first-class-errors)` floors the tooling runtime at the oldest supported LTS. Its subject is the
`fce` tool and its documentation worker; this repository has no tool yet, so adopting it would decide a floor
for a binary that does not exist. It belongs here the day the `dum` scaffolder is built.

`ADR-0024 (first-class-errors)` authorises one bounded editorial migration of accepted ADRs. It is a
historical, one-off authorisation granted to that repository for a migration this one never performed;
adopting the permission for an act not committed would record a decision that was never taken.

## Alternatives Considered

### Keep the holes and explain them in the index

Considered, and in fact done first: the index gained a paragraph saying why the sequence started at 0011.
Rejected because the explanation has to be re-read every time a number is met anywhere else — in a code
comment, in a commit message, in a cross-reference — and the index is not there.

### Append the adopted decisions at the end instead of inserting them

Considered, on the argument that a decision "reached Accepted in this repository" only on the day it was
adopted, so its dated line — and therefore its position — should be today's. Rejected because the same
argument would apply to the 34 moved decisions, which kept their original dates: this repository did not
decide anything in July either, it did not exist. Treating the two groups differently would have made the
sequence mean one thing for 34 entries and another for 9.

### Renumber without recording the old numbers

Rejected outright. The git history cannot follow, so dropping the mapping would strand 420 commit messages
against a base whose numbering no longer matches, with nothing to reconstruct it from.

## Mapping

| Was (FCE) | Now | Origin | Decision |
|---|---|---|---|
| ADR-0001 | [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) | adopted | Lock the analyzer's Roslyn floor |
| ADR-0004 | [ADR-0002](0002-check-every-pull-request-against-the-adr-base.md) | adopted | Check every pull request against the ADR base |
| ADR-0011 | [ADR-0003](0003-host-dummies-as-a-standalone-package.md) | moved | Host JustDummies as a standalone package in this repository |
| ADR-0013 | [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) | moved | Gate distinct collections by cardinality, otherwise by a bounded draw |
| ADR-0015 | [ADR-0005](0005-cap-any-combine-at-arity-eight.md) | moved | Cap Any.Combine at arity eight |
| ADR-0020 | [ADR-0006](0006-materialize-dummies-only-through-generate.md) | moved | Materialize dummies only through Generate() |
| ADR-0022 | [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.md) | moved | Floor the library's .NET Framework support at 4.7.2 |
| ADR-0025 | [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.md) | moved | Generate matching strings from a home-grown regular subset |
| ADR-0030 | [ADR-0009](0009-draw-arbitrary-strings-from-an-explicit-terminal-set.md) | moved | Draw arbitrary strings from an explicit, terminal value set |
| ADR-0031 | [ADR-0010](0010-name-any-factories-after-their-clr-type.md) | moved | Name Any's scalar factories after their CLR type |
| ADR-0032 | [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.md) | moved | Draw arbitrary values from an explicit, top-level choice pool |
| ADR-0033 | [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.md) | moved | Meet string exclusions with a bounded redraw |
| ADR-0034 | [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.md) | adopted | Require a scope on the version-driving commit types |
| ADR-0035 | [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.md) | moved | Enforce structural Any conflicts at compile time, value-dependent ones at run time |
| ADR-0036 | [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.md) | moved | Draw lattice-constrained scalars on the grid |
| ADR-0037 | [ADR-0016](0016-vary-the-datetimeoffset-offset-dimension.md) | moved | Vary the DateTimeOffset offset dimension |
| ADR-0038 | [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.md) | moved | Open the ambient seed scope to test-framework adapters |
| ADR-0039 | [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md) | moved | Adapt JustDummies to xUnit v3 through a companion package |
| ADR-0040 | [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md) | moved | Split the JustDummies test bed between an example suite and a property suite |
| ADR-0041 | [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.md) | moved | Draw flag-enum combinations behind an opt-in |
| ADR-0042 | [ADR-0021](0021-serialize-draws-on-a-random-source.md) | moved | Serialize draws on a random source, and scope reproducibility to the draw sequence |
| ADR-0043 | [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.md) | adopted | Gate pull requests on the mutation score of what they changed |
| ADR-0044 | [ADR-0023](0023-ship-justdummies-analyzers.md) | moved | Ship first-party JustDummies analyzers, and guard the reproducible async surface with them |
| ADR-0045 | [ADR-0024](0024-guard-public-and-internal-arguments-against-null.md) | moved | Guard public and internal arguments against null, enforced by a reflection convention |
| ADR-0046 | [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md) | adopted | Make the per-pull-request mutation gate advisory |
| ADR-0047 | [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.md) | moved | Measure JustDummies mutation against the deterministic unit suite only |
| ADR-0048 | [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) | moved | Guarantee a generated regex value matches its pattern, by bounded redraw |
| ADR-0049 | [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md) | moved | Drop the JustDummies generator from the per-pull-request mutation matrix |
| ADR-0050 | [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) | moved | Let a size maximum cap without steering the draw, and ceiling an explicitly demanded size |
| ADR-0051 | [ADR-0030](0030-filter-the-datetimeoffset-pool-by-the-declared-offset.md) | moved | Filter the DateTimeOffset pool by the declared offset |
| ADR-0052 | [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) | moved | Draw arbitrary numbers within an ordinary magnitude |
| ADR-0053 | [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.md) | moved | Unify discrete generation in one ordinal space, with a dedicated engine only where the arithmetic substrate forces one |
| ADR-0054 | [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) | moved | Decide a generator's constraint surface by constructive versus rejective, not by terminality |
| ADR-0055 | [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md) | adopted | Enforce the style rules the compiler can express, and keep the DotSettings authoritative for the rest |
| ADR-0056 | [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) | adopted | State the coding rules where an agent can act on them, and check them at the edit |
| ADR-0057 | [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md) | adopted | Keep one dated line per state an ADR reached, and never overwrite one |
| ADR-0058 | [ADR-0037](0037-suppress-ca1510-while-the-netstandard-floor-stands.md) | moved | Suppress CA1510 while the pre-.NET-6 floor stands |
| ADR-0059 | [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) | moved | Guard the recipe-versus-value boundary with analyzers where the type system cannot reach it |
| ADR-0062 | [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.md) | adopted | Derive the build's Sonar rule set from the quality profile |
| ADR-0063 | [ADR-0040](0040-throw-the-library-s-own-exceptions-through-named-factories.md) | moved | Throw the library's own exceptions through named factories |
| ADR-0064 | [ADR-0041](0041-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md) | moved | Exempt the whole failure-reporting path from the null-guard convention |
| ADR-0065 | [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.md) | moved | Carry a declared constraint as a value object, not as its rendered text |
| ADR-0066 | [ADR-0043](0043-declare-a-value-object-and-enforce-its-identity.md) | moved | Declare a value object with an attribute, and enforce its identity by convention |
| — | [ADR-0044](0044-extract-justdummies-into-its-own-repository.md) | recorded here | Extract JustDummies into its own repository |
| — | [ADR-0045](0045-renumber-the-decision-base.md) | recorded here | Renumber the decision base into a contiguous sequence |
