# ADR-0092 | Run every mutation leg from its own source project

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0092-run-every-mutation-leg-from-its-own-source-project.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-31
**Accepted:** 2026-09-02
**Decision Makers:** Reefact

## Context

A mutation verdict is an answer about an oracle: *does any test in this suite fail on this
rewrite?* Change the suite and the same mutant changes verdict, so which tests judge is not a
performance setting — it is what the score means.

[ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.md) established the gate and
gave each component a Stryker configuration naming the project to mutate and the test projects that
must kill its mutants. [ADR-0026](0026-measure-justdummies-mutation-against-the-unit-suite-only.md)
then narrowed the library's oracle to its deterministic example suite, removing the FsCheck property
suite on two grounds it states: a randomised oracle makes a mutant killable on one run and surviving
on the next, and a hundred cases per property is the slow half of every mutant.

Those configurations also name the solution. Measured on 2026-08-31 against the pinned engine
(4.16.0), that field decides the oracle and the declaration does not: Stryker discovers the test
projects itself — every project in the solution referencing the mutated assembly — and never reads
the list. The library's run reported **2119 tests**, which is every suite in the repository, where
its configuration names one of **790**. Nothing warns.

So ADR-0026 has never been in effect. The commit that removed the property suite from the list landed
the same day the configuration was created, on a file that already named the solution, and removed
nothing — the decision was never in force for a single run. The property suite has judged every
library mutant since, and the seed-dependence that decision exists to remove has been present
throughout.

Three narrowing mechanisms were measured and none is a remedy: the command-line test-project
override leaves the count unchanged; the test-case filter is accepted and silently ignored under the
MTP runner, a filter matching no test at all still producing the same score on the same mutants; and
a solution filter file, which MSBuild builds without complaint, makes Stryker abort. Running the
engine from the mutated project's own directory, with no solution named, is the one form under which
the declared list is the oracle.

Nothing gates on a score today. Two of the three components carry no threshold, the weekly sweep
disables its own, and the per-pull-request check has been advisory since
[ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md).

## Decision

Every mutation leg runs from the directory of the project it mutates, no Stryker configuration names
a solution, and a test in the example suite fails the build if one does.

## Rationale

* **A measurement whose oracle is not the declared one is not a measurement.** The scores published
  since ADR-0026 answer a question nobody asked, and a reader has no way to tell: the configuration
  says one thing, the run does another, and the two never meet. Correctness is the property this
  repository refuses to bound, and an instrument that misreports is the same defect one level up.
* **It costs nothing to fix now, and more every week.** With no threshold to trip, the scores can
  move freely; the day a bar is set from a published figure, that figure will have been measured
  with the wrong oracle and the correction becomes a change that fails pull requests. Meanwhile each
  weekly sweep publishes another trend that is not the one it is read as.
* **The working directory is enforceable where the declaration is not.** The list is inert in a
  solution context and no option overrides it, so honouring the declaration is not a matter of
  writing it more carefully — it requires the invocation that reads it. A convention held only by
  care is how this was lost the first time, which is why the test is part of the decision rather
  than a precaution beside it.
* **It restores ADR-0026 rather than replacing it.** That decision was right and is untouched: the
  oracle is the deterministic example suite. What was missing was any means of knowing it had not
  taken effect.

## Alternatives Considered

### Correct the declaration to match what runs

Considered because it is honest and costs nothing: delete the inert lists, and record that the
oracle is every suite referencing the mutated assembly. Rejected because it keeps the defect and
merely documents it. The property suite would go on making verdicts seed-dependent, which is the
harm ADR-0026 identified; and a repository that answers a broken instrument by rewriting the label
has decided its measurements are decorative.

### Raise the pinned engine and hope a newer one reads the list

Considered because the behaviour may well be fixed upstream. Rejected as a remedy here: a newer
engine invents new mutants and moves every score on its own, so it cannot be introduced as part of a
change whose whole purpose is to make one number trustworthy. It is a separate decision, and if it
lands, the working directory costs nothing to keep.

### Make the declaration true by shrinking the solution

Considered because a solution filter naming only the mutated project and its suite would narrow
discovery at the source, keeping the invocation unchanged. Rejected on measurement: Stryker refuses
such a file outright, aborting where MSBuild builds it without complaint.

## Consequences

### Positive

* ADR-0026 takes effect: the library's verdicts come from a deterministic suite, so the same commit
  scores the same twice running.
* Every mutant is judged by a suite two to three times smaller, which is the per-mutant cost.
* The configurations become readable as what they are — a component and the suite answerable for it.

### Negative

* Every historical score is superseded. Figures published before this decision were measured against
  a different oracle and are not comparable with what follows.
* A leg is one step further from the plain repository-root invocation a reader might expect, and the
  reason lives in the workflow's header rather than in the command.

### Risks

* **A component whose suite is genuinely not enough.** Narrowing the oracle can only lower a score,
  and a component relying on a sibling suite to kill its mutants will show survivors it did not show
  before. That is the instrument working: the survivor was always there, judged by a test that
  belongs to another component. The one component carrying a real threshold is unaffected — nothing
  but its own suite referenced it — and it was measured clearing that bar after the change.

## Follow-up Actions

* Revisit [ADR-0028](0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md).
  It dropped the generator's per-pull-request leg on a measured cost, and that cost was measured with
  every suite in the repository judging each mutant. The oracle is now a fraction of that, so the
  premise has moved and the leg may be affordable again. This decision does not reinstate it: that is
  ADR-0028's to reopen, on a fresh measurement.
* Read the first sweep published after this change as a new baseline, not as a regression against the
  old figures.

## References

* ADR-0022 — Gate pull requests on the mutation score of the diff: the decision that gave each component a configuration.
* ADR-0025 — Make the per-pull-request mutation gate advisory: why no score gates today.
* ADR-0026 — Measure JustDummies mutation against the deterministic unit suite only: the decision this makes effective.
* ADR-0028 — Drop the JustDummies generator from the per-pull-request mutation matrix: the cost model this moves.
* [`workflows/justdummies-mutation.en.md`](../workflows/justdummies-mutation.en.md) — how the legs are wired, and the measurements behind this record.
