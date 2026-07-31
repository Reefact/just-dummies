# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited by writing a **new** ADR that supersedes the old
one, and the old one's status changes to *Superseded* with a link to its
successor.

## Where these decisions come from

This repository was extracted from
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) on
2026-07-31, and the decisions below came with the code they describe
([ADR-0068](0068-extract-justdummies-into-its-own-repository.md)). Two
consequences are visible in this index and are deliberate:

* **The numbering has gaps.** The ADRs kept the numbers they were accepted
  under. Renumbering them would have broken every cross-reference in the
  accepted texts and in the source repository, where those numbers still
  resolve. New ADRs continue from 0068 — above the highest number
  FirstClassErrors had reached — so a number never means two different
  decisions depending on which repository you are reading.
* **A few decisions live in both repositories.**
  [ADR-0011](0011-host-dummies-as-a-standalone-package.md) (the package
  identity and its no-dependency rule) and
  [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) (the .NET
  Framework 4.7.2 floor) bind both products, so both repositories carry them.
  Conversely, decisions whose *subject* is FirstClassErrors — why its testing
  package rebased on this library, why its build runs these analyzers — stayed
  there and are not listed here.

Anything an ADR here says about `Reefact/first-class-errors`, or any issue
number it cites, refers to that repository.

## When is an ADR written?

Every pull request is checked against this base — the moment new decisions enter
the codebase. Most pull requests embark no architectural decision and add no ADR;
the check is what is mandatory, not the artifact. The test for "significant": *if
the implementation changed but the decision stood, the ADR should not need
editing.* A new decision is **recorded** here, a decision that replaces another is
written as a **superseding** ADR, and a change that **conflicts** with an accepted
ADR is raised for the maintainer. The agent procedure — draft as *Proposed*, never
flip a status unilaterally — is in [`AGENTS.md`](../../../../AGENTS.md).

## An ADR is a decision record, not a specification

An ADR captures a **decision and the reasoning behind it** — not how that
decision is implemented. Implementation mechanics (code, configuration, YAML,
exact flags, XML or command snippets, guard-by-guard walkthroughs) live in the
code and in the reference documentation the ADR links to, never in the ADR
itself. In particular, **Rationale is argument, not a design document**: if a
paragraph explains *how something is built* rather than *why the decision is
right*, it belongs in the reference docs, and the ADR links to it.

## File conventions

* One decision per file, named `NNNN-kebab-case-summary.md`, numbered in the
  order decisions were recorded.
* Every ADR exists in **English and French**: `NNNN-....md` and
  `NNNN-....fr.md`, cross-linked in their header. The English file is canonical.
* The header carries **one dated line per state the decision actually reached**,
  and no date is ever overwritten — a `Proposed:` line stays when an `Accepted:`
  line is added.
* Status is one of *Proposed*, *Accepted*, *Superseded* or *Deprecated*.

## Index

| ADR | Title | Status |
|---|---|---|
| [ADR-0011](0011-host-dummies-as-a-standalone-package.md) | Host JustDummies as a standalone package in this repository | Accepted |
| [ADR-0013](0013-gate-distinct-collections-by-cardinality-else-bounded-draw.md) | Gate distinct collections by cardinality, otherwise by a bounded draw | Accepted |
| [ADR-0015](0015-cap-any-combine-at-arity-eight.md) | Cap Any.Combine at arity eight | Accepted |
| [ADR-0020](0020-materialize-dummies-only-through-generate.md) | Materialize dummies only through Generate() | Accepted |
| [ADR-0022](0022-floor-the-library-on-net-framework-4-7-2.md) | Floor the library's .NET Framework support at 4.7.2 | Accepted |
| [ADR-0025](0025-generate-strings-from-a-home-grown-regular-subset.md) | Generate matching strings from a home-grown regular subset | Accepted |
| [ADR-0030](0030-draw-arbitrary-strings-from-an-explicit-terminal-set.md) | Draw arbitrary strings from an explicit, terminal value set | Superseded |
| [ADR-0031](0031-name-any-factories-after-their-clr-type.md) | Name Any's scalar factories after their CLR type | Accepted |
| [ADR-0032](0032-draw-arbitrary-values-from-an-explicit-top-level-pool.md) | Draw arbitrary values from an explicit, top-level choice pool | Accepted |
| [ADR-0033](0033-meet-string-exclusions-with-a-bounded-redraw.md) | Meet string exclusions with a bounded redraw | Accepted |
| [ADR-0035](0035-enforce-structural-any-conflicts-at-compile-time.md) | Enforce structural Any conflicts at compile time, value-dependent ones at run time | Accepted |
| [ADR-0036](0036-draw-lattice-constrained-scalars-on-the-grid.md) | Draw lattice-constrained scalars on the grid | Accepted |
| [ADR-0037](0037-vary-the-datetimeoffset-offset-dimension.md) | Vary the DateTimeOffset offset dimension | Superseded |
| [ADR-0038](0038-open-the-ambient-seed-scope-to-adapters.md) | Open the ambient seed scope to test-framework adapters | Accepted |
| [ADR-0039](0039-adapt-dummies-to-xunit-v3-through-a-companion-package.md) | Adapt JustDummies to xUnit v3 through a companion package | Accepted |
| [ADR-0040](0040-split-the-justdummies-test-bed-between-example-and-property-suites.md) | Split the JustDummies test bed between an example suite and a property suite | Accepted |
| [ADR-0041](0041-draw-flag-enum-combinations-behind-an-opt-in.md) | Draw flag-enum combinations behind an opt-in | Accepted |
| [ADR-0042](0042-serialize-draws-on-a-random-source.md) | Serialize draws on a random source, and scope reproducibility to the draw sequence | Accepted |
| [ADR-0044](0044-ship-justdummies-analyzers.md) | Ship first-party JustDummies analyzers, and guard the reproducible async surface with them | Accepted |
| [ADR-0045](0045-guard-public-and-internal-arguments-against-null.md) | Guard public and internal arguments against null, enforced by a reflection convention | Superseded |
| [ADR-0047](0047-measure-justdummies-mutation-against-the-unit-suite-only.md) | Measure JustDummies mutation against the deterministic unit suite only | Accepted |
| [ADR-0048](0048-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) | Guarantee a generated regex value matches its pattern, by bounded redraw | Accepted |
| [ADR-0049](0049-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md) | Drop the JustDummies generator from the per-pull-request mutation matrix | Accepted |
| [ADR-0050](0050-let-a-size-maximum-cap-without-steering-the-draw.md) | Let a size maximum cap without steering the draw, and ceiling an explicitly demanded size | Accepted |
| [ADR-0051](0051-filter-the-datetimeoffset-pool-by-the-declared-offset.md) | Filter the DateTimeOffset pool by the declared offset | Accepted |
| [ADR-0052](0052-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) | Draw arbitrary numbers within an ordinary magnitude | Accepted |
| [ADR-0053](0053-unify-discrete-generation-in-one-ordinal-space.md) | Unify discrete generation in one ordinal space, with a dedicated engine only where the arithmetic substrate forces one | Accepted |
| [ADR-0054](0054-decide-a-constraint-surface-by-constructive-versus-rejective.md) | Decide a generator's constraint surface by constructive versus rejective, not by terminality | Accepted |
| [ADR-0058](0058-suppress-ca1510-while-the-netstandard-floor-stands.md) | Suppress CA1510 while the pre-.NET-6 floor stands | Accepted |
| [ADR-0059](0059-guard-the-recipe-versus-value-boundary-with-analyzers.md) | Guard the recipe-versus-value boundary with analyzers where the type system cannot reach it | Accepted |
| [ADR-0063](0063-throw-the-library-s-own-exceptions-through-named-factories.md) | Throw the library's own exceptions through named factories | Accepted |
| [ADR-0064](0064-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md) | Exempt the whole failure-reporting path from the null-guard convention | Accepted |
| [ADR-0065](0065-carry-a-declared-constraint-as-a-value-object.md) | Carry a declared constraint as a value object, not as its rendered text | Accepted |
| [ADR-0066](0066-declare-a-value-object-and-enforce-its-identity.md) | Declare a value object with an attribute, and enforce its identity by convention | Accepted |
| [ADR-0068](0068-extract-justdummies-into-its-own-repository.md) | Extract JustDummies into its own repository | Accepted |
