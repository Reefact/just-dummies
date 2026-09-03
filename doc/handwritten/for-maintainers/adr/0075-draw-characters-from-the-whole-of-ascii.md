# ADR-0075 | Draw characters from the whole of ASCII, and narrow only by a named family

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0075-draw-characters-from-the-whole-of-ascii.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-18
**Accepted:** 2026-08-18
**Decision Makers:** Reefact

## Context

`Dummy.String()` and `Dummy.Char()` each carry a **character family**: a once-per-generator constraint naming
the characters a draw may use. The families are `Alpha`, `Numeric` and `AlphaNumeric`, alongside two
casings, plus `WithChars` on the string and `OneOf` on the character. Unconstrained, both generators draw
from ASCII letters and digits — 62 characters — and `CharacterPools` holds that definition once so the two
cannot drift apart on it. `DummyChar`'s documentation states that its families mirror the string's, and a
reflection guard in `SurfaceParityTests` holds each builder to the constraint set its family declares.

No named family reaches a character that is neither a letter nor a digit. Reaching `:` means supplying the
characters oneself — `Dummy.Char().OneOf(':')`, or `Dummy.String().WithChars(...)` — which answers *"these exact
characters"* and not *"a character that is not alphanumeric"*. This is the report that opened the question.

The library nevertheless already draws beyond letters and digits, elsewhere. The regex generator resolves
every position a pattern leaves **free** — the dot, a shorthand, a negated class — to printable ASCII
(0x20–0x7E), and `RegexAlphabet` records the reason: restricting the free positions keeps generated dummies
legible instead of scattering arbitrary Unicode. So `Dummy.StringMatching(".")` yields `:` while no character
family can, and two doors to the same product answer the same question differently.

Four further facts bear on the choice.

**A dummy's worth is what it exposes.** A value nobody asserts on is still passed to the code under test,
and it certifies whatever it survives. A generator that only ever draws letters and digits certifies
nothing about a carriage return, a NUL or an escape character — the characters most likely to break
parsing, storage and logging. An order reference that must not contain `\r\n` has that invariant, and today
nothing in a test declares it, because nothing can produce a counter-example.

**Changing an unconstrained draw is a major version.** [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md)
promises that a seed replays across patch and minor versions, enforced by a golden master pinning both the
values each factory produces and the draws it consumes. Widening the default moves every value every
committed seed replays.

**Beyond ASCII, reproducibility itself is at stake.** Unicode categories move with the runtime's version,
so a family defined by them could draw differently on two target frameworks — the cross-target-framework
guarantee `tools/justdummies-check` compares byte for byte. A surrogate is half a character: `WithChars`
already rejects one for that reason.

**Two analyzers mirror the family-to-alphabet mapping.** JD015 and JD029 each reason about what a declared
family admits, and an analyzer references no JustDummies assembly and cannot call one. A family they do not
name is not misreported; it is simply not read.

Finally, .NET's `char.IsPunctuation` is narrower than the printable non-alphanumeric block: it classifies
`+`, `<`, `=` and `$` as symbols. POSIX `[:punct:]` is that whole block minus the space, all 32 characters.

## Decision

An unconstrained `Dummy.Char()`, and the unconstrained filler of `Dummy.String()`, draw from the whole of ASCII
(0x00–0x7F), and every character family — `Printable`, `NonPrintable`, `Whitespaces`, `Alpha`, `Numeric`,
`AlphaNumeric`, `Punctuation`, `Hexadecimal`, and the subtractive `WithoutAlpha` / `WithoutNumeric` — only
ever narrows that set.

## Rationale

**A default that draws only letters and digits certifies nothing.** The point of an arbitrary value is that
the code under test had no say in it. Restricting it in advance to the characters that never cause trouble
removes precisely the evidence the draw exists to produce: the test passes, and it has established nothing
about the values the code will actually meet. Widening the default is therefore not a convenience — it is
what makes an unconstrained draw mean something.

**The invariant belongs at the call site, and now it can be written there.** An order reference that must
not contain a line break has a real invariant; a column that holds at most 50 characters has one. Under
this decision a test states it — and a test that does not state it gets a value that will find out. That is
the same contract the rest of the library applies: constraints express what the surrounding code requires,
and the generator supplies the rest arbitrarily.

**ASCII is where the bound belongs, and localisation is the reason.** The step past ASCII is not one more
notch of ambition, it is a different problem: the pool would depend on the runtime's Unicode version, which
puts the cross-target-framework seed guarantee at risk, and surrogates make a `char` stop being a
character. Stopping at 128 keeps every draw explainable, reproducible on every leg, and free of
combining-mark and normalisation questions no test-support library should be answering. Anything beyond it
is a caller-supplied alphabet — `WithChars`, `OneOf` — which is the honest shape for text a domain
actually uses.

**Making the widest set the default is what lets every constraint narrow.** The previous draft of this
record kept letters and digits as the default and added wider families on top; that made `Printable()` a
constraint that *enlarged* the draw, which is not what a constraint is, and it forced `Whitespaces()` and
`NonPrintable()` into being documented exceptions to the rule. Starting from the whole of ASCII closes the
model: the default is the top of the lattice, every family is a subset, and there is no exception to
explain. `Printable()` becomes a real constraint rather than a no-op naming the default.

**Symmetry across the two generators is already a commitment.** `CharacterPools` exists so the string
filler and the character pool cannot disagree, `DummyChar` documents its families as mirroring the string's,
and the parity guard fails a builder whose family set drifts. Every family lands on both.

**The member list is bounded by the shape of ASCII, not by taste.** The universe splits into blocks —
controls, space, digits, upper, lower, punctuation — and the families are their useful unions. `Hexadecimal`
is the one member that cuts across them; it earns its place because a published standard defines it
(RFC 4648, "Base 16"), and that criterion is what keeps the door from becoming a queue: an alphabet a
standard defines may be named, an alphabet a project invents is `WithChars`. The subtractive pair
accumulates, so `WithoutAlpha().WithoutNumeric()` is the third useful combination without a third member.

## Alternatives Considered

### Keep letters and digits as the default, and add wider families on top

The previous draft of this record, and the smallest change: no existing seed moves, and `Punctuation()`
answers the original report.

Rejected once the model was examined rather than the symptom. It leaves the default certifying nothing, and
it makes `Printable()` a constraint that widens the draw — reintroducing, as a named member, exactly the
inconsistency a reader notices first. `Whitespaces()` and `NonPrintable()` then have to be documented as
exceptions to the narrowing rule, and a rule with a growing exception list is not a rule.

### Take printable ASCII (0x20–0x7E) as the default

Considered seriously, and adopted in an intermediate version of this record: it is the universe the regex
generator already uses, every dummy stays visible in a failure message, and no draw can corrupt a terminal.

Rejected because it hides the class of defect this decision exists to surface. A carriage return, a NUL and
an escape character are the characters most likely to break storage, parsing and log handling, and a
default that excludes them means no unconstrained test ever meets one. It also forces `Whitespaces()` — the
tab sits at 0x09 — and `NonPrintable()` to reach outside the default, so the narrowing rule keeps its
exceptions and only their number changes.

### Draw from the whole Basic Multilingual Plane, or from Unicode by category

The literal reading of "any char", and the general form of the family idea: `Letter()`, `Symbol()` and the
rest, defined as the BCL defines them.

Rejected on reproducibility and on scope. The pool would follow the runtime's Unicode version, so one seed
could draw different values on two target frameworks, against a guarantee this repository checks byte for
byte; a surrogate is half a character and cannot be drawn as one; and normalisation, combining marks and
locale-dependent casing are a problem a dummy library has no business owning. ASCII is the largest set that
is total, stable and explainable.

### Express the subtractive family as a flags enum

One member, `Without(Characters.Alpha | Characters.Numeric)`, instead of one per block — everything
expressible, nothing to extend later.

Rejected on style rather than on capability. It introduces a public enum where the whole surface is fluent
named methods, and it makes the call site read as configuration rather than as a sentence. Two members
cover the useful cases and compose into the third.

### Match `char.IsPunctuation` rather than POSIX `[:punct:]`

Considered because a .NET caller may reasonably assert with the BCL predicate, and will be surprised that a
family called `Punctuation` can draw `+`.

Rejected because it would split the printable block in two and leave `+ < = > | $ ^ ~` reachable by no
named family. The divergence is documented on the member instead, which is where a caller meets it. The
space stays out of the family for a different reason: it is the one character that disappears silently
under a `Trim()`, and a family whose purpose is "a separator I can rely on" must not draw one. The space
remains nameable through `Whitespaces()`.

## Consequences

### Positive

* An unconstrained draw certifies something. A test that passes with a dummy containing a control
  character has established that the code tolerates one; today it establishes nothing.
* Every character family narrows the default, with **no exception** — the model closes, and `Printable()`
  is a real constraint rather than a name for the default.
* One universe across the library's character surface, held by `CharacterPools` and checked by the parity
  guard, instead of one per generator.
* An anchored fragment carrying punctuation is legal under `Printable()`, which no named family admitted
  before, and JD015 gives the same verdict at build time as the run time does at declaration.
* "May a dummy be `:`" is answered by the default itself, and the answer no longer depends on which door
  the caller came through.

### Negative

* A **major version**. Every seed replays different values, the golden master moves, and every existing
  test that uses an unconstrained string or character draws something else.
* A default draw can contain `\0`, `\r`, `\n` and `\x1b`. The last opens an ANSI escape sequence, so a
  failing test can corrupt the terminal that reports it — damage to the reporting channel rather than to
  the code under test. The library escapes control characters in its own diagnostic output; how a test
  framework renders a value is outside its reach.
* Nine members on two builders where there were three, each carrying a baseline entry, two analyzer arms,
  a property-test family index and a documentation twin.
* `Punctuation()` deliberately disagrees with `char.IsPunctuation`, and documentation is the only defence.

### Risks

* A consumer may read the widened default as the library having become a fuzzer, and constrain everything
  defensively — which would cost exactly the evidence the change buys. Mitigated by documenting the
  invariant-at-the-call-site framing rather than presenting the default as a stress test.
* The regex generator still resolves its free positions to printable ASCII, so the two doors diverge again,
  in the opposite direction from the one that opened this question. Flagged below rather than settled here.
* The analyzers' copy of the family mapping is the only one outside the library, and nothing checks that
  the two agree.

## Follow-up Actions

* Decide whether `RegexAlphabet` follows this decision — whether a pattern's free positions draw from ASCII
  or stay printable. The argument cuts both ways: the pattern is itself an explicit constraint, so its
  default may legitimately differ; but two universes are what this record set out to remove.
* Ensure the library escapes control characters wherever it renders a drawn value — conflict messages, pool
  inspections, the seed golden master, which already carries an `Escape` for its own file format.
* Record the size axis separately: the same "widest default, explicit narrowing" question applies to length
  and count, where a declared maximum does not currently steer the draw (ADR-0029).

## References

* [ADR-0046](0046-bound-the-generators-ambition-never-its-correctness.md) — the rule that asks whether a
  refusal is the honest answer, and the reason this record states where the universe stops.
* [ADR-0049](0049-replay-a-seed-across-patch-and-minor-versions.md) — why this is a major version, and the
  cross-target-framework guarantee that rules Unicode out.
* [ADR-0033](0033-decide-a-constraint-surface-by-constructive-versus-rejective.md) — the refusal to leave a
  domain decision to the caller's own filtering, which this record follows.
* [ADR-0008](0008-generate-strings-from-a-home-grown-regular-subset.md) — the generator whose free
  positions draw from printable ASCII, and the divergence left open above.
* `JustDummies/CharacterPools.cs` and `JustDummies/RegexAlphabet.cs` — the definitions this decision
  unifies.
