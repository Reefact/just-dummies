# Architecture Decision Records

Dated records of significant decisions — their context, the option chosen, and
the consequences. An ADR is a historical log: once accepted it is not edited in
place; a decision is revisited by writing a **new** ADR that supersedes the old
one, and the old one's status changes to *Superseded* with a link to its
successor.

## Where these decisions come from

This repository was extracted from
[`Reefact/first-class-errors`](https://github.com/Reefact/first-class-errors) on
2026-07-31 ([ADR-0044](0044-extract-justdummies-into-its-own-repository.md)).
Almost every decision below was accepted there before this repository existed,
and the **Origin** column says how each one got here:

* **moved** — the decision travelled with the code it describes. It is no longer
  in `Reefact/first-class-errors` once that repository's cleanup lands, and this
  base is its only home. 34 decisions.
* **adopted** — the decision governs this repository's build, CI or conventions,
  but its record stays live in `Reefact/first-class-errors` too, because that
  repository still applies it. Both copies are now independent: either side can
  supersede its own without touching the other. 9 decisions.
* **recorded here** — decided in this repository, on its own. 45 decisions.

The numbers are this base's own, assigned in the order the decisions were
recorded upstream ([ADR-0045](0045-renumber-the-decision-base.md)); every ADR
also carries its former number in its header, and the **Origin** column repeats
the mapping. **Commit messages were never rewritten**, so a commit message older
than 2026-07-31 cites the number the decision had in
`Reefact/first-class-errors` — `docs: draft ADR-0010 hosting Dummies as a
standalone package` is this repository's ADR-0003. The history itself was
rewritten on 2026-08-08
([ADR-0053](0053-rewrite-the-published-history-to-a-single-line.md)), which
changed every commit identifier but carried each message over byte for byte.

## References to the other repository

Some decisions here cite ADRs that stayed in `Reefact/first-class-errors` and
govern only that product — why its testing package rebased on this library, why
its build runs these analyzers, the tooling runtime floor. Those citations are
written **`ADR-00NN (first-class-errors)`** and are not in the table below. The
qualifier is load-bearing: a bare `ADR-0006` means *this* base's ADR-0006, which
is a different decision from the one that number denotes over there.

## When is an ADR written?

Every pull request is checked against this base — the moment new decisions enter
the codebase ([ADR-0002](0002-check-every-pull-request-against-the-adr-base.md)).
Most pull requests embark no architectural decision and add no ADR; the check is
what is mandatory, not the artifact. The test for "significant": *if the
implementation changed but the decision stood, the ADR should not need editing.*
A new decision is **recorded** here, a decision that replaces another is written
as a **superseding** ADR, and a change that **conflicts** with an accepted ADR is
raised for the maintainer. The agent procedure — draft as *Proposed*, never flip
a status unilaterally — is in [`AGENTS.md`](../../../../AGENTS.md).

## An ADR is a decision record, not a specification

An ADR captures a **decision and the reasoning behind it** — not how that
decision is implemented. Implementation mechanics (code, configuration, YAML,
exact flags, XML or command snippets, guard-by-guard walkthroughs) live in the
code and in the reference documentation the ADR links to, never in the ADR
itself. In particular, **Rationale is argument, not a design document**: if a
paragraph explains *how something is built* rather than *why the decision is
right*, it belongs in the reference docs, and the ADR links to it.

## File conventions

* One decision per file, named `NNNN-kebab-case-summary.md`.
* Every ADR exists in **English and French**: `NNNN-....md` and
  `NNNN-....fr.md`, cross-linked in their header. The English file is canonical.
* The header carries **one dated line per state the decision actually reached**,
  and no date is ever overwritten
  ([ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md)).
* Status is one of *Proposed*, *Accepted*, *Superseded* or *Deprecated*.
* A **supersession** is written on both sides and adds no date. The superseded
  record's status becomes `Superseded by` and a link to the successor; nothing
  else about it changes — its dates stay, and its body is never edited, because
  an accepted decision is a piece of history rather than a draft. The successor
  carries a `Supersedes` line below its header, linking back. Each side links
  its own language's twin. The worked pair is
  [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) and
  [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.md).
* A citation of a superseded record is **not** repinned. Its status line names
  the successor, which is what a reader follows; a citation only moves when what
  it claims stopped being true.
* A citation of an ADR that lives only in the other repository is qualified
  `ADR-00NN (first-class-errors)`; an unqualified number always means this base.

## Start here

One record is not about a feature but about how every feature question is answered:
[**ADR-0046 — Bound the generator's ambition, never its correctness**](0046-bound-the-generators-ambition-never-its-correctness.md).
Seven of the decisions below each bound a surface or an effort; ADR-0046 is the rule they share, and
it is the default answer to *"should the generator handle this case too?"*. Read it before the
others. Its number is late because [numbers here are stable handles, not a reading order](0045-renumber-the-decision-base.md)
and it was decided on 2026-08-01 — the index carries the order, the numbering carries identity.

## Considered, not adopted

Some options are refused on purpose, and each refusal carries the condition that would reopen it.
They are listed here so that a report from the field meets the record rather than a blank page — the
failure mode of a "not now" is that nobody remembers the option existed on the day it would matter.

| Option | Record | What would revive it |
|---|---|---|
| Roslyn's control-flow graph for guard placement | [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) | A constructor from a real codebase that the placement rule refuses and whose author minded, a second engine question needing the same graph, or exception-handler entry proving to be an ordinary edge at the Roslyn floor. The record carries the signature to match a report against. |

## Index

| ADR | Title | Status | Origin |
|---|---|---|---|
| [ADR-0001](0001-lock-the-analyzer-roslyn-floor.md) | Lock the analyzer's Roslyn floor | Accepted | adopted · FCE ADR-0001 |
| [ADR-0002](0002-check-every-pull-request-against-the-adr-base.md) | Check every pull request against the ADR base | Accepted | adopted · FCE ADR-0004 |
| [ADR-0003](0003-host-dummies-as-a-standalone-package.md) | Host JustDummies as a standalone package in this repository | Accepted | moved · FCE ADR-0011 |
| [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) | Gate distinct collections by cardinality, otherwise by a bounded draw | Accepted | moved · FCE ADR-0013 |
| [ADR-0005](0005-cap-any-combine-at-arity-eight.md) | Cap Any.Combine at arity eight | Accepted | moved · FCE ADR-0015 |
| [ADR-0006](0006-materialize-dummies-only-through-generate.md) | Materialize dummies only through Generate() | Accepted | moved · FCE ADR-0020 |
| [ADR-0007](0007-floor-the-library-on-net-framework-4-7-2.md) | Floor the library's .NET Framework support at 4.7.2 | Accepted | moved · FCE ADR-0022 |
| [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.md) | Generate matching strings from a home-grown regular subset | Accepted | moved · FCE ADR-0025 |
| [ADR-0009](0009-draw-arbitrary-strings-from-an-explicit-terminal-set.md) | Draw arbitrary strings from an explicit, terminal value set | Superseded by ADR-0033 | moved · FCE ADR-0030 |
| [ADR-0010](0010-name-any-factories-after-their-clr-type.md) | Name Any's scalar factories after their CLR type | Accepted | moved · FCE ADR-0031 |
| [ADR-0011](0011-draw-arbitrary-values-from-an-explicit-top-level-pool.md) | Draw arbitrary values from an explicit, top-level choice pool | Accepted | moved · FCE ADR-0032 |
| [ADR-0012](0012-meet-string-exclusions-with-a-bounded-redraw.md) | Meet string exclusions with a bounded redraw | Accepted | moved · FCE ADR-0033 |
| [ADR-0013](0013-require-a-scope-on-the-version-driving-commit-types.md) | Require a scope on the version-driving commit types | Accepted | adopted · FCE ADR-0034 |
| [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.md) | Enforce structural Any conflicts at compile time, value-dependent ones at run time | Accepted | moved · FCE ADR-0035 |
| [ADR-0015](0015-draw-lattice-constrained-scalars-on-the-grid.md) | Draw lattice-constrained scalars on the grid | Accepted | moved · FCE ADR-0036 |
| [ADR-0016](0016-vary-the-datetimeoffset-offset-dimension.md) | Vary the DateTimeOffset offset dimension | Superseded by ADR-0030 | moved · FCE ADR-0037 |
| [ADR-0017](0017-open-the-ambient-seed-scope-to-adapters.md) | Open the ambient seed scope to test-framework adapters | Accepted | moved · FCE ADR-0038 |
| [ADR-0018](0018-adapt-dummies-to-xunit-v3-through-a-companion-package.md) | Adapt JustDummies to xUnit v3 through a companion package | Accepted | moved · FCE ADR-0039 |
| [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md) | Split the JustDummies test bed between an example suite and a property suite | Accepted | moved · FCE ADR-0040 |
| [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.md) | Draw flag-enum combinations behind an opt-in | Accepted | moved · FCE ADR-0041 |
| [ADR-0021](0021-serialize-draws-on-a-random-source.md) | Serialize draws on a random source, and scope reproducibility to the draw sequence | Accepted | moved · FCE ADR-0042 |
| [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.md) | Gate pull requests on the mutation score of what they changed | Accepted | adopted · FCE ADR-0043 |
| [ADR-0023](0023-ship-justdummies-analyzers.md) | Ship first-party JustDummies analyzers, and guard the reproducible async surface with them | Accepted | moved · FCE ADR-0044 |
| [ADR-0024](0024-guard-public-and-internal-arguments-against-null.md) | Guard public and internal arguments against null, enforced by a reflection convention | Superseded by ADR-0041 | moved · FCE ADR-0045 |
| [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md) | Make the per-pull-request mutation gate advisory | Accepted | adopted · FCE ADR-0046 |
| [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.md) | Measure JustDummies mutation against the deterministic unit suite only | Accepted | moved · FCE ADR-0047 |
| [ADR-0027](0027-guarantee-a-generated-regex-value-matches-by-bounded-redraw.md) | Guarantee a generated regex value matches its pattern, by bounded redraw | Accepted | moved · FCE ADR-0048 |
| [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md) | Drop the JustDummies generator from the per-pull-request mutation matrix | Accepted | moved · FCE ADR-0049 |
| [ADR-0029](0029-let-a-size-maximum-cap-without-steering-the-draw.md) | Let a size maximum cap without steering the draw, and ceiling an explicitly demanded size | Superseded by ADR-0076 | moved · FCE ADR-0050 |
| [ADR-0030](0030-filter-the-datetimeoffset-pool-by-the-declared-offset.md) | Filter the DateTimeOffset pool by the declared offset | Accepted | moved · FCE ADR-0051 |
| [ADR-0031](0031-draw-arbitrary-numbers-within-an-ordinary-magnitude.md) | Draw arbitrary numbers within an ordinary magnitude | Accepted | moved · FCE ADR-0052 |
| [ADR-0032](0032-unify-discrete-generation-in-one-ordinal-space.md) | Unify discrete generation in one ordinal space, with a dedicated engine only where the arithmetic substrate forces one | Accepted | moved · FCE ADR-0053 |
| [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) | Decide a generator's constraint surface by constructive versus rejective, not by terminality | Accepted | moved · FCE ADR-0054 |
| [ADR-0034](0034-enforce-the-style-rules-the-compiler-can-express.md) | Enforce the style rules the compiler can express, and keep the DotSettings authoritative for the rest | Accepted | adopted · FCE ADR-0055 |
| [ADR-0035](0035-state-the-coding-rules-where-an-agent-can-act-on-them.md) | State the coding rules where an agent can act on them, and check them at the edit | Accepted | adopted · FCE ADR-0056 |
| [ADR-0036](0036-keep-one-dated-line-per-state-an-adr-reached.md) | Keep one dated line per state an ADR reached, and never overwrite one | Accepted | adopted · FCE ADR-0057 |
| [ADR-0037](0037-suppress-ca1510-while-the-netstandard-floor-stands.md) | Suppress CA1510 while the pre-.NET-6 floor stands | Accepted | moved · FCE ADR-0058 |
| [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) | Guard the recipe-versus-value boundary with analyzers where the type system cannot reach it | Accepted | moved · FCE ADR-0059 |
| [ADR-0039](0039-derive-the-build-rule-set-from-the-quality-profile.md) | Derive the build's Sonar rule set from the quality profile | Accepted | adopted · FCE ADR-0062 |
| [ADR-0040](0040-throw-the-library-s-own-exceptions-through-named-factories.md) | Throw the library's own exceptions through named factories | Accepted | moved · FCE ADR-0063 |
| [ADR-0041](0041-exempt-the-whole-failure-reporting-path-from-the-null-guard-convention.md) | Exempt the whole failure-reporting path from the null-guard convention | Accepted | moved · FCE ADR-0064 |
| [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.md) | Carry a declared constraint as a value object, not as its rendered text | Accepted | moved · FCE ADR-0065 |
| [ADR-0043](0043-declare-a-value-object-and-enforce-its-identity.md) | Declare a value object with an attribute, and enforce its identity by convention | Accepted | moved · FCE ADR-0066 |
| [ADR-0044](0044-extract-justdummies-into-its-own-repository.md) | Extract JustDummies into its own repository | Accepted | recorded here |
| [ADR-0045](0045-renumber-the-decision-base.md) | Renumber the decision base into a contiguous sequence | Accepted | recorded here |
| [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) | Bound the generator's ambition, never its correctness | Accepted | recorded here |
| [ADR-0047](0047-declare-the-adapters-library-dependency-independently.md) | Declare the adapter's library dependency independently of the version it is packed at | Accepted | recorded here |
| [ADR-0048](0048-publish-only-from-a-commit-that-is-on-main.md) | Publish only from a commit that is on main | Accepted | recorded here |
| [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) | Replay a seed across patch and minor versions | Accepted | recorded here |
| [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) | Name a suppressed rule through a catalogue constant, not a string literal | Accepted | recorded here |
| [ADR-0051](0051-land-pull-requests-by-rebase.md) | Land pull requests by rebase | Accepted | recorded here |
| [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.md) | Publish the JD rules as a first-party catalogue, and read the descriptors from it | Accepted | recorded here |
| [ADR-0053](0053-rewrite-the-published-history-to-a-single-line.md) | Rewrite the published history to a single line, and carry the release tags onto it | Accepted | recorded here |
| [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) | Draw only valid values from a typed builder, and judge nothing in a caller-supplied pool | Accepted | recorded here |
| [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) | Hold the user documentation to contracts the build checks | Accepted | recorded here |
| [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) | Scaffold the generator once and hand the file to the developer | Accepted | recorded here |
| [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) | Make the emitted generator a first-class `IAny<T>` | Accepted | recorded here |
| [ADR-0058](0058-leave-the-scaffolded-file-open-to-the-analyzers.md) | Leave the scaffolded file open to the JustDummies analyzers | Accepted | recorded here |
| [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) | Emit only members resolved in the target compilation | Accepted | recorded here |
| [ADR-0060](0060-seed-generators-from-constructor-guards.md) | Seed generators from constructor guards, and leave the rest as a compile error | Accepted | recorded here |
| [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.md) | Draw from the ambient context and hold no state | Accepted | recorded here |
| [ADR-0062](0062-emit-the-generator-into-the-target-types-namespace.md) | Emit the generator into the target type's namespace | Accepted | recorded here |
| [ADR-0063](0063-give-the-scaffolder-no-dependency-on-the-package.md) | Give the scaffolder no dependency on the JustDummies package | Accepted | recorded here |
| [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) | Never draw null for a nullable parameter | Accepted | recorded here |
| [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md) | Keep the scaffolding engine loadable by a Roslyn host | Accepted | recorded here |
| [ADR-0066](0066-load-msbuild-from-the-sdk-never-from-the-tool.md) | Load MSBuild from the installed SDK, never from the tool's own files | Accepted | recorded here |
| [ADR-0067](0067-report-a-filtered-pool-through-an-explicit-interface.md) | Report a filtered pool through an explicitly implemented interface, and warn about nothing | Accepted | recorded here |
| [ADR-0068](0068-carry-the-pool-inspection-wherever-a-caller-supplies-the-values.md) | Carry the pool inspection wherever a caller supplies the values, and nowhere else | Accepted | recorded here |
| [ADR-0069](0069-answer-a-cardinality-bound-under-the-comparer-that-will-use-it.md) | Answer a cardinality bound under the comparer that will use it | Accepted | recorded here |
| [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md) | Emit an entry point on request, as a file of its own | Accepted | recorded here |
| [ADR-0071](0071-report-a-run-as-data-without-moving-the-exit-codes.md) | Report a run as data without moving the exit codes | Accepted | recorded here |
| [ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.md) | Read project defaults from a file the command line overrides | Accepted | recorded here |
| [ADR-0073](0073-layer-the-agent-instructions-by-when-they-are-needed.md) | Layer the agent instructions by when they are needed | Accepted | recorded here |
| [ADR-0074](0074-draft-a-releases-github-notes-by-hand-and-refuse-without-them.md) | Draft a release's GitHub notes by hand from the changelog, and refuse without them | Accepted | recorded here |
| [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md) | Draw characters from the whole of ASCII, and narrow only by a named family | Accepted | recorded here |
| [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.md) | Let a declared maximum steer the size draw | Accepted | recorded here |
| [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) | Admit a JD rule that reports a correct spelling, bounded by an exact named equivalent | Superseded by ADR-0080 | recorded here |
| [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.md) | Own a bound declared twice as one rule, and narrow JD024 out of it | Accepted | recorded here |
| [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) | Constrain what a dummy draws, never the literals it was given | Accepted | recorded here |
| [ADR-0080](0080-admit-a-rule-that-names-a-resolved-ambiguity.md) | Admit a JD rule that names an ambiguity the library resolves, alongside the shorter equivalent | Accepted | recorded here |
| [ADR-0081](0081-declare-one-top-level-type-per-file.md) | Declare one top-level type per file, enforced by a third-party style analyzer | Accepted | recorded here |
| [ADR-0082](0082-answer-for-the-finished-chain-not-each-constraint.md) | Answer for the finished chain, not for each constraint read | Accepted | recorded here |
| [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) | Block compilation on a guard the engine cannot vouch for | Accepted | recorded here |
| [ADR-0084](0084-place-a-guard-by-syntax-reach-not-a-control-flow-graph.md) | Place a guard by syntax reach, not by a control-flow graph | Accepted | recorded here |
| [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) | Change the guard reader only against a report from the field | Accepted | recorded here |
| [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.md) | Read the guard helpers of named libraries, in both their spellings | Accepted | recorded here |
| [ADR-0087](0087-check-a-documented-count-against-its-source-not-its-translation.md) | Check a documented count against its source, not against its translation | Accepted | recorded here |
| [ADR-0088](0088-state-the-whitespace-guard-with-a-member-of-its-own.md) | State the whitespace guard with a member of its own | Accepted | recorded here |
