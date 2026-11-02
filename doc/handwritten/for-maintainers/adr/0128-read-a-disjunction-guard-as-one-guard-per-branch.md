# ADR-0128 | Read a disjunction guard as one guard per branch

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0128-read-a-disjunction-guard-as-one-guard-per-branch.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-02
**Accepted:** 2026-11-02
**Decision Makers:** Reefact

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

[ADR-0060](0060-seed-generators-from-constructor-guards.md) reads a closed set of guard idioms and keeps the
matching conservative: one parameter, no boolean composition, constant operands. §5.3 states the rule as a
condition that "contains no `&&` or `||`", and a guard outside it marks its parameter `to verify` and blocks the
build ([ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md)). The same guard written as two
`if`s, or through the `ThrowIf…` helpers, is read.

[ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) lets the reader change only against a
report from the field: a real constructor, the idiom it uses, what the engine did with it, a corpus shape, and
cases for what the change reads or refuses. Two reports have arrived. The first developer-UX trial met the range
guard written as one condition in ten of its twenty domains
([CLI-C4](../../../../tools/ux-trial/REPORT.md#cli-c4)): `if (month is < 1 or > 12)`, `if (month < 1 || month > 12)`.
The second met it in eighteen of its twenty contexts, at every profile, and measured it as the largest cost of the
trial ([CLI-1](../../../../tools/ux-trial/REPORT-2.md#cli-1), [CLI-2](../../../../tools/ux-trial/REPORT-2.md#cli-2)):
nine to twenty-three parameters per domain needed a hand-written `Between`, and the shapes went past a numeric
range — `double.IsNaN(k) || k < 1.0 || k > 5.0`, `string.IsNullOrWhiteSpace(code) || code.Length > 12`,
`items.Count == 0 || items.Any(i => i is null)`, and a single relational pattern, `value.Length is > 12`. Several
verifiers noted that a throwing `if (A || B)` is exactly `if (A) throw; if (B) throw;`, which the engine already
reads.

[ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) asks, before the generator is made cleverer,
whether the honest answer is a refusal, and treats a mechanism nobody can reason about from the call site as
needing a decision. It never bounds correctness: a wrong constraint reported as inferred is a defect, and the
unsafe direction of a reading is the one that narrows the draw past what the constructor demands.

## Decision

A throwing guard whose condition is a disjunction — `||`, or an `or` pattern — is read as one guard per branch,
each branch against the closed set as if it stood alone, and a single relational or constant pattern as the
comparison it spells; a conjunction stays unread.

## Rationale

**A disjunction splits by construction, not by heuristic.** `if (A || B) { throw … }` refuses to build the object
whenever `A` holds and whenever `B` holds, which is what `if (A) { throw … } if (B) { throw … }` says, and the second
form is what the closed set reads today. Reading the first as the second derives nothing: it is a rewrite that
holds for every `A` and `B`, with no case for the two to disagree on. The constraint that comes out is exactly the
one the two-`if` spelling earns, so ADR-0060's bias — under-report rather than misfire — is untouched.

**The same argument forbids splitting a conjunction.** `if (A && B) { throw … }` refuses only where both hold; a
value for which `A` holds and `B` does not is accepted. Reading `A` alone would constrain the draw to values the
constructor never refused — narrower than the domain, under a recap reporting it inferred, and silent, since every
draw still constructs. That is the wrong-constraint outcome ADR-0046 never bounds, so `&&` keeps the mark, and so
does a disjunction nested where it is no longer the whole condition, `!(A || B)`, which is a conjunction in
disguise.

**Each branch answers the questions a whole condition answered.** The one-parameter rule, placement above every
write to that parameter, the constant operand, the closed set itself: each is asked of a branch, so `a < 0 || b < 0`
is two guards on two parameters, a branch the set does not know marks the parameters it mentions and the other
branches still contribute what they read, and a branch adding nothing — `double.IsNaN(k)`, which the generator
never draws — adds nothing. Nothing in the placement layer changes: the branch sits where its condition sat.

**A pattern is the comparison it spells.** `month is < 1 or > 12` is `month < 1 || month > 12` in the other
grammar the language offers for the same test, and `value.Length is > 12` is `value.Length > 12`. A relational
pattern is allowed on the built-in numeric types alone, where the comparison is the built-in one, so none of the
user-defined-operator questions the equality rows ask arise.

**The field report is the one ADR-0085 asks for, twice over.** Twenty-eight domains between the two trials, the
largest measured cost of the second, a workaround users could not discover because the user page never listed
the shapes the reader knows (#383), and a rewrite the verifiers themselves proposed. The corpus carries a shape per
spelling — `||`, the `or` pattern, three branches, two parameters, a branch outside the set — and the resolver
suite the cases.

## Alternatives Considered

### Keep refusing, and document the workaround

Considered because the mark is a designed outcome under ADR-0085, and splitting the guard into two `if`s or the
`ThrowIf…` helpers gets it read.

Rejected because the cost fell on eighteen of twenty contexts and the mark asks the developer to write by hand a
constraint the engine reads exactly from the other spelling. ADR-0085 keeps the mark as the standing answer for
what the surface does not cover; a shape the surface covers by construction, under two field reports, is what
its own entry procedure exists to admit.

### Read a conjunction as well, taking the weaker side

Considered because `A && B` guards are met in the field too — a nullable enum tested `is { } q && !Enum.IsDefined(q)`
in the second trial.

Rejected because no reading of a conjunction is exact: taking either operand narrows the draw past the domain,
and taking neither is the mark. §5.3 names that cost as deliberate, and the field reports asked for the disjunction.

### A boolean normaliser over the whole condition

Considered because it would read `!(A || B)` and the shapes De Morgan reaches.

Rejected under ADR-0046: a normaliser is a mechanism nobody reasons about from the call site, for shapes the
reports did not meet. A disjunction at the top of the condition is the whole of what was asked.

## Consequences

### Positive

* A range guard written as one condition, in either grammar, reads as the range it is, and the largest cost of
  the second trial ends.
* The rule a user can hold in their head stays short: a `||` is its branches, a `&&` is not read.

### Negative

* A guard that was marked `to verify` is now read, so a file re-scaffolded with `--force` loses a sentinel the
  developer may have resolved by hand with a different base. The chain written is the one the guard states.

### Risks

* A branch read where the whole condition should have been refused. Mitigation: nothing is read of a branch that
  the closed set would not have read standing alone, and the placement questions are asked of each branch.

## Follow-up Actions

* `items.Count == 0 || items.Any(i => i is null)` reads whole once the null-element row exists (#403).
* The user page lists the shapes the reader knows and the ones it does not, including this one (#383).

## References

* Issue [#364](https://github.com/Reefact/just-dummies/issues/364) — the decision this record carries.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md) — the closed set, whose "no boolean composition" this
  record amends for a disjunction only.
* [ADR-0085](0085-change-the-guard-reader-only-against-a-field-report.md) — the entry procedure this change meets.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — bound the ambition, never the correctness.
* [`any` specification](../specifications/justdummies-tool.md), §5.3 — the closed set and its placement rules.
