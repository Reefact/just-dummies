# ADR-0067 | Report a filtered pool through an explicitly implemented interface, and warn about nothing

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0067-report-a-filtered-pool-through-an-explicit-interface.fr.md)

**Status:** Proposed
**Proposed:** 2026-08-11
**Decision Makers:** Reefact

## Context

[ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) made a caller-supplied
value set compose with every other constraint a generator carries: once the values are supplied there is
nothing to build, so each declared constraint becomes a test that each value passes or fails, and the
domain is the values that pass.

A value the constraints reject leaves that domain **silently**. The only outcome the library reports is
an *emptied* domain, raised at declaration as a conflict naming both sides. Between "all the values
survive" and "none of them does" there is no signal at all.

That silence has a cost for one caller in particular: the one whose value set is a **catalogue** — a list
of first names, of currency codes, a fixture table — declared once, reused across a suite, with the
invariants that surround it declared next to each draw. When part of that catalogue never draws, there
are exactly two repairs: widen the invariant, or fix the catalogue. Choosing between them requires
knowing *which* values fell and *which* declared constraint took each one.

The library already holds those facts. The specification keeps the caller's list and the surviving list
side by side, and it already derives which declared constraints reject which values, because that
derivation is what lets a conflict message name both sides rather than blame the caller's array.

The facts are also already reachable from outside, **by accident**. A distinct collection gates eagerly
on the surviving cardinality ([ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md)),
so probing that gate and then drawing a distinct set of exactly the surviving size reconstitutes the
surviving pool through the public surface alone. The path is deterministic — the gate is a
declaration-time check, not a draw — but it costs one declaration per probe, it rests on an interaction
designed for something else (a pinned value counts as extending the domain only when the generator could
not have drawn it), and nothing promises it keeps working.

The generators already answer this class of question through an interface they implement **explicitly**:
25 of them carry an internal cardinality-and-membership interface that exists so distinct collections can
gate. The shape is therefore in place and only its visibility is not — but that interface answers a count
and a yes/no, and says nothing about *why* a value is absent.

Four further facts frame the choice:

* The library has **no reporting channel**. It writes to exactly one place, on the failure path, through
  an optional caller-supplied sink that falls back to standard error when it is absent or throws.
* The analyzers cannot cover the case. The rule that reads a string chain stops reasoning about the
  length budget once a value set is declared, and no chain rule follows a chain through a variable — and
  a catalogue is a variable by nature.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) draws the boundary of what the library
  judges: a typed builder judges the domain it represents, and the generic entry points judge nothing in
  a caller-supplied pool, because the type is opaque and the pool is the whole specification.
* [ADR-0006](0006-materialize-dummies-only-through-generate.md) refuses an implicit conversion to the
  generated type, on the ground that it is neither cheap, nor total, nor referentially transparent, and
  that it lets a caller forget a draw is happening at all.

Nothing is published: the public-API baseline carries only `#nullable enable`. A public interface added
now costs nothing and would be a major version after `1.0`.

## Decision

A generator whose domain is a caller-supplied value set reports that domain — the values the declared
constraints kept, and the ones they rejected together with the constraint that rejected each — through a
dedicated interface it implements explicitly, reachable only by a deliberate cast and never announced on
the generator's own initiative.

## Rationale

**The question the silence leaves open has exactly two answers, and both turn on the same fact.** Naming
the constraint that took each value is what separates *the catalogue is wrong* from *the invariant is
wrong*. A count cannot separate them, and neither can a membership test — which is precisely why the
interface already in place answers the wrong question, however convenient publishing it would be.

**Reporting a domain is not materializing a dummy, so the conversion refusal does not reach it — and its
criteria are met here rather than bent.** ADR-0006 refused a member that was neither cheap, total nor
referentially transparent, and whose worst property was letting a caller forget a draw was happening. An
inspection draws nothing: the domain is fixed the moment the constraints are declared, the same question
returns the same answer on every call and under every seed, and an explicit cast is the opposite of
forgetting.

**Explicit implementation is what makes a diagnostic feature affordable.** The cost of such a feature is
not the code it takes, it is what it does to the surface every other user reads. The fluent chain is the
library's teaching surface, and a member that answers a maintenance question does not belong in the same
completion list as the constraints — on every generator, for every user who will never ask it. Explicit
implementation removes it from that list entirely, so the feature costs exactly nothing to everyone who
does not want it. This is the same reasoning the internal interface already embodies; the decision
extends it rather than inventing it.

**The cast is the right ergonomics for the question, not a workaround around a limitation.** Inspecting a
recipe is stepping outside the contract the rest of the surface teaches, which is that a recipe's output
is a value. A cast states that intent at the call site, where a reader sees it.

**Handing back the facts keeps the library out of a judgement that is not its own.** A warning would
require a channel the library does not have, would fire where nobody reads it — a test that passes — and
would have the library rule that a narrowed catalogue is a mistake. It is not: narrowing a shared
catalogue at one call site is exactly what composing a value set with a constraint is *for*. ADR-0054
already places a caller's pool outside what the library judges; reporting what the constraints did to it
respects that line, warning about it does not.

**The information is already escaping through a route nobody designed, which argues for deciding rather
than for leaving things alone.** The status quo is not "the domain is private"; it is "the domain is
public through an undesigned interaction, at a cost, with no promise attached". A decision replaces an
accident.

**The window is open now and closes at the first release.** Adding a public interface to a surface whose
shipped baseline is empty costs nothing today. After `1.0` the same addition is a major version, and the
silence would have to be lived with or paid for — the same timing argument ADR-0033 made when it opened
the string value set to composition.

## Alternatives Considered

### Publish the existing cardinality interface as it stands

Considered because it is already implemented explicitly on 25 generators, so publishing it would cost a
visibility change and nothing else — the cheapest possible version of this decision.

Rejected because it answers a different question. A count and a membership test let a caller reconstruct
the surviving list only by probing it value by value, and never say which constraint removed a value —
the fact both repairs turn on. It would also freeze, in the public surface, an interface whose shape is
owed to an internal collaboration with the distinct-collection gate, and which would then have to serve
two masters at once.

### Put the members on the generators themselves

Considered because it needs no cast and no second type: the members would sit on the fluent builders,
discoverable by anyone who wonders what their pool has become.

Rejected because it charges the whole readership for a maintenance feature. The constraint list is what
the fluent surface teaches, and an inspection member sits inside it on every generator, in every
completion list, for every user who will never ask the question. Discoverability is the argument for it
and the argument against it.

### Warn at run time when part of a pool is rejected

Considered because it is the shape the need takes when it is first felt: the caller wants to be *told*
that their catalogue has drifted, not to have to ask.

Rejected on three counts. The library has exactly one write, on the failure path, so a warning needs a
channel invented for it. A warning on a passing test is invisible, which defeats the purpose it was added
for. And it would rule against a legitimate use, since narrowing a shared catalogue at one call site is
what the composition exists to allow — the library would be reporting a defect where there is a feature.

### Report it at compile time with an analyzer

Considered because a warning genuinely belongs at build time, in the IDE and in CI, and because there is
precedent: several constraint rules already front-load to the build what constant arguments make
decidable.

Rejected as insufficient rather than wrong. The string rule stops reasoning once a value set is declared,
and no chain rule follows a chain through a variable — and a catalogue is a variable by nature, so the
case that motivates this decision is exactly the one an analyzer cannot see. It remains a worthwhile
complement for a pool written inline at the call site, and is recorded below as a follow-up rather than
as an alternative to this decision.

### Leave the filtering to the caller

Considered because it needs no API at all: a caller can filter their own catalogue against their own
invariants before handing it over, and compare the two lists themselves.

Rejected because it duplicates the library's predicates in the caller's code, where they drift from the
constraints they mirror — and drift is the whole failure being addressed. It also reports an emptied list
as an argument error about an array the caller never wrote, rather than naming the two constraints in
play, which is the regression ADR-0033 removed.

### Do nothing, and let the distinct-collection probe stand

Considered because it already works, deterministically, through the public surface.

Rejected because it costs a declaration per probe, rests on an interaction designed for another purpose,
and is a promise nobody made: a refactor of the gate would break callers who never knew they depended on
it, and the failure would surface far from its cause.

## Consequences

### Positive

* The repair question has a first-class answer, and a project can turn it into a test that locks its own
  catalogue against its own invariants — the check running where the catalogue lives.
* The fluent surface is unchanged, and the feature costs nothing to anyone who does not cast for it.
* No reporting channel is invented, and the library goes on judging nothing in a caller's pool.
* The undesigned probe stops being the only route to a fact the library already holds.

### Negative

* A second public interface to keep in step with the generators, and a public commitment to naming the
  rejecting constraint: the culprit derivation becomes a contract rather than a detail of message
  building.
* Two levels to explain instead of one. A generator is a recipe whose only output is a value — except
  that some of them will also answer a question about their domain.

### Risks

* **The family scope is the real decision left open.** Every family carries a value set. On a single
  generator the interface is a wart; on all of them it is a project, and the scalar families reach their
  domain through an ordinal space where *the surviving values* is far less immediate than it is on a
  string. Settling that by drift would recreate the asymmetry ADR-0033 removed.
* **Naming the culprit is a judgement when several constraints reject the same value.** Reporting all of
  them, in declaration order, is the obvious answer; it is also a contract once published.
* The interface's name is frozen at `1.0` along with the rest of the surface.

## Follow-up Actions

* Decide the family scope before implementing — at minimum, whether the interface is optional, with the
  caller testing for it, or carried by every generator that admits a value set.
* Settle the interface's name while the surface is still free to change.
* Consider the analyzer complement for a pool written inline at the call site, which this decision leaves
  uncovered rather than refuses.
* Document the feature for users, English and French, if the decision is accepted.

## References

* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — the composition this
  record reports on.
* [ADR-0004](0004-gate-distinct-collections-by-cardinality-else-bounded-draw.md) — the distinct-collection
  gate whose eager check makes the domain observable today.
* [ADR-0006](0006-materialize-dummies-only-through-generate.md) — the materialization boundary this
  decision does not cross.
* [ADR-0054](0054-draw-only-valid-values-from-a-typed-builder.md) — the line between what a typed builder
  judges and what it must not judge in a caller's pool.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the ambition boundary any new
  capability is measured against.
* [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.md) — the value object a declared
  constraint is already carried as, and which a reported rejection names.
