# ADR-0105 | Accept an undeclared value in an enum exclusion as a no-op

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0105-accept-an-undeclared-enum-exclusion-as-a-no-op.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-11
**Accepted:** 2026-10-14
**Decision Makers:** Reefact

Narrows [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) on one point — the argument-side
rule for the enum exclusions — as [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.md) did for the
floating-point ones. Everything else ADR-0054 decided stands.

## Context

[ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) decided that *"a typed builder draws, and
accepts as an argument, only valid values of the domain it represents"*. `Any.Enum<T>()` is such a builder:
it draws a member its type declares, or, on a `[Flags]` enum, a combination of declared members where the
caller asks for one ([ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.md)).

The builder applies that rule to `OneOf` and to nothing else. `OneOf` refuses a value the type does not define
with an `ArgumentException`. `Except` and `DifferentFrom` take the value as given: an undeclared one can match no
candidate the builder would draw, so the values produced are the same whether it is carried or not. The run time
accepts such an exclusion as a no-op.

`JD017` judged the three constraints alike. It reported `Any.Enum<Day>().Except((Day)99)`, and
`Any.Enum<Status>().DifferentFrom(default)` on an enum with no zero member, as values outside the universe — a
warning, an error under a warnings-as-errors build. The analyzer refused at build time a chain the run time
honours on every seed ([#228](https://github.com/Reefact/just-dummies/issues/228)). The suite that keeps the rule
and the builder in step asked the builder only through `OneOf`, so nothing compared the two on an exclusion.

An undeclared enum value is reached through a numeric cast or through `default`, the zero every enum has
whether it declares one or not. A misspelt member name does not compile. `DifferentFrom` is documented as
taking *"typically an existing value the test already holds"*, and an enum value a test holds may be `default`.

[ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.md) settled the same question for the floating-point
builders. It kept a non-finite bound and `OneOf` value refused, and accepted a non-finite value in `Except` and
`DifferentFrom` as a no-op, on the ground that excluding a value the builder never draws restates a guarantee it
already gives. The library treats a restated guarantee as a no-op wherever else it meets one.

The library is below 1.0, and a behaviour change is permitted ([`CONTRIBUTING.md`](../../../../CONTRIBUTING.md),
"Public API baseline").

## Decision

`Any.Enum<T>()` accepts a value its type does not define in `Except` and `DifferentFrom` as a no-op, and `JD017`
does not report it, while an undeclared `OneOf` value stays refused as ADR-0054 decided.

## Rationale

**An exclusion of a value that is never drawn restates a guarantee, and the library's rule for a restated
guarantee is a no-op.** ADR-0101 drew the line in one sentence: an argument that would make the constraint
meaningless is refused, an argument that restates a guarantee already held is accepted. An undeclared enum value
in `OneOf` would be drawn, so it is refused; the same value in `Except` removes nothing, so it is accepted. The
enum builder now reads exactly as the floating-point builders do, and a reader learns one rule rather than two.

**The analyzer must not refuse what the run time honours.** A build-time rule earns its place by catching what
would fail later. Here nothing fails later: the chain draws a declared member on every seed and replays from its
seed. A warning on it is friction without protection, and under warnings-as-errors it stops a correct build. The
two sides disagreed, and the run time is the side that matches the library's rule for restated guarantees.

**The value is reached deliberately, so silence loses no typo.** The usual case for flagging an inert argument
is a slip of the pen. A misspelt member name never compiles, so an undeclared enum value is written through a
cast or through `default`: a deliberate spelling. `DifferentFrom(default)` over a value the test holds is an
ordinary arrangement, and it is the one this rule broke.

**Refusing at the run time instead would break the same arrangement louder.** Making `Except` throw for an
undeclared value would bring the run time in line with the old rule, but it would turn
`DifferentFrom(default)` into an exception at arrange time and reopen, for enums, the asymmetry ADR-0101 closed
for floating-point values. ADR-0046's *"refuse loudly at the edge"* is for a request the library cannot honour;
this one it already honours.

## Alternatives Considered

### Refuse the undeclared exclusion at run time

The issue's second direction: `Except` and `DifferentFrom` validate their argument against the declared
universe, as `OneOf` does, and `JD017` keeps reporting. Considered because it keeps ADR-0054's sentence literal
and needs no analyzer change. Rejected because it makes a restated guarantee an error, which is the one thing
ADR-0101 decided the library does not do, and because `DifferentFrom(default)` on an enum without a zero member
would throw where it now works.

### Keep reporting it, at the information level

Report an undeclared exclusion as a fact to weigh rather than a warning, in the manner of `JD024` for an
integer sentinel the domain never produces. Considered because it keeps a signal for a caller who cast the wrong
number. Rejected by the maintainer in favour of silence: the value is always written deliberately, through a
cast or `default`, and an information diagnostic on `DifferentFrom(default)` would fire on exactly the
arrangement this record makes legal.

### Supersede ADR-0054 wholesale

Rejected for the reason ADR-0101 gave: almost all of it stands — the draw-side rule, the `OneOf` verdict, the
generic exemption — and a successor restating it all to change one clause would bury the change.

## Consequences

### Positive

* `JD017` and the run time agree on every exclusion, and the agreement suite now asks the builder through
  `Except` and `DifferentFrom` as well as `OneOf`, so the two cannot drift apart again unnoticed.
* `DifferentFrom(default)` builds cleanly under warnings-as-errors whether or not the enum declares a zero.
* The enum and floating-point builders follow one argument rule.

### Negative

* A caller who meant to exclude a declared value and cast the wrong number gets no signal at build time. The
  exclusion is inert, so the test's real failure, if any, surfaces where the drawn value is used.

### Risks

* The no-op rests on a builder fact: an undeclared value is never drawn. On a `[Flags]` enum that holds for a
  value no OR of declared members reaches; a reachable combination is drawn once `AllowingCombinations()` is
  declared, and `JD017` already stands down on that chain. A future change to what the builder draws would have
  to keep this decision explicitly.

## Follow-up Actions

* The `JD017` pages, English and French, state the narrowed rule in the same change that lands it.
* Whether ADR-0054's status changes beside its second narrowing is the maintainer's call on acceptance; this
  record does not change it.

## References

* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — the rule this record narrows.
* [ADR-0101](0101-accept-a-non-finite-exclusion-as-a-no-op.md) — the same narrowing for the floating-point
  builders, and the principle this record applies.
* [ADR-0020](0020-draw-flag-enum-combinations-behind-an-opt-in.md) — which combinations a `[Flags]` enum draws.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — when a refusal is the honest answer.
* [Issue #228](https://github.com/Reefact/just-dummies/issues/228) — the disagreement between `JD017` and the run
  time, and the open question this record settles.
