# ADR-0093 | Publish mutation statuses, not a score

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0093-publish-mutation-statuses-not-a-score.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-01
**Accepted:** 2026-09-02
**Decision Makers:** Reefact

## Context

ADR-0022 made mutation testing a diff-scoped check on every pull request, backed by a weekly
full sweep. ADR-0025 made the per-pull-request check advisory and rested the enforcement on
that sweep: *"the weekly sweep is the real signal."* Every score threshold in
`build/stryker/` was set the same way — from a component's measured full-sweep score,
rounded down — and the library's own threshold was deliberately left at zero, waiting for
the first sweep that would measure it.

That sweep completed on **2026-09-01**, the first ever carried to the end for the library.
It reported **100 %**. Of 4575 judged mutants, **2070 were killed by a failing test and
2505 ended in a timeout**, which Stryker scores as a kill; nothing survived because more
than half the component was never judged. The timeouts are not concentrated in code that
loops: they are spread across every file of the component.

The cause is measured and ordinary. The tool calibrates its per-mutant time budget from an
initial run of the test suite **alone**, then runs mutants in concurrent sessions. On this
repository's suite, four concurrent sessions each take about **twice** as long as one
running alone. The limit is therefore fixed under one set of conditions and applied under
another, and a session can exceed it before the mutant has done anything.

A `Timeout` counted as a kill inverts the signal: the more loaded the machine, the higher
the score. Nothing in the tool's output invites doubt — it does not report the budget it
used, and the run summary's own wording was "all mutants were killed".

A second observation bears on the same question and has **no identified cause**. On
`JustDummies.GenAny/Guards.cs`, at one commit and with the same declared oracle of 495
tests read from both logs, the CI runner reported 38 survivors and a Linux container 52:
seventeen mutants the runner called killed survive in the container. For one of them,
applying the mutation to the source by hand leaves the whole suite green, under both SDK
versions involved. Nine explanations were eliminated by measurement — the tool's own
concurrency and the SDK difference among them — and none accounts for it. Two identical
local runs agree to two mutants out of 623.

Nothing in this repository gates on a mutation score today: every configuration carries
`break: 0` and the sweep disables the threshold by construction.

## Decision

The repository publishes mutation results as **counts by status**, never as a score, and no
threshold is ever set from a run whose non-verdict statuses are not a small residue.

## Rationale

* **A timeout is not evidence that anything noticed the change.** It is the absence of a
  verdict within a budget, and here the budget was wrong for reasons that have nothing to do
  with the code under test. Folding it into "killed" is not a rounding error, it is a
  category error, and it is the one that turned "half the component was never judged" into
  "100 %".
* **The failure direction is what makes it dangerous.** A defect that makes a number
  pessimistic is eventually noticed because it obstructs; this one moves the figure in the
  direction everybody wants to believe, and it moves with machine load rather than with the
  suite. A number that cannot go down for a bad reason cannot be read at all.
* **It costs nothing to stop publishing a score, because nothing consumes one.** No check
  fails on a mutation figure, so this decision removes no enforcement — it removes a claim.
  What replaces it is strictly more information: the counts a score was computed from, which
  a reader can combine themselves once they know what each status means.
* **The threshold rule follows from the same fact.** The method every existing bar was set
  by — take the measured score, round down — presumes the measurement. Applied to this
  sweep it would have pinned the library's ratchet to an artefact and made every later run
  look like a regression against a number that never existed. Naming the precondition is
  cheaper than discovering it again.
* **The unexplained divergence argues for the same shape rather than against it.** Its cause
  is unknown and may stay unknown for a while; a report that publishes what it observed,
  status by status, survives that ignorance, where a single number that silently averages
  over it does not.
* **This does not weaken ADR-0025, it repairs the premise it rests on.** That decision moved
  the enforced signal onto the weekly sweep. The sweep still is that signal — the decision
  here is about what the sweep is allowed to claim, so that when a bar is eventually set it
  is set from something measured.

## Alternatives Considered

### Fix the time budget and keep publishing a score

Considered because the timeout inflation has a known, ordinary cause, and correcting the
budget makes the number defensible again for that component.

Rejected as a substitute rather than as a complement: correcting a budget is an estimate
that has to be maintained per component and per machine, and the second observation shows
the figure can be wrong for reasons no budget addresses.

The correction was then measured rather than assumed, on one file of 205 mutants. The
default budget leaves 173 timeouts against 32 kills; ten extra seconds changes nothing at
all; thirty extra seconds turns 112 of those timeouts into real kills and costs 2.8 times
the wall clock. So the fix is real — most of those mutants were caught by a failing test
and never got to say so — and no affordable setting delivers it: the value that works
projects the library's leg past the cap its job runs under. Splitting that leg across jobs
is the lever, not a number, and it is a larger change than this decision. Which is the
point: publishing the statuses does not wait on it.

### Count a timeout as a survivor instead of a kill

Considered because it fails safe: a mutant with no verdict is treated as one nobody caught,
so the number can only understate.

Rejected because it is the same category error with the sign flipped, and it would report a
component as full of holes on a day the runner was busy. A mutant that genuinely never
terminates *is* detected, and calling that a survivor is as false as calling a
harness-induced timeout a kill. Neither answer is available without knowing which of the two
happened, which is exactly what publishing the status preserves and a score destroys.

### Say nothing until the cause of the divergence is found

Considered because a report that says "these numbers are partly unexplained" is
uncomfortable, and the temptation is to wait for a clean story.

Rejected: the weekly sweep publishes a figure every Monday whether or not anyone has
explained it. Waiting does not suspend the claim, it only leaves the wrong one standing.

## Consequences

### Positive

* A reader sees what was measured — how many mutants a test caught, how many had no verdict
  — instead of a single figure that cannot distinguish them.
* The library's threshold stops waiting on the wrong event. It waits on a sweep whose
  timeouts are a residue, which is a condition someone can check.
* A run dominated by non-verdicts announces itself instead of reading as a perfect score.

### Negative

* No single number to put in a trend line or a badge. Following the repository's mutation
  posture over time now takes reading a small table rather than one figure.
* Two of the four components have never been measured under conditions where their score
  would mean anything, so the honest answer to "how well is this tested?" is longer than it
  was.

### Risks

* **A count can be over-read exactly like a score was.** "2070 killed" invites the same
  false precision if the reader forgets that a mutant may be equivalent, or that the diff
  scope changed. The statuses are more honest, not self-explanatory.
* **The unexplained divergence stays unexplained.** This decision makes a report describe
  its own run faithfully; it does not make two runs agree, and nothing here should be read
  as having closed that question.

## Follow-up Actions

* Split the library's sweep across jobs. The per-mutant budget is measured, and no value of
  it is both effective and affordable inside one job; parallelism across runners is what
  would let the budget be honest.
* Re-open the question of what the library's threshold should be once a sweep produces a
  figure whose timeouts are a residue.
* Keep the `Guards.cs` divergence open as a question rather than an incident: it is
  reproducible in both directions and the arbiter is a mutation applied by hand.

## References

* [ADR-0022](0022-gate-pull-requests-on-the-mutation-score-of-the-diff.md) — the check this
  decision reports through.
* [ADR-0025](0025-make-the-per-pull-request-mutation-gate-advisory.md) — the decision that
  moved the enforced signal onto the weekly sweep.
* [ADR-0092](0092-run-every-mutation-leg-from-its-own-source-project.md) — the previous
  repair to the same instrument, and the reason its oracle is now the declared one.
* [`justdummies-mutation.en.md`](../workflows/justdummies-mutation.en.md) — what each leg
  runs, what the summary publishes, and the measurements this record argues from.
