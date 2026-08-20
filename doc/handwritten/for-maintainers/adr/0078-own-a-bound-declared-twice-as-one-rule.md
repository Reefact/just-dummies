# ADR-0078 | Own a bound declared twice as one rule, and narrow JD024 out of it

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0078-own-a-bound-declared-twice-as-one-rule.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-20
**Accepted:** 2026-08-20
**Decision Makers:** Reefact

## Context

JD024 reports a constraint that narrows nothing, in the `Constraints` category and at information severity.
Its recorded reason is that this is the only constraint family the run time never reports — every other
contradiction throws eventually and loudly — and that the case it exists for is an exclusion of a sentinel
the generator could never draw, which is silent today and starts mattering the day someone widens the
range. That reading is what places it at information rather than at warning: excluding a value the current
domain cannot produce is a legitimate defensive act.

JD024 is raised by a single analyzer, which is gated on an integer factory and reasons over an integral
domain. String chains and collection chains never receive it, and the analyzer that does read a string
chain's lengths reads only the maximum and the exact length, for a different question.

Bounds fold silently and monotonically in every family. A minimum keeps the larger of the two values, a
maximum the smaller; the losing call returns the generator unchanged. Nothing throws, and no run-time report
mentions it. On a chain that declares the same bound twice, exactly one of the two calls is therefore dead —
always the looser one — whichever order they are written in. In the loosening order the second call is
inert; in the tightening order the first is erased by the second.

Of the four combinations that shape can take — two writing orders across the integral and the
string-or-collection families — only one is reported today. On an integral scalar written from the tighter
bound to the looser, the second call leaves the domain unchanged and JD024 fires, saying the constraint is
already implied by the constraints declared before it. The other three are silent, and the silence follows
from which analyzer was written first rather than from any decision.

Declaring the bounds of a range separately is a documented feature of the library: a shared helper can set a
floor and a call site add a ceiling, which is what keeps a range decomposable. Both calls of the shape
considered here sit in a single fluent chain.

The library also ships exact aliases of a single bound — `NonEmpty` is a minimum length of one, `Positive` a
minimum of one — so a chain can declare the same bound twice under two different names.

The rules' own README states the severity taxonomy: errors are hard defects, warnings flag likely mistakes,
the information rules are conventions. This repository treats a change to the semantics of a diagnostic id
as an architectural decision rather than as a patch, and ADR-0077 has just settled which spelling rules the
JD set admits and at which severity.

## Decision

A bound declared twice on one fluent chain is reported by a rule of its own, at warning severity, in every
generator family, matched on the constraint's name; JD024 no longer reports that shape and keeps the
constraint that narrows nothing.

## Rationale

**One phenomenon deserves one id.** The dead call is the looser bound in both writing orders, and the
mistake behind it is the same one — a bound written twice, usually by a copy-paste or a merge. Splitting it
across two ids by writing order would hand the author a different diagnostic, in a different category page,
for a difference they did not make on purpose. That the two orders are reported by different mechanisms
today is an accident of implementation order, not a distinction anyone chose to draw.

**The reason JD024 sits at information does not transfer, so this rule does not inherit its severity.**
JD024 is at information because an inert constraint has a defensible reading: the author excluded a sentinel
before the range that could produce it existed. A bound written twice inside one chain has no such reading.
Both calls are in one expression, in front of the same reader, and the tighter simply erases the looser —
there is no future in which the erased call starts mattering. By the taxonomy the rules' README states, that
is a likely mistake and not a convention, which is the warning tier.

**Severity follows the failure mode, not the category.** ADR-0038 already settled that principle for this
rule set when it put a silent green at error and a probabilistic one at warning. Holding this rule to
information for symmetry with the other member of its family would be consistency of the wrong kind.

**Widening JD024 would make its own message false.** JD024 says a constraint changes nothing. In the
tightening order that is precisely wrong: the constraint the author wrote second changes the domain, and the
dead call is the one written first. An id whose message has to be read as untrue on half the cases it covers
has stopped being a stable handle for a rule.

**JD024 stands down so that one mistake draws one diagnostic.** In the loosening order on an integral scalar
both rules would otherwise describe the same call, and two diagnostics on one expression for one mistake is
noise that trains a reader to disable both. Narrowing JD024 to the exclusion case it was written for leaves
it saying exactly what its message says.

**Extending the rule to every family is a correction, not an expansion.** The fold is the same in all of
them and the run time is silent in all of them; only the analyzer coverage differs. Reporting a mistake on
an integer and not on a string, for the same mistake with the same consequence, is the kind of inconsistency
a user reads as a defect in the tool.

**Matching on the name rather than on the effect keeps the rule free of false positives, and puts the alias
case where it belongs.** A chain that reaches the same bound through two different names is writing the
bound twice in effect, but the alias is a legibility choice with a defensible reading — it says something
about intent that the explicit bound does not. That makes it a question about spelling, which ADR-0077 has
just decided how to treat, and not a question about a dead call. Matching on the name draws the line exactly
there, and it draws it where a reader can see it.

**Nothing else will report this.** The run time cannot: the fold is by design and throwing on it would break
decomposability, which the library maintains on purpose. JD024 does not, outside one of four cases. An
author who later deletes the surviving call believing it redundant changes the domain the test draws from,
and no mechanism in the product would have told them.

## Alternatives Considered

### Widen JD024 to cover the shape in every family

The loosening order is already exactly what JD024 describes, and the analyzer that raises it already walks
the chain it would need. Reusing the id would cost no new documentation pages, no catalogue constant and no
row in any table.

Rejected because JD024's message is false for the tightening order, where the second call is the one that
narrows and the first is the one that dies. Widening it would also move a shipped id's meaning, which this
repository treats as a decision to be recorded rather than a change to be made — and the record would have
to argue for a message that no longer matches the rule.

### Report at information severity, for consistency with JD024

Both rules concern a constraint that ends up doing nothing, they would sit in the same category, and a
reader scanning the rule table would find a coherent family.

Rejected: the taxonomy in the rules' README is about the failure mode, not about category membership. The
defensive reading that earns JD024 its information severity does not exist for two bounds in one chain, so
the symmetry would be visual only, and it would understate a mistake that nothing else reports.

### Match on the effect, so the alias forms are caught too

`NonEmpty().WithMinLength(8)` reaches a minimum twice and the first is dead, on exactly the same grounds as
the explicit pair. A rule that reasoned about the bound rather than the method name would cover it.

Rejected because the alias is not the same act. Choosing `NonEmpty()` says something the explicit bound does
not, and a warning on it would be heavy-handed. The question it raises is which of two correct spellings to
prefer, which ADR-0077 admits at information severity under its own conditions — a different rule, and one
that can be added later without disturbing this one.

### Leave the shape silent

The domain is well defined in every case, the drawn values satisfy every constraint declared, and no test
fails because of it.

Rejected because "well defined" is not the standard the set is held to — JD024 exists for a case that is
equally well defined. The author's belief about the bound and the generator's actual bound differ, nothing
in the product reconciles them, and the divergence surfaces only when someone edits the chain later.

## Consequences

### Positive

* One mistake draws one diagnostic, with one message, in every generator family.
* The three silent cases of four close, and the remaining silence is a decision rather than an accident.
* JD024 keeps exactly the scope its message describes, which makes its page easier to write and its
  suppression easier to reason about.
* The alias question is parked where ADR-0077 can answer it, instead of being decided implicitly here.

### Negative

* JD024's documented scope narrows, so both of its pages and both rule tables have to say so, and a
  suppression written against JD024 for a doubly-declared bound stops matching.
* A new id costs an English and a French page, a row in each rule table, a catalogue constant on the
  `catalog` release train, and an update to every count of the rules.
* A chain that redeclares a bound through an alias stays silent, which some readers will expect to fire.

### Risks

* The rule fires on a shape a code generator or a heavily parameterised helper could produce legitimately
  inside one chain; at warning severity that costs a suppression rather than a build. Nothing in the
  repository or the documentation writes that shape today.
* Narrowing a shipped id is only safe while the rules stay unshipped in release-tracking terms, which they
  do below 1.0; doing the same after the surface is frozen would be a breaking change.

## Follow-up Actions

* Open the issue specifying the rule — the four vocabularies, the two orders, and the alias case it stays
  silent on.
* Restate JD024's scope on its English and French pages once the new rule ships.
* Revisit the alias case against ADR-0077's criterion if it is ever wanted as a rule of its own.

## References

* [ADR-0038](0038-guard-the-recipe-versus-value-boundary-with-analyzers.md) — severity follows the failure mode.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the surface, and refuse at a nameable boundary.
* [ADR-0052](0052-publish-the-jd-rules-as-a-first-party-catalogue.md) — the catalogue every new id is published through.
* [ADR-0077](0077-admit-a-rule-that-reports-a-correct-spelling.md) — where the alias case belongs.
* [Issue #95](https://github.com/Reefact/just-dummies/issues/95) — the discussion this decision came out of.
