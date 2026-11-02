# ADR-0129 | Report a chain whose constants admit exactly one value, over every exact type

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0129-report-a-chain-whose-constants-admit-exactly-one-value.fr.md)

**Status:** Proposed
**Proposed:** 2026-11-02
**Decision Makers:** Reefact

## Context

The library's second fundamental is that what is arbitrary becomes visibly arbitrary and what is significant stays
a literal. A generator whose constant constraints admit exactly one value — `Any.Int32().Between(5, 5)`,
`GreaterThanOrEqualTo(x).LessThanOrEqualTo(x)`, a `OneOf` with a single value — draws a constant dressed as a draw:
the value the test chose, written as if the test did not care about it. ADR-0097's rationale calls that "the
hand-picked constant this library exists to replace". No rule reported it: JD023 fires on an empty domain only, and
its folding state treated a single-value chain as legal.

JD023 itself covered the integer generators only, as its page stated, so a crossed constant range on another exact
type — `Any.Decimal().GreaterThan(100m).LessThan(10m)`, `Any.DateOnly().After(later).Before(earlier)` — compiled
and threw on every run. Issue #356 decided to extend it to the types whose comparison is exact: `decimal`, and
`DateOnly`, `DateTime`, `DateTimeOffset`, `TimeSpan` and `TimeOnly`, which compare by ticks or day numbers. The
binary floating-point families stay out, since neighbours and rounding do not fold like integers. On `TimeOnly`,
the rule follows the window across midnight (ADR-0125): a constant bound outside a window that crosses midnight is a
chain that admits no value.

An information diagnostic does not appear in `dotnet build` output (issue #382), so a rule at that severity teaches
nothing where a build is read. `Any.Fixed` returns a literal through a generator where one is required (ADR-0118).
`Between(t, t)` on a time of day stays the single value `t`, and the whole day is `Any.TimeOnly()` (ADR-0125).

The library's own test suites draw single-valued chains on purpose: there the drawn value is the subject under
test, not a dummy, as the repository's testing guide says.

## Decision

A warning in the constraints family, JD038, reports a chain over an exact type whose constant constraints admit
exactly one value, reusing JD023's folding — which now covers `decimal` and the temporal generators — and names the
way out: write the literal, or `Any.Fixed` where a generator is required, and on a time of day `Any.TimeOnly()` if
the whole day was meant.

* It reports `Between(a, a)`, a floor and a ceiling that meet, a lattice with one point left in its range, and a
  value set reduced to one value, on the integer, decimal and temporal generators.
* A value pinned by name, `Zero()`, is not reported: it already reads as the constant it is.
* A bound computed from another draw is not foldable and is not reported.
* There is no code fix.
* The library's own suites decline the rule, since the drawn value is their subject.

## Rationale

**The mistake is the one the library exists to prevent.** A single-valued chain looks like a draw and is a
literal: a reader cannot tell it from a dummy, which is exactly what the second fundamental says must be visible.
Reporting it keeps the call site honest about what matters.

**One folding, two questions.** JD023 already rebuilds the domain constraint by constraint; asking how much is
left, rather than whether anything is, costs no new reading and cannot disagree with the emptiness verdict. That is
also why the extension to the exact types is decided here: the same walk answers both questions on every type it
reads, and a type it reads for one it reads for the other.

**Exact types only.** Integers, `decimal` and the temporal types compare exactly, so a bound, a lattice and an
exclusion fold to the same values the run time sees. A binary floating-point bound does not: two decimal literals
may be one `float`, and a strict bound's neighbour depends on the magnitude. The rule is bounded where its
certainty ends (ADR-0046).

**A warning, because there is no correct spelling to tolerate.** An inert exclusion has a defensible reading, which
is why JD024 is information; a constant range reduced to one point has none, and information is invisible in a build.

**Zero() by name.** `Zero()` says what it does; a reader does not mistake it for a draw. Reporting it would turn a
documented idiom into noise, and the rule's purpose is the constant that hides.

**Named ways out, no code fix.** A literal changes the call site's type, `Any.Fixed` keeps it a generator, and on a
time of day `Between(t, t)` may have meant the whole day: which applies is the developer's to say.

## Alternatives Considered

### Information severity

Considered because the chain draws and the test passes. Rejected: the severity is invisible in a build, and the
rule has no case where the reported spelling is the one to keep, which is the distinction ADR-0077 draws.

### Extend JD023's message rather than add a rule

Considered because the folding is shared. Rejected: JD023 reports a chain that throws, and a consumer who silences
it must not silence a chain that draws; one id per fact keeps a suppression honest.

### Report `Zero()` too

Considered for consistency: it admits one value. Rejected: the pin is named, the reader is not misled, and the
library documents the call as the idiom for the empty case.

### A code fix replacing the chain with the literal

Considered because the rewrite is mechanical. Rejected: it changes the call site's type, and on a time of day it
cannot know whether the day was meant.

## Consequences

### Positive

* A constant dressed as a draw is reported where it is written, on every exact type.
* Crossed constant ranges on `decimal` and the temporal generators are reported at build time, as the integer ones were.

### Negative

* A test outside this repository that draws a single-valued chain as its subject reports a warning it must suppress
  at the site, or decline for its suite as this repository's suites do.

### Risks

* A single value reached by excluding a wide range one value at a time is not counted, and not reported: the walk
  is bounded as JD023's is.

## Follow-up Actions

* The developer-UX trial corpus draws single-valued chains in its probes on purpose; the next trial decides whether
  those probes suppress the rule or decline it.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the rule is bounded where its certainty ends.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — when a rule reports a correct spelling.
* [ADR-0097](0097-follow-the-declared-bounds-with-the-ordinary-magnitude-window.md) — the hand-picked constant this library exists to replace.
* [ADR-0118](0118-return-a-literal-through-any-fixed.md) — `Any.Fixed`.
* [ADR-0125](0125-let-a-time-of-day-window-cross-midnight.md) — the window across midnight, and `Between(t, t)`.
* Issues #356 and #382; AN-C2 of the first developer-UX trial.
