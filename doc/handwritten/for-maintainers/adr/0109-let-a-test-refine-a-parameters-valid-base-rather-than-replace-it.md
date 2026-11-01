# ADR-0109 | Let a test refine a parameter's valid base rather than replace it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-27
**Accepted:** 2026-10-27
**Decision Makers:** Reefact

Supersedes [ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.md).

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

A scaffolded `AnyX` draws each constructor parameter from a recipe the engine inferred: a row of the
base table tightened by the constructor's guards (§5.2, §5.3), or, for a composed parameter, the
generator that type owns (§5.4, [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md)).
That recipe is the parameter's **valid base**: what the constructor accepts.

Where the base is written depends on what the parameter has to report
([ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.md)): a parameter whose
chain a guard tightened, or that one of the two blocking markers of §5.5 and §5.6 holds, gets a
private factory the constructor calls; every other one is written inline, as one call in the
constructor's initializer, because a method wrapping that call would say nothing the call does not.

A rule can belong to the composing type rather than to the composed one. A `Shipment` may accept only
a domestic `Route`, while a `Route` on its own may be international. The engine cannot apply such a
rule to a composed parameter: it marks the parameter (§5.6), and the developer writes the rule into
the file, which is theirs ([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md)).

Each parameter carried two `With` overloads: one pinning a value, one taking an `IAny<T>` that
**replaces** the recipe. A test that cares about one part of a value — the distance of a route, an
upper bound on a quantity — could only reach it through the second, by building a generator from
scratch. That generator does not carry the base, so the test has to restate the rule, or loses it.

A test that loses it still compiles, and draws: it passes whenever the draw happens to satisfy the
rule, and fails on the other runs, for a reason unrelated to what it verifies. A test that fails only
on some runs is the failure the library exists to remove.

The library's generators are immutable recipes, every constraint returning a new instance
([ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md)). On a library generator,
constraints accumulate, and two that contradict each other are refused where they are declared, the
message naming both sides. A constraint may return a different generator type from the one it is
called on.

The analyzer that reports a discarded constraint, `JD006`, exempts a call that is the whole body of a
lambda passed as an argument, so that a test asserting a refusal can write the call there.

The rationale of [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.md) relied on the
`IAny<T>` overload to serve a developer drawing from an isolated `Any.WithSeed` context, parameter by
parameter; the ambient scope, `Any.UseSeed`, reaches every draw the emitted generator makes, composed
parameters included. The rationale of [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md)
relied on the same overload as the way a test opts into a generator that sometimes draws `null`.

The library maps a scaffolded `AnyX` to the anonymous, parameterized Creation Method of
*xUnit Test Patterns* (`core-concepts`): its `With` methods are the parameters.

## Decision

Each resolved parameter of a scaffolded generator is drawn from one factory holding its valid base,
and a test either pins its value or refines that base through a lambda, never replaces it.

## Rationale

The fault was not that a test could build a generator; it was that building one meant starting
over. A Creation Method exists so that a test does not have to know how to build a valid object,
including the objects it depends on. The replacing overload made that knowledge leak back into every
test that parameterized a value: a construction rule written once in the generator was copied into
each such test, where it reads as deliberate as the value the test is about — and where forgetting it
compiles. Refining starts from the base instead, so the test states only what it cares about, and the
rule keeps its single address.

One factory is what makes that address real. The default recipe and the refinement have to start from
the same place, or the developer who tightens one leaves the other loose. That place has to be the
one the developer already edits: the base the engine wrote, or the rule they wrote where the engine
asked them to. A refinement starting from a fresh generator would compile, would read as though it
started from a valid one, and would reproduce the intermittent failure in generated code instead of
in the test.

That is also why every resolved parameter now has a factory, and why this decision supersedes
ADR-0096. ADR-0096 was right that a method wrapping one call said nothing the call did not — while the
constructor was its only caller. With a second caller, the factory says the one thing a reader most
needs to find: where the parameter's base is, for the default and for every refinement alike. A
parameter inline in the initializer would leave the refinement either a second copy of that call or
nothing to start from.

The lambda returns the refined generator rather than mutating the one it receives, because the
generators are immutable and a mutating `With` would contradict the contract the emitted type
implements (ADR-0057). It also keeps the one slip left visible: a lambda that drops a constraint has to
do it as a statement in a block, which `JD006` reports, whereas a mutating shape would drop the whole
refinement in exactly the form `JD006` exempts.

On a leaf, refining is stronger than restating, not just shorter: the library's constraints
accumulate, so a refinement that contradicts the base fails where it is written, naming both sides,
on every run. That is the library's correctness line applied to the call site
([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)).

Removing the replacing overload rather than keeping it beside the refining one is what makes the
failure a compile error. With both present, nothing at the call site tells the two apart, and the one
that loses the rule stays the one a reader reaches for. The needs it served all have another route:
pinning a value covers a test that cares about the whole value, `null` included; the ambient seed
scope covers determinism outside a test better than per-parameter context generators did, since it
reaches composed parameters too; and the rest — drawing among a few chosen values, a collection whose
elements need another recipe, a nullable parameter drawn `null` some of the time — are rare enough
that the developer adds the overload to their own partial file, where it is a choice made for one
parameter rather than the default offered on all of them.

The value overload stays. A test that cares about the whole value has to be able to say so, and a
fixed value that breaks a rule fails on every run rather than on some, which is the visible failure
this decision is after.

## Alternatives Considered

### A configuring `Action<T>`, the options-pattern shape

Considered because it is the familiar shape of nested configuration in .NET, and lets a statement
lambda chain adjustments without returning anything.

Rejected because the emitted generators are immutable: the natural call compiles and silently discards
the adjustment, in the one form `JD006` exempts. Making `With` mutate instead would reopen ADR-0057.

### Starting the lambda from a fresh `new AnyX()`

Considered because it needs no factory for a composed parameter, keeping ADR-0089's emitted shape.

Rejected because it ignores the base the developer wrote. The lambda would hand the test an arbitrary
value while reading as though it started from a valid one.

### Keeping ADR-0096's inline parameters, and repeating their call in the refining overload

Considered because it keeps the emitted file as short as it was, a factory only where a parameter has
something to report.

Rejected because it writes the base twice in one file. A developer who edits the call in the
constructor's initializer leaves the refinement's copy as it was, which is the drift a single address
exists to prevent.

### Starting from the current recipe, after an earlier `With` call

Considered because successive refinements would then compose.

Rejected because the field would have to keep the concrete generator type, and after a value is
pinned there is no generator left to start from. Every other `With` method lets the last call win,
and this one does too.

### Keeping the replacing overload beside the refining one

Considered because it keeps every existing call compiling.

Rejected because the overload that loses the rule would stay one keystroke away from the one that
keeps it, with nothing at the call site to tell them apart.

### Keeping the replacing overload on nullable parameters only

Considered because a nullable value type drawn `null` some of the time has no route through a lambda
that must return the generator it receives.

Rejected because the partial file covers that need in a few lines, and one shape for every parameter
is simpler to read and to teach than a shape that depends on the parameter's type.

### Composed parameters only

Considered because the composing type's rule is where the problem was first seen.

Rejected because a leaf has the same defect: replacing a guarded `Positive()` loses it exactly as
replacing a composed base loses a rule the developer wrote.

### Threading an isolated context through the generators

Considered because it would let `Any.WithSeed` reach every parameter, the case the replacing overload
served.

Rejected because it needs a second copy of every recipe and reverses ADR-0061, while the ambient
`Any.UseSeed` already reaches the whole tree.

## Consequences

### Positive

* A test states only what it cares about; a rule a parameter must satisfy is written once, in the
  generator, and changing it there changes every test at once.
* The replace-and-lose-the-rule call no longer compiles.
* On a leaf, a refinement that contradicts the base is refused where it is written, on every run.
* The emitted surface does not grow: each parameter keeps two `With` overloads.

### Negative

* Every resolved parameter carries a factory, so the emitted file is longer. This reverses
  ADR-0096's decision, and ADR-0089's consequence that a composed parameter carries no method.
* A composed parameter whose generator does not exist yet is named at several places in the file,
  so the build reports the missing type more than once.
* Drawing among chosen values, changing a collection's element recipe and drawing `null` some of the
  time need an overload the developer writes in their partial file.
* Files scaffolded before this decision keep the replacing overload; a re-scaffold removes it, and a
  call still using it then fails to compile.
* ADR-0061's and ADR-0064's rationales name the replacing overload; their decisions stand, but the
  routes they described now go through the ambient scope, the value overload and the partial file.

### Risks

* A developer who relied on the replacing overload may read its absence as a regression. The
  documentation has to teach each remaining route, step by step.
* A refinement guarantees its starting point, not its result: a lambda that overrides what the base
  set for a composed value is the test's choice, and can still produce a value the composing
  constructor rejects.

## Follow-up Actions

* Teach the two overloads and every route the replacing one served on the `justdummies-cli` page.
* Watch for a field report of a need the remaining routes serve badly; the record of which needs were
  judged rare is above.

## Amendment (2026-11-01)

The alternative *Starting from the current recipe, after an earlier `With` call* was rejected above
on two grounds: the field would have to keep the concrete generator type, and after a pin there is
no generator left to start from. The second developer-UX trial showed what the rejection cost
([CLI-9](../../../../tools/ux-trial/REPORT-2.md#cli-9), issue
[#406](https://github.com/Reefact/just-dummies/issues/406)): a refinement written on a composed value
— `k => k.WithValue(v => v.GreaterThanOrEqualTo(0.125))` on a coefficient whose outer base had set
`v => v.LessThanOrEqualTo(0.25)` — started again from the coefficient's base and dropped the cap the
outer base had set, so the composing constructor threw on most draws. That is the rule losing its
single address, at one remove; on a leaf, a second `With…(Func)` silently dropped the first. The
emitted shape contradicted this record's own rationale: refining exists so that the test states only
what it cares about, and the rule keeps its single address.

Both grounds are answered in the emitted shape rather than argued away. Each resolved parameter
keeps its **current recipe**, typed with the generator its factory returns, beside what is drawn;
`With…(Func)` refines that recipe, so refinements accumulate as the library's constraints do and a
contradiction is refused naming both sides; `With…(value)` pins the value and keeps the recipe, so a
refinement after a pin starts from the last recipe rather than from the base, and takes the pin's
place. "The last call wins" no longer describes the refining overload: a pin and a refinement still
replace each other, two refinements add up. The decision stands as written — a test refines the base
or pins a value, never replaces the base — and §4.2 carries the shape. One consequence of that shape
is recorded there too: each `With…` copies the generator through a private copy constructor and
writes one parameter of the copy, so the fields are no longer `readonly`. The instance a caller holds
still never changes, which is what ADR-0057's immutability is about; the maintainer accepted holding
it by the file's convention rather than by the compiler, against an all-arguments constructor that
grew with the parameter count twice over.

Files scaffolded before this amendment keep the earlier shape; a re-scaffold with `--force` gives
the new one, and the `cli` changelog says how to patch a file by hand.

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) — the file is the developer's, partial included.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) — immutable generators.
* [ADR-0061](0061-draw-from-the-ambient-context-and-hold-no-state.md) — the ambient context.
* [ADR-0064](0064-never-draw-null-for-a-nullable-parameter.md) — never draw null by default.
* [ADR-0089](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.md) — the generator a composed type owns.
* [ADR-0096](0096-emit-a-parameter-inline-whenever-it-has-nothing-to-report.md) — the inline emission this record supersedes.
* Issue [#273](https://github.com/Reefact/just-dummies/issues/273).
