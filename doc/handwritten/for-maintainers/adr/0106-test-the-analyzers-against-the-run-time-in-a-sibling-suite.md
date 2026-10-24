# ADR-0106 | Test the analyzers against the run time in a sibling suite

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0106-test-the-analyzers-against-the-run-time-in-a-sibling-suite.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-24
**Accepted:** 2026-10-24
**Decision Makers:** Reefact

## Context

`JustDummies.Analyzers` is tested by one project, `JustDummies.Analyzers.UnitTests`: one suite per rule, each
compiling hand-written snippets and asserting what the rule reports on them. A snippet covers only the spellings
someone thought to write.

Several rules claim that a chain they report throws at run time, and that a chain they do not report does not throw.
`JD015` makes that claim about an `Any.String()` chain: its constants "admit no value, so the chain throws a
`ConflictingAnyConstraintException` the moment the arrange line runs". The claim can be checked independently of
the rule: running the chain shows whether it throws, whatever the rule says.

The September 2026 adversarial review opened twenty-one issues. Ten were analyzer blind spots: chains that threw at
run time while no rule reported them. Each was found the same way, by running the chain to see what actually happens
and then checking the rules against the result (issue #232). That check was done by hand, and only once.

The library already splits its own tests this way. [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md)
gave `JustDummies` two sibling suites: `JustDummies.PropertyTests` for invariants quantified over generated
arguments, with FsCheck, and `JustDummies.UnitTests` for named cases. The analyzers have no such split, and their
only test project has no property-testing framework.

A generated chain has to be compiled before a rule can analyse it. That makes each case far more expensive than in
a run-time property, so such a suite runs hundreds of cases where a run-time property runs thousands.

## Decision

The analyzers gain a sibling test suite, `JustDummies.Analyzers.PropertyTests`: it generates the chains a rule claims
to judge, runs them to obtain the expected outcome, and requires the rule to report a chain exactly when the run time
refuses it, while `JustDummies.Analyzers.UnitTests` keeps the named cases.

## Rationale

**The run time is what a rule claims to predict, and asking it is cheap.** A rule that says "this chain throws" can
be checked by running the chain. Comparing the two on generated chains turns the review's one-off manual audit into
a permanent safety net: a chain the rule does not recognise fails the suite as soon as it is generated, not at the
next review. Its first runs found three `JD015` blind spots that the review had missed, and one class of refusal
that no rule covers.

**A disagreement in either direction is a finding.** A refused chain that goes unreported is the blind spot the
review kept finding. A report on a chain the run time accepts is a false positive, and it costs more: under
warnings-as-errors, it breaks a correct build. Only a comparison with the run time catches both.

**A sibling project, for the reasons ADR-0019 gave and one more.** Named cases and generated cases make different
kinds of claims, and the project a failure comes from already tells which kind broke; FsCheck also stays out of the
suite that does not need it. The further reason is cost: a suite that compiles one snippet per case needs its own
time budget, and merging it into the named-case suite would slow down every quick run while a rule is being edited.

**Each rule is checked against its own documented scope, not against everything the run time refuses.** The
generated chains cover what the rule claims to judge, and nothing more. A refusal the rule does not claim to cover,
such as declaring a second family where only one is allowed, is excluded from the generated chains and documented
with the issue that tracks it, instead of counting as a failure of the rule. The suite then catches a rule that
breaks its own promise, and a question of scope goes to a decision rather than to a test.

## Alternatives Considered

### Add the generated tests to `JustDummies.Analyzers.UnitTests`

Considered because it needs no new project and reuses the existing harness. Rejected because it mixes the two kinds
of test that ADR-0019 deliberately keeps apart, it adds FsCheck to a suite that does not use it, and it ties every
quick run of a rule's named cases to the slowest suite in the repository.

### Keep the comparison as a periodic manual audit

Considered because the review that motivated it worked. Rejected because a manual audit only finds what exists on
the day it is run. Every spelling added after that goes unchecked until the next audit, and the review showed that
this is where these defects appear.

### A symbolic model instead of the run time

Considered because issue #231 builds one for the run-time side. Rejected for this suite because the question here
is whether a rule agrees with the library, and the library itself is the authority on that. A model would add a
third party whose own errors could hide or invent a disagreement.

## Consequences

### Positive

* A rule that claims to predict a run-time refusal is checked against that claim on generated chains, in both
  directions, on every run.
* A disagreement is reported as the shortest chain that still shows it.
* Scope gaps are recorded in the generator with the issue that tracks them, so what the suite does not cover is
  documented.

### Negative

* One more test project, with its own solution entry and its own share of CI time.
* Compiling one snippet per case keeps the case count in the hundreds, so a rare disagreement may take several runs
  to surface.

### Risks

* If the generated chains cover less than a rule's real scope, the suite can pass while the rule misses chains
  outside them. The generator has to grow with the rule, as its named cases do.

## Follow-up Actions

* Extend the suite from `JD015` to the other rules that predict a refusal (`JD016`, `JD017`, `JD023`), as each gets
  a generator of its own.
* Remove the once-per-generator filter from the `JD015` generator once issue #274 is decided.

## References

* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md) — the same split for the
  library, and most of the reasoning reused here.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — why a rule's scope, not everything the
  run time does, bounds what is compared.
* [Issue #232](https://github.com/Reefact/just-dummies/issues/232) — the request and the review it came from.
* [Issue #274](https://github.com/Reefact/just-dummies/issues/274) — the conflicting declarations no rule reports
  yet.
