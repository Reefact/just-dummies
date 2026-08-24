# ADR-0089 | Draw a composed parameter through the generator its type owns

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0089-draw-a-composed-parameter-through-the-generator-its-type-owns.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-24
**Accepted:** 2026-08-24
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

A parameter whose type the base table has no row for is a composed parameter: a domain type of the
developer's own. The engine had two ways to draw one. If the compilation already contained a
generator for that type, it emitted a call to it. Otherwise it unwrapped the type's one-parameter
static factory, read the guards in that factory's body (§5.3), and derived a recipe for the
parameter here.

The derived recipe describes the value object's invariant. The same invariant is what the generator
scaffolded for that value object reads, from the same guards, when the developer runs the tool on
the type itself.

A domain type is composed by many others. An aggregate holds a reference, so does a line, so does an
event; each is a separate constructor parameter in a separate generated file.

The tool writes each file once and transfers ownership of it to the developer
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md)). It does not
regenerate them, and does not see them again.

A parameter the engine cannot draw is already answered by emitting an identifier that does not
exist, so the developer's own build reports it at that line
([ADR-0060](0060-seed-generators-from-constructor-guards.md)).

The engine names a generator by one function over the type's name (§11.3), and that name carries no
type arguments.

`dum generate` refuses a generic target (§3.2).

Three adversarial passes over the guard reader found the same defect shape each time: a derived
recipe the tool reported as inferred, and a real draw the domain's own constructor rejected. The
standing answer to a guard the reader cannot vouch for is to mark it and block the build
([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md),
[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md)).

## Decision

A composed parameter is drawn through the generator its own type owns, named whether or not the
compilation carries that generator yet.

## Rationale

A value object's recipe has one right address: the generator for the type that declares the
invariant. Deriving it at each composing site made as many copies as there were sites, and because
the tool hands every file over and never returns to it, those copies could only diverge — from each
other, and from the constructor they described, the first time that constructor changed. One reading
per type replaces N readings per type, and the reading that survives is the one the developer will
look at when they want to know how an `OrderReference` is drawn.

That the copies were also *wrong* is what forced the question, but it is not the argument. Each pass
closed the defects it found and the next pass found more, in the fixes themselves; a mechanism whose
correctness needs that many rounds is a mechanism carrying more ambition than the base allows
([ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)). Removing it is cheaper
than continuing to make it honest, and it costs no coverage: the guards it read are still read,
once, where they belong.

Naming the generator when it is absent is the same move ADR-0060 already makes, and it is strictly
more informative than the sentinel it replaces here. Both produce a compile error at the parameter's
own line, in the editor, the error list and continuous integration alike. But an invented identifier
says only that something is missing, while a type name says which type to run the tool on. The
developer was going to write that generator either way — the alternative was a copy of its recipe
inlined somewhere else — so the error is not an obstacle placed in their path, it is the path,
stated one step earlier.

This does not weaken [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md), and
the boundary is worth stating because it is the first question a reader will ask. That record
governs the *library's* members: it exists because a member absent from the developer's asset is a
compile error they did not cause and cannot interpret. A generator for their own domain type is
neither — they caused it by not having scaffolded it, and the message names the remedy. The rule
itself still binds where it applies: nothing from the library is chained onto a generator this
compilation cannot see, since there is no type to resolve a member against.

A generic type is left to §5.5 because the naming function cannot name it. `Repository<Order>` and
`Repository<Line>` would both be told to write `AnyRepository`, which is not the name for either,
and `dum generate` would refuse the target anyway. A sentinel that says nothing is better than a
name that says the wrong thing — the same bias toward under-reporting that ADR-0060 chose for
guards.

## Alternatives Considered

### Keep deriving a recipe, and mark it where the reading is uncertain

Considered because it is what three passes of work had been building toward, and because it keeps
a composed parameter resolvable without the developer scaffolding a second type.

Rejected because marking answers the wrong half of the problem. A mark says the reading may be
wrong; it does not say the recipe is duplicated, and duplication is the fault that survives even a
perfectly correct reading. Two files composing the same value object would still carry two copies of
its invariant, both correct on the day they were written, and nothing in the tool would ever
reconcile them.

### Emit the derived recipe only when no generator exists, as a fallback

Considered because it keeps the common case identical and never produces a file that fails to
compile, which is the gentler default.

Rejected because it makes the emitted recipe depend on what happens to be in the compilation on the
day the tool runs. The same parameter would scaffold two different ways in two projects, and
scaffolding the value object later would silently change what a re-run produces. It also preserves
the duplication in exactly the situation where it hurts most — the developer has not thought about
that type yet, so the copy is the only statement of its invariant anywhere.

### Emit nothing and leave the parameter open, as before this path existed

Considered because it claims the least, which suits a tool whose whole argument is honesty.

Rejected because the sentinel it emits is less informative than the type name for no gain. The
developer has to discover which type to scaffold from the parameter's declaration; the tool already
knows and would be declining to say.

## Consequences

### Positive

* A value object's invariant is read once and lives at one address, so changing its constructor
  changes one generated file rather than every file that composes it.
* An emitted file states its whole dependency graph in its constructor initializer: every composed
  type it needs is a name the compiler will check.
* The composition path stops reading guards, which removes the surface three passes kept finding
  defects in.
* A composed parameter carries no method, so an emitted file is shorter by one method per composed
  parameter and reads as a list of calls.

### Negative

* Scaffolding an aggregate before its value objects now produces a file that does not compile, where
  it previously produced one that did. The remedy is named at the failing line, but it is a step the
  developer did not have to take before.
* A constraint the *composing* type's constructor declares on a composed parameter can no longer be
  applied — a generator for a domain type carries no `WithMaxLength`. It is reported rather than
  dropped in silence, but it is not honoured.
* The `factory` provenance word and the per-parameter candidate list describe nothing any more and
  are removed, which is a breaking change to the recap and to `--format json`.

### Risks

* A developer scaffolding a deep aggregate first meets several `CS0246` at once and may read them as
  the tool being broken rather than as a work list. The recap naming each generator is what should
  make the list legible; whether it does is a field question.
* The removed constraint above is the one place this decision trades coverage for a single address,
  and the trade is only worth it while composed parameters are rarely constrained by their
  composing type. If the field says otherwise, that is a report against this record.

## Follow-up Actions

* Watch for the deep-aggregate case in use: if several missing generators at once read as breakage
  rather than as a work list, the remedy is in how the recap presents them, not in the emission.

## References

* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) — the file is handed over and never regenerated, which is why a copy can only diverge.
* [ADR-0059](0059-emit-only-members-resolved-in-the-target-compilation.md) — the library's members, and the boundary this record does not cross.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the compile-error mechanism this decision spells as a type name.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
