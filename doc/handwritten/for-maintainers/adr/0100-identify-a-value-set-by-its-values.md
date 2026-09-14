# ADR-0100 | Identify a declared value set by its values, not by the call that declared it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0100-identify-a-value-set-by-its-values.fr.md)

**Status:** Accepted
**Proposed:** 2026-09-14
**Accepted:** 2026-09-14
**Decision Makers:** Reefact

## Context

[ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.md) made a declared constraint a value
object that renders itself, and gave it equality on purpose rather than as a convenience: around
twenty comparisons across the specifications decide whether a second declaration is a harmless
redeclaration, which returns the generator untouched, or a genuine conflict. That equality is
ordinal text — two constraints are equal when they read the same.

For a constraint whose arguments are positional and scalar, reading the same and being the same are
the same question. `Between(0, 100)` written twice renders one string; `Between(0, 100)` beside
`Between(5, 50)` renders two. Nothing is lost by deciding identity on the text.

`OneOf` is not such a constraint, and its own documentation says so: duplicates are ignored, and
nothing is promised about order. `OneOf("a", "b")`, `OneOf("b", "a")` and `OneOf("a", "b", "b")`
therefore declare one domain and render three different strings.

Further facts bear on the choice:

* Nine generators accept a caller-supplied value set — `AnyChar`, `AnyEnum<TEnum>`, `AnyGuid`,
  `AnyPattern`, the string specification and the four interval engines. Until
  [issue #185](https://github.com/Reefact/just-dummies/issues/185) all nine compared the rendered
  call, so two of those three spellings were refused as a second, conflicting set. `AnyPattern` was
  fixed where the audit found it; the other eight were not, which left the surface disagreeing with
  itself about what one domain is.
* The conflict such a comparison produced is one the caller cannot act on. `Cannot apply
  OneOf("b", "a") because OneOf("a", "b") is already defined` names two constraints that ask for the
  same thing, and there is nothing in either to loosen.
* Each of the nine already keeps the set **as declared**, deduplicated, beside the set that survives
  the other constraints — one field feeds the diagnostics, the other feeds the draw. The identity
  this decision needs is therefore already stored, and needs no new state.
* The rendered call is still required, and for a reason the equality question does not touch: a
  conflict quotes the caller their own words, which is part of what makes a contradiction in a
  test's `Arrange` read as a defect of the test.
* `ConstraintCall.OfElided` exists for a pool whose element type is opaque to the library, whose
  arguments it must not render. Such a constraint has no rendered arguments to compare, so text was
  never going to be the general answer to set identity either.

## Decision

A constraint whose argument is a value set is identified, on redeclaration, by that set and not by
the text of the call that declared it.

## Rationale

**The contract already said it, and only the code disagreed.** `OneOf` documents duplicates as
ignored and promises nothing about order, which is a statement that the set is the constraint and
the spelling is not. A comparison that refuses a reordering is not enforcing a rule the library
has; it is contradicting one the library published.

**A conflict that cannot be acted on is worse than no conflict.** Every other contradiction this
library raises names two constraints the caller can choose between. A redeclaration conflict over
one domain names two calls that agree, and sends the reader to look for a disagreement that is not
there. The message is well-formed and the diagnosis it invites is false, which is the failure mode
the base's whole first-class-errors lineage exists to avoid.

**This is a departure from ADR-0042's shape, and it is the narrow one.** That record moved equality
into the type precisely so that comparison sites would stop carrying behaviour, and this decision
gives one class of constraint its comparison back. The reason it does not reopen the case ADR-0042
settled is that the two answer different questions: ADR-0042 is about where the *rendering and its
equality* live, and it keeps them; this is about a constraint whose identity was never its
rendering. The nine sites do not re-implement equality — they compare the sets the generators
already hold, and go on using `ConstraintCall` for every scalar constraint they declare.

**Deciding it in `ConstraintCall` would buy less than it costs.** Carrying the values alongside the
rendered text would put the rule in one place, which is the shape ADR-0042 argues for, but it would
box every element of every value set for a comparison that runs once per declaration, give the type
two kinds of equality to explain, and still leave `OfElided` without an answer. The rule is worth
stating once; it is not worth a second identity on the type every constraint in the library flows
through.

**A rule worth stating is worth a guard.** Nine copies of a comment is how this drifted in the first
place — the audit found one site, and the other eight kept the defect because nothing compared them.
A reflection guard that draws its material from the builders themselves holds the rule across the
generators that exist and the ones not yet written, which is the same move
[ADR-0098](0098-offer-every-oneof-in-both-shapes.md) made for the shape of `OneOf`.

## Alternatives Considered

### Carry the set identity inside `ConstraintCall`

Considered first, because it is what ADR-0042's own argument points at: equality belongs to the type
rather than to each comparison site, and a value set is exactly the case where the type currently
answers wrongly.

Rejected on cost rather than on principle. The constraint would have to carry its values as objects
next to the text it renders, boxing every element of every declared set; the type every constraint in
the library flows through would gain a second notion of equality, which is a thing to explain at
every site that does *not* use it; and the opaque-pool constraint would still have no values to
compare. The nine sites that need the rule already hold the set, so the centralisation would move
the code without removing the concept.

### Normalise the rendering — sort and deduplicate before rendering

Considered because it would need no new comparison at all: if the text were canonical, text equality
would already be set equality, and every existing site would become correct untouched.

Rejected because it breaks the contract the rendering exists for. A conflict message quotes the
caller their own words; a sorted, deduplicated rendering quotes them words they did not write, and
does it precisely in the message asking them to go find the line. It would also be a silent change
to every diagnostic that names a pool.

### Document that the spelling is part of the declaration

Considered as the cheapest answer, and the one that needs no code: state that a value set must be
redeclared identically, and the existing comparison becomes correct by definition.

Rejected because it would publish a rule to protect an implementation detail. The caller gains
nothing from it, it contradicts what `OneOf` already documents about duplicates and order, and it
would make the library's own `Distinct` on the values — which every one of the nine sites performs —
a step whose result no longer matches what the constraint claims to be.

## Consequences

### Positive

* Re-declaring one domain is the no-op the surface promises, on every generator that takes a value
  set rather than on the one an audit happened to reach.
* A conflict over a value set now means the sets genuinely differ, so the message names something
  the caller can act on.
* The rule is held by a guard that constructs the builders and draws its own material, so a
  generator added later is covered without the guard being edited.

### Negative

* Nine sites compare a set where ADR-0042 had left one comparison operator, so a reader of any one
  of them meets a local rule before meeting this record.
* The comparison allocates a set per declaration, where the previous one compared two strings. It
  runs once per declared constraint, never per draw.

### Risks

* **A generator could hold a narrowed set and compare the wrong thing.** The rule needs the set *as
  declared*; comparing the set that survived the other constraints would refuse a legitimate
  redeclaration made after a narrowing. Every generator stores both today, and the one method that
  would narrow the declared set in place — `OrdinalIntervalSpec.NarrowingAllowed` — has no caller.
  The guard does not cover this, because it declares the set before narrowing anything.

## Follow-up Actions

* None. The eight remaining sites, the guard and the changelog entry land together.

## References

* [ADR-0042](0042-carry-a-declared-constraint-as-a-value-object.md) — the record this one departs
  from, and the reason the departure is narrow.
* [ADR-0098](0098-offer-every-oneof-in-both-shapes.md) — the companion rule on the shape of `OneOf`,
  held by the same kind of guard.
* [ADR-0099](0099-offer-oneof-wherever-the-generator-would-otherwise-build.md) — where a value set is
  offered at all.
* [Issue #185](https://github.com/Reefact/just-dummies/issues/185) — the audit item whose review
  found the defect on `AnyPattern`.
