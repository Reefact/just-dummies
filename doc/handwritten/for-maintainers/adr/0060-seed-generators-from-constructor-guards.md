# ADR-0060 | Seed generators from constructor guards, and leave the rest as a compile error

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0060-seed-generators-from-constructor-guards.fr.md)

**Status:** Accepted
**Proposed:** 2026-07-30
**Accepted:** 2026-08-09
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md), the document this record was extracted from.

## Context

Unconstrained generators draw their full domain: the string generator yields zero to sixteen
characters, so it can return the empty string, and the integer generator draws the whole range
including negatives (§14.5).

Domain constructors commonly reject part of that domain.

This was measured on a real validating factory from this repository: an unconstrained string
generator composed onto it threw 594 times in 10 000 draws, and 557 on an independent re-run —
roughly one in seventeen, the rate an unconstrained draw over the lengths 0 to 16 predicts (§17).

Guard clauses at the head of a constructor are the dominant validation idiom in the code this tool
targets.

The tool has the constructor body as source for any type in the developer's solution, and does not
for a type coming from a package.

Some invariants are not expressed as guards at all — validation delegated to a helper, a guard
library, or a rule spanning two parameters.

The developer runs the tool and opens the resulting file within the same minute.

## Decision

The engine derives constraints from a closed set of recognised constructor guard clauses, and emits
an identifier that does not exist for any parameter whose generator it cannot infer.

## Rationale

Without guard reading the tool's default output is not merely imprecise, it is harmful: it
manufactures, inside the developer's test suite, the intermittent failure the library exists to
eliminate. One failure in seventeen is worse than no tool at all, because it discredits the library
at the moment of first use.

A closed, syntactic set bounds the risk. Reading guards is not inference about intent; each
recognised form maps to exactly one constraint, and anything outside the set is ignored.
Conservative matching — one parameter, no boolean composition, constant operands — under-reports
rather than misfires, which is the correct bias here: a missing constraint yields a value the
constructor may reject and a visible failure, whereas a wrong constraint yields a value that
silently mis-exercises the test.

For the parameters that remain unresolved, a compile error is the cheapest signal available. The
developer is in the file, having just run the tool; the compiler names the parameter in its own
message, and that message reaches the editor, the error list and continuous integration alike. A
signal delivered later costs more, and one never delivered costs most.

Shipping a file that does not compile is defensible only because of [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md). A tool that owned its
output could not do it; a tool handing over a skeleton can, and stating the gap plainly is more
honest than a file that compiles and fails later.

## Alternatives Considered

##### Neutral generators, leaving all tightening to the developer

Considered because it makes the tool claim nothing it cannot prove, which is attractive for a
library built on precision.

Rejected on the measurement. The default output would fail intermittently for most validating
constructors, which is the highest-cost failure mode available and the one the library was built to
remove.

##### A run-time exception for unresolved parameters

Considered because the file then compiles, which is friendlier at first sight.

Rejected because it defers the signal past the moment the developer is looking at the file, and
converts a scaffolding gap into a test failure whose cause is a line they never read.

##### Omitting the unresolved parameter from the recipe

Considered because it is the most elegant of the three: the generator would simply require the
developer to supply that parameter.

Rejected because it is silent. The generator becomes partially usable without saying so, and the
gap surfaces as a null or a default deep inside a test.

##### A declaration file mapping types to their construction

Considered because it would let the developer teach the tool once, covering invariants no guard
expresses, and would make composition correct for value objects in general rather than only for
guarded ones.

Rejected for the first version because it converts the tool into a convention system, contradicting
the design rule that nothing be configured before first use. Left open in §16.

## Consequences

**Positive.** The emitted default works for the dominant validation idiom. Unresolved parameters
are impossible to overlook.

**Negative.** A scaffolded file may not compile until edited, which will surprise anyone expecting
scaffolding to produce working code. Invariants outside the recognised set still yield values the
constructor rejects.

**Risks.** The recognised set may match a guard whose meaning it mistakes, producing a constraint
that is wrong rather than absent — the one outcome worse than inferring nothing. Mitigated by the
conservative matching conditions and the same-axis conflict rule; the own-code test (§12) is the
check most likely to catch it, because it runs the emitter over code written for other reasons.

## Follow-up Actions

* Every addition to the recognised guard set needs a case in the resolver suite and, where
  possible, an instance in the own-code test.

## References

* §5.3, §5.5, §9, §14.5, §17 of this specification.

---
