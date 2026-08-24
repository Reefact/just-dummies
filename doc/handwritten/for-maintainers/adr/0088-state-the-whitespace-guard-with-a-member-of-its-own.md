# ADR-0088 | State the whitespace guard with a member of its own

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0088-state-the-whitespace-guard-with-a-member-of-its-own.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-24
**Accepted:** 2026-08-24
**Decision Makers:** Reefact

## Context

`string.IsNullOrWhiteSpace` is the most common way a .NET constructor rejects a string that carries no content.
The scaffolder reads that guard, and until now emitted `.NonEmpty()` for it — a floor of one character. The two
are not the same: a value of one space satisfies the floor and the guard rejects it.

The fold rested on a premise written into the specification, that an unconstrained `Any.String()` draws only
ASCII letters and digits, which makes an all-whitespace draw impossible. [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md)
falsified it — the filler is the whole of ASCII, whitespace included — and [ADR-0076](0076-let-a-declared-maximum-steer-the-size-draw.md)
made a declared maximum steer the draw, so a short ceiling makes short strings ordinary. Neither record revisited
the line, and it still reads as a justification.

The consequence is measurable rather than theoretical. Under a four-character ceiling roughly one draw in eighty
is entirely whitespace. Against a domain that guards with `IsNullOrWhiteSpace`, a scaffolded generator therefore
compiles, raises no rule, reports the parameter as inferred, and is rejected by the constructor it was written
for — the failure mode [ADR-0083](0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) exists to prevent, reached
here by reading a guard rather than by failing to read one.

The library has no member that states the guard. Its nearest neighbours each miss:

* `NonEmpty()` is the floor the fold already used, and the one the guard outlives.
* `AlphaNumeric()` rejects whitespace, and also the punctuation the guard admits — it constrains a domain the
  guard never spoke about.
* `WithoutAlpha()` and the subtractive pair remove a family from every position, where the guard asks only that
  one position not be blank.

Two further facts bear on the shape of any member added here.

**The library already carries a narrower notion of whitespace.** The `Whitespaces` family is the space and the
tab — the readable pair, chosen so a test can rely on seeing a separator, and mirrored by the regular subset's
own `\s`. The BCL's `char.IsWhiteSpace`, which `IsNullOrWhiteSpace` is defined in terms of, accepts six ASCII
characters: the pair, and the four line and page breaks. Measured on unconstrained draws under a short ceiling,
two blank values in three are blank only by way of a character the family does not name.

**`ADR-0086` forbids the approximation that would avoid all this.** A guard helper whose semantics the constraint
table cannot carry is left unread rather than mapped to something close, and both guard-library spellings of the
whitespace rejection are unread today for exactly that reason, with a comment naming the missing member.

## Decision

`Any.String()` gains `NotBlank()`, a constructive constraint requiring at least one character the BCL's
`char.IsWhiteSpace` rejects, and the scaffolder reads every spelling of the whitespace guard as that member
rather than as `NonEmpty()`.

## Rationale

**The alternative to a member is a permanent refusal, and the guard is too common to refuse.** ADR-0086's rule
leaves an unmappable helper unread, and that is the right answer while nothing states the semantics — it is
what the two guard-library rows do now. But `IsNullOrWhiteSpace` is not a corner of the idiom table: it is the
ordinary way a .NET domain says a string must carry content. Answering the most common guard in the language
with a compilation block, permanently, spends the scaffolder's usefulness to protect a gap the library could
simply close. Adding the member is what turns a standing refusal into an exact read, and it is the only route
that does so without approximating.

**Correctness is not what this library bounds.** [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md)
bounds what the generator attempts and never what it guarantees once it does. A drawn value satisfying every
declared constraint is the guarantee, and a domain-rejected draw from a clean recap is a breach of it. The member
is therefore not an increment of ambition to be weighed against the bound; it is the repair of a correctness
defect the bound never covered.

**It states the guard exactly, which is the whole point.** The predicate is the BCL's own, so the constraint
does not narrow a domain the guard left open — interior whitespace stays legal, punctuation stays legal — and
does not leave open a domain the guard closes. That exactness is what earns it a row in the closed table under
ADR-0086's own rule: measured, not approximated. A member built on the narrower family predicate would have
satisfied the letter of the same rule while leaving two blank draws in three still reaching the domain, which
is why the wider predicate is part of the decision rather than an implementation detail.

**Two notions of whitespace are the honest outcome, and naming them is cheaper than unifying them.** Widening
the `Whitespaces` family to the BCL's six would move every seed that draws from it, against the replay promise
of [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md), and would cost the family the legibility
ADR-0075 chose it for. The two serve different jobs — one is an alphabet a draw is narrowed to, the other a test
a value must pass — and a reader meets the difference at the one place it matters, where declaring both
contradicts and the message names each side.

**It is not a character family, so the queue ADR-0075 closed stays closed.** That record admits a named alphabet
only where a published standard defines it, and sends anything a project invents to `WithChars`. This member
names no alphabet: it constrains the assembled value, on the same axis as a length rather than the alphabet
axis, and every character of a draw remains free. The family list is untouched, and so is the rule that every
family narrows ASCII.

**Constructive rather than rejective, because [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md)
already decided how to tell.** The constraint describes a value the generator must build, so it is built —
never drawn and retried. On the value-set path the same declaration filters the supplied pool, which is what
that record means by offering a constraint where the generator can satisfy it.

**An anchored literal answers for itself.** A prefix, a suffix or a contained value that already carries a
non-blank character satisfies the guarantee, and the constraint then asks nothing of the filler and judges no
alphabet. That keeps [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) intact:
the literal is read to decide what the draw must supply, never rejected for what it contains.

## Alternatives Considered

### Leave both spellings unread, as ADR-0086 already does for the guard libraries

The standing answer, and the one requiring no new public surface: the scaffolder blocks compilation with the
verification mark and the developer writes the constraint themselves.

Rejected on how much it costs at how common a guard. The mark is the right answer for an idiom the table cannot
carry, and `IsNullOrWhiteSpace` is one the table can carry once a member exists — so choosing the mark here is
choosing to leave a closable gap open forever. Every scaffold of a validated string type would carry a block a
developer resolves by hand, which is the outcome the tool exists to avoid, and the base would still owe a reader
an explanation of why the most ordinary guard in .NET is the one it cannot read.

### Map the guard to `AlphaNumeric()`

Available today, rejects whitespace, and needs no new member.

Rejected because it states an invariant the domain never declared. A guard on blankness says nothing about
punctuation, and a generator that never draws a hyphen for a parameter whose domain admits one certifies less
than the test appears to. It is the approximation ADR-0086 names and refuses, and it would trade a wrong value
for a silently narrowed domain rather than fix anything.

### Build the member on the existing `Whitespaces` family predicate

The narrower reading, and the one that would leave the library with a single notion of whitespace.

Rejected on measurement. The family is the space and the tab; the guard rejects four more characters, and those
four account for two of every three blank draws a short ceiling produces. A member built this way would state
the guard inexactly in the permissive direction — the one direction that matters — and the defect it exists to
close would survive it in the majority of cases.

### Widen the `Whitespaces` family to the BCL's six, then build on it

Unifies the two notions, and removes the divergence a reader has to learn.

Rejected on cost and on purpose. It moves every seed that draws from the family, which ADR-0049 makes a major
version, and it takes the legibility ADR-0075 deliberately chose — a family whose job is "a separator I can rely
on" should not hand back a form feed. The divergence is real but it is between an alphabet and a test, which are
different things that happen to share a word.

## Consequences

### Positive

* The most common string guard in .NET reads exactly, where it previously read wrongly, and the two
  guard-library spellings stop blocking compilation.
* A defect class closes at its source: the value the scaffolder emits satisfies the domain that will judge it.
* The specification's falsified premise is removed rather than left standing beside the records that falsified
  it.
* Callers writing generators by hand gain the constraint too — the guard was unstatable for them as well.

### Negative

* One more member on the string surface, with its baseline entry on both target frameworks, three analyzer arms,
  a documentation twin and its place in the parity table.
* The library carries two notions of whitespace, and documentation is the only thing keeping them apart.
* `NotBlank()` has no `Any.Char()` counterpart, so the two surfaces are no longer symmetric — deliberately, since
  a single character is blank or it is not, which the existing families already say.

### Risks

* A caller may read `NotBlank()` as forbidding interior whitespace and use it where they meant a family or a
  pattern. Mitigated on the member's own documentation, where they meet it.
* The analyzers hold their own copy of what each constraint admits, and nothing checks the two agree — the risk
  ADR-0075 already recorded, now with one more member on it.

## Follow-up Actions

* Revisit whether the specification's base table should emit this member for a `string` parameter with no guard
  at all. It is a different question — nothing was read there — and this record does not settle it.
* The regular subset draws `\s` from the readable pair, so a pattern and this constraint disagree about
  whitespace in the same way the family does. Flagged rather than settled, alongside the divergence ADR-0075
  already left open for the free positions of a pattern.

## References

* [ADR-0086](0086-read-the-guard-helpers-of-named-libraries.md) — the "measured, or not in the table" rule, and
  the two rows whose comments named the member this record adds.
* [ADR-0075](0075-draw-characters-from-the-whole-of-ascii.md) — the family rule this member is not subject to,
  and the widened default that falsified the fold's premise.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — why the constraint is built
  rather than filtered, and why it reaches the value-set path.
* [ADR-0079](0079-constrain-what-a-dummy-draws-never-the-literals-it-was-given.md) — the exemption an anchored
  literal keeps.
* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the bound this repair sits outside.
