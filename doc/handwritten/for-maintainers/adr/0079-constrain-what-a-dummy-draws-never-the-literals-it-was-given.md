# ADR-0079 | Constrain what a dummy draws, never the literals it was given

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Context

`Any.String()` without a value set is **constructive**: it lays a value out as
`prefix + filler + contained values + filler + suffix` and returns it, never generating and filtering. The
character pool feeds the **filler** alone — the anchored fragments are appended exactly as the caller wrote
them. `ADR-0075` already frames every character family, and the subtractive `WithoutAlpha` / `WithoutNumeric`,
as narrowing the set an unconstrained draw draws from.

Until now the declaration-time cross-validation went further than the layout: it also held every anchored
fragment to the declared family, to each subtraction and to the declared casing, and refused the chain when a
fragment carried a character they excluded. `JD015` mirrored that refusal at build time whenever the arguments
were constants.

The consequence was that a very ordinary format could not be expressed at all: a fixed prefix followed by a
body restricted to an alphabet. `AlphaNumeric().StartingWith("ORD-")` was refused, because the separator the
caller wrote is not alphanumeric.

The available workaround was to declare the separator inside a custom pool. That makes the separator drawable
**everywhere**, so the values produced carry it in the body and at the end — the opposite of the invariant the
chain was meant to express. It costs a second rule as well: a custom pool occupies the one character-family
slot and, because the pool is the whole character definition, refuses to combine with a casing, so a
single-case format has to bake its casing into a pool literal instead of declaring `UpperCase()`.

Two neighbouring mechanisms are unaffected by any of this and stay as they are. The **length budget** is a
separate check: the fragments are laid out side by side, so their lengths still have to fit the declared
length. And with a value set in force the specification stops laying anything out and becomes a **filter** over
caller-supplied values, where the same constraints narrow the supplied pool rather than shape a string —
a different mechanism with its own contract (`ADR-0054`).

## Decision

A character family, a custom pool, a subtraction and a casing govern every character `Any.String()` **draws**
and nothing else: on a shaped string that is the filler alone, so a literal fixed by `StartingWith`,
`EndingWith` or `Containing` is kept exactly as written; on a value set nothing is drawn at all, so the
supplied values stay subject to them.

## Rationale

The rule the generator already follows is the layout, and the layout never gave the character pool any reach
over the fragments. The removed validation therefore refused chains the generation path would have honoured
perfectly well: it enforced a rule that nothing downstream needed, and its only observable effect was to make a
legitimate format unwritable.

This bounds no correctness, which is the line `ADR-0046` draws. A drawn value still satisfies every constraint
declared — what changed is what the constraint *declares*, not whether the generator honours it. The narrower
claim is also the more useful one: a caller who writes a separator into a prefix is stating that the separator
belongs there and nowhere else, and that is precisely the invariant the chain can now express and the old
workaround could not.

Taking the three kinds together rather than one at a time is what makes the rule teachable. On the constructive
path a family, a subtraction and a casing are the same kind of thing — the three filters that narrow the
alphabet the filler is drawn from. A rule exempting a literal from two of them but not the third would have to
be carried as an exception, and that exception would follow from nothing in the layout it claims to describe.
Uniformity also removes a whole class of contradictory combinations instead of shrinking it, the same value
`ADR-0008` names when it refuses to make a generated pattern chainable with the string constraints.

One rule covers both paths, and reading it as an exemption for literals is what makes them look like two. A
constraint governs the characters drawn, and what those are is a question about the layout rather than about
who wrote them. On a shaped string the fragments claim their own regions and the family claims the complement,
so the two never meet and cannot contradict each other — which is why `AlphaNumeric().StartingWith("ORD-")`
composes. A value set claims the whole string instead: it supplies the value entire and leaves no filler, so
the family's region is that same value, and two constraints over one region must agree exactly as
`Alpha().Numeric()` must. `OneOf("ORD-1").AlphaNumeric()` is refused for the reason two families are refused,
not by a different principle. The alternative reading — exempting a supplied value because the caller wrote it
— would leave the family governing nothing whatsoever on that path, making it a silent no-op rather than a
constraint.

`JD015` has to narrow with the run time. A diagnostic that refuses at build time what the run time honours is
worse than either behaviour on its own, because the caller cannot satisfy both. The rule keeps the length
budget, which is still value-dependent, still undecidable by the type system, and still exactly the case
`ADR-0014` names as the analyzer's — that record's illustration moved onto the length budget for this reason,
while its decision stands untouched.

## Alternatives Considered

### Keep the refusal and document the custom pool as the way out

It costs no change, and the workaround does produce a value.

Rejected because the value it produces violates the invariant the caller was trying to state: the separator
becomes drawable in the body and at the end. A documented workaround that quietly breaks the rule it works
around is worse than the refusal it replaces, and it also forfeits the casing, so two of the format's rules
stop being readable calls and become an opaque string literal.

### A per-segment DSL, giving each zone its own family and length

It is the richest form, and it expresses a multi-zone format exactly rather than by exemption.

Rejected because `ADR-0008` already rejects the same shape, under "keep the generator chainable with the other
string constraints": a terminal, whole-specification generator "removes a class of contradictory combinations
entirely", and a segment DSL would reintroduce that class between segments instead of between the pattern and
the chain. `Any.StringMatching(...)` already expresses a genuinely multi-zone format more compactly, and stays
the right tool for it.

### Exempt the family and the subtraction, but keep judging the casing

A casing reads as a property of the whole value rather than of an alphabet, and keeping the check would leave a
net under an obvious typo — a lowercase prefix declared beside `UpperCase()`.

Rejected because the distinction has no basis on the path being changed: the casing is one of the three filters
that build the filler alphabet, applied character by character exactly as the family and the subtractions are.
Keeping it would preserve a class of contradictions the uniform rule removes, and would make the rule two rules
— one for alphabets, one for casing — where the layout justifies only one.

### An opt-in on the fragment methods, asking for the exemption explicitly

It would let both readings coexist and break nothing.

Rejected because it widens the public surface to offer a choice nobody needs in the other direction: a caller
who writes a literal has already said what those characters are. A flag would document a hesitation rather than
a rule.

## Consequences

### Positive

* A fixed prefix followed by a constrained body becomes expressible, with each of the format's rules a named
  call rather than a hand-built pool literal.
* Values honour the invariant the chain states: the separator appears in the prefix and nowhere else.
* The constraint semantics stop depending on the order the constraints are declared in, because no combination
  of a character constraint and a fragment can fail any more.
* `JD015` becomes smaller and truer: one check, matching the run time exactly.
* What it stops refusing, `JD031` reports as information: the literal's character appears where it was written
  and nowhere else. The reading the refusal carried survives as a note, which is what it should have been — a
  fixed-prefix format is the intended use, and only its author can tell it from a slip.
* A relaxation, not a break — no chain that works today stops working, and no generated value changes shape.
  Only code asserting on the removed exception is affected.

### Negative

* `UpperCase()` and `LowerCase()` no longer mean "every letter in the value has this case" on the constructive
  path; they mean it of the letters the generator drew. A lowercase literal declared beside `UpperCase()` is
  kept as written, and the typo that used to be reported now passes silently.
* `JD015` loses three of its four checks, so a chain contradicting its own family is no longer flagged at
  build time — because it is no longer a contradiction.

### Risks

* The two paths read differently at the call site — `UpperCase().Containing("abc")` now keeps `"abc"` while
  `OneOf("abc").UpperCase()` still rejects it — and a reader who learns the rule as "a literal you wrote is
  exempt" will expect the second to be accepted too and find the behaviour arbitrary. The rule that predicts
  both is about what is drawn, never about who wrote it; the documentation has to teach it that way, because
  the authorship reading is the intuitive one and it is wrong.
* A literal carrying characters the declared family cannot draw is accepted, so a genuine mistake — a lowercase
  prefix beside `UpperCase()`, a separator beside a family chosen by habit — no longer fails anything. `JD031`
  puts it in front of the author at `Info`, which is a note rather than a stop: a codebase that silences the
  rule loses the last thing watching that case.

## Follow-up Actions

* Watch whether `JD031` keeps carrying news. It is `Info` precisely so that answer can come from use rather
  than from prediction: a codebase writing fixed-prefix formats everywhere may reasonably silence it, and if
  that becomes the common outcome the rule is telling readers something they already know.

## References

* Issue #94 — the report, the measured workaround, and the acceptance criteria.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md) — every family only narrows what a draw draws;
  this record settles what "a draw" covers.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the ambition is bounded, the
  correctness of a returned value never is.
* [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.md) — the rejection a segment DSL runs into.
* [ADR-0014](0014-enforce-structural-any-conflicts-at-compile-time.md) — the value-dependent case an analyzer
  carries; its illustration was repinned onto the length budget by this decision.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — the caller's pool as the whole
  specification, on the generic entry points.
* [JD015](../../for-users/analyzers/JD015.en.md) — the narrowed rule.
