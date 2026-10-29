# ADR-0111 | Own an undefined enum value in a value set as JD017, and narrow JD014 out of it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0111-own-an-undefined-enum-value-as-jd017.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-29
**Accepted:** 2026-10-29
**Decision Makers:** Reefact

## Context

`Any.Enum<T>().OneOf(...)` refuses, as soon as it is called, a value the enumeration does not define: one that is
neither a declared member nor, on a `[Flags]` enumeration, a combination of declared members. The refusal is an
argument exception, thrown before the call reads anything the chain declared.

Two rules reported that value. JD017 exists for it: its domain is what the type declares, and its message teaches
that a value the type does not define is a category error rather than a narrowing that happens to be empty. JD014
reports a constant argument a guard certainly rejects, one rule over one table of guards; enumeration membership
was added to that table so that JD034 and JD035, which stop reading a chain at a call that may reject its argument,
would stop there too. Nothing made either rule stand down, so `OneOf((Currency)42)`, a `[Flags]` value no
combination produces, and an undeclared zero in a mixed pool each drew two warnings at the same line and column
(issue #321, found in the developer UX trial).

JD017 judged membership only on chains it could trace back to `Any.Enum<T>()`, and not at all once
`AllowingCombinations()` was declared. JD014 judged it on the `OneOf` call itself, whatever its receiver, and read
collection expressions. The run time refuses the value in every one of those shapes, with or without
`AllowingCombinations()`.

ADR-0078 and ADR-0107 each gave a phenomenon reported by two rules a single owner, on the ground that two
diagnostics on one expression for one mistake is noise that trains a reader to disable both. This repository
treats a change to the semantics of a diagnostic id as an architectural decision.

## Decision

A value set element `Any.Enum<T>()` refuses because the type does not define it is reported by JD017 alone, on the
`OneOf` call wherever its receiver comes from, and JD014 no longer reports it.

## Rationale

**One mistake draws one diagnostic.** The reasoning of ADR-0078 and ADR-0107 applies unchanged: the author made
one mistake, and a second warning at the same position adds nothing but a reason to silence both.

**The owner is the rule whose message teaches the mistake.** JD014's message says a guard rejects an argument,
which is true of every row of its table and says nothing about why this value is wrong. JD017's says the generator
draws only values the type defines. A reader who meets it learns the category error and stops writing undeclared
values, which is what a diagnostic is for.

**The owner must lose nothing JD014 caught.** Handing the case over is only a reduction of noise if every shape the
run time refuses stays reported. JD017 therefore judges membership on the `OneOf` call itself, with the same
reading of the guard JD014 used, rather than through its chain walk: a receiver held in a variable, a collection
expression and a chain carrying `AllowingCombinations()` stay covered, as the run time refuses all three.

**The refusal still stops the chain for the rules that read past calls.** Narrowing JD014's report does not change
what the guard does. JD034 and JD035 must still stop reading at the call, so the shared notion of a rejected
argument keeps the case; only JD014's diagnostic stands down.

## Alternatives Considered

### JD014 as the owner

Considered because JD014 already judged the call wherever its receiver came from, so no coverage had to move.

Rejected because its message names the symptom, not the error. Moving coverage into JD017 is a one-time cost; a
message that does not teach is paid on every report.

### Leave both, at different severities

Considered because it keeps both readings available.

Rejected because the reader still sees two diagnostics for one mistake at the same position, which is the noise
ADR-0078 rejected. Severity changes how loud the duplicate is, not that it is one.

## Consequences

### Positive

* An undefined enumeration value draws one warning, worded as the category error it is.
* JD017 now reports the shapes it missed: a generator held in a variable, a collection expression, a chain carrying
  `AllowingCombinations()`.

### Negative

* A team that suppressed JD017 and kept JD014 loses the report on this shape.

### Risks

* A new guard on enumeration value sets could be added to JD014's table without the corresponding stand-down. The
  differential property suite checks that exactly the owning rule reports an enumeration value the run time
  refuses, and that the other stays silent.

## Follow-up Actions

* None.

## References

* [ADR-0078](0078-own-a-bound-declared-twice-as-one-rule.md) — one owner for a bound declared twice.
* [ADR-0107](0107-own-a-constraint-repeated-identically-as-one-rule.md) — one owner for a constraint repeated identically.
* [JD014](../../for-users/analyzers/JD014.en.md), [JD017](../../for-users/analyzers/JD017.en.md).
* Issue [#321](https://github.com/Reefact/just-dummies/issues/321).
