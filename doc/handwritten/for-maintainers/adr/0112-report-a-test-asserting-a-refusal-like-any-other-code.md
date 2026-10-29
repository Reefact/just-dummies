# ADR-0112 | Report a test asserting a refusal like any other code, and let it suppress the rule explicitly

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0112-report-a-test-asserting-a-refusal-like-any-other-code.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Context

ADR-0038 bounded the cost of the JD rules partly by scoping them so that "a conflict-asserting negative test" stays
silent, to keep them usable in a suite that tests the library's own failure behaviour. Twenty rules implemented that
scope with one shared heuristic: a call that is the whole body of a lambda passed as an argument, as in
`Check.ThatCode(() => Any.Int32().Between(10, 5))`, was treated as a test asserting a throw, and not reported.

The heuristic reads the shape of the code, not its intent, and it is wrong both ways. `Task.Run(() => chain.Generate())`
or a lambda in a tuple has the same shape and is arrange code, yet was silent; until #346,
`Select(_ => chain.Generate())` was silent too. FluentAssertions' `generator.Invoking(g => g.Between(10, 5))` is a
negative test, yet is reported since #346 narrowed the shape to parameterless lambdas. Each correction moved the
boundary without making it right.

The tests that assert a JustDummies refusal are, overwhelmingly, this repository's own: removing the heuristic
surfaces 455 diagnostics, all in `JustDummies.UnitTests` and `JustDummies.PropertyTests`, all on chains written to be
refused. A consumer tests their own code, where an impossible chain is a mistake. These chains throw on every run, so a
missed diagnostic is caught the first time the test runs; a reported one on a deliberate negative test costs one
justified suppression. The .NET analyzers in common use — the platform's `CA` rules, xUnit's and NUnit's own — do not
try to recognise negative tests either; a project silences a rule where it means to, locally or by configuration.

This repository treats a change to the semantics of a diagnostic id as an architectural decision.

## Decision

No JD rule tries to recognise a test asserting a refusal: such a test is reported like any other code, and silences
the rule on the test method with a justified `[SuppressMessage]`.

## Rationale

**The intent is stated, not guessed.** A `[SuppressMessage]` on the test method says, in the test, that the refusal is
the subject, and its justification says why. A heuristic says nothing a reader can see, and guesses wrong in both
directions; every refinement so far traded one residue for another.

**It costs the party that writes such tests.** Almost every negative test of a JustDummies refusal lives in this
repository. Paying for them with suppressions here keeps the rules exact for consumers, who were the ones paying for
the heuristic's silent misses.

**Being wrong is cheap in this direction.** The chains these rules report throw every time they run. A diagnostic on
a deliberate negative test is one line to silence; a missed one on arrange code was a red test discovered later. The
argument ADR-0038 made for cheap suppressions applies here as it did there.

**It is how analyzers usually behave.** A reader who knows the platform's rules expects a diagnostic to mean the same
thing in a test as anywhere else, and to silence it the same way.

## Alternatives Considered

### Keep refining the shape

Considered because each refinement fixed a real case: a parameterless lambda, a receiver chain climbed to the outermost
call.

Rejected because no syntactic shape carries intent. Every narrowing left a residue on one side, and every widening on
the other.

### Disable the rules per test project, by configuration

Considered because it is a few lines instead of one attribute per test method.

Rejected because it switches the rules off everywhere in those projects, including on arrange code they would catch,
while a method-level suppression silences exactly the test that means it.

## Consequences

### Positive

* Twenty rules lose their exemption, and the shared heuristic is deleted.
* A diagnostic means the same thing wherever it appears; a page no longer has to describe which lambda shapes are
  silent.

### Negative

* This repository's suites carry one `[SuppressMessage]` per test method that asserts a refusal: 261 at the time of
  this decision.
* A consumer who writes a negative test of a JustDummies refusal now sees the diagnostic, and must suppress it.

### Risks

* A consumer whose build treats warnings as errors and holds such tests turns red on upgrade. The changelog says so,
  and names the suppression.

## Follow-up Actions

* None.

## References

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — the scoping this decision revises.
* [ADR-0050](0050-name-a-suppressed-rule-through-a-catalogue-constant.md) — how a suppression names its rule.
* Pull requests [#345](https://github.com/Reefact/just-dummies/pull/345) and [#346](https://github.com/Reefact/just-dummies/pull/346), whose reviews found the heuristic's residues.
