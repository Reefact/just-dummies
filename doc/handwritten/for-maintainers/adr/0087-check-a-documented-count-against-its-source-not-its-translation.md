# ADR-0087 | Check a documented count against its source, not against its translation

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0087-check-a-documented-count-against-its-source-not-its-translation.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-23
**Accepted:** 2026-08-23
**Decision Makers:** Reefact

## Context

The documentation states how many analyzer rules the package ships, in prose, on pages a reader
reaches before installing anything. The number is a fact the code owns: it is the count of distinct
`JDxxx` identifiers the shipped assembly raises.

**Measured on the tree, seven statements over five pages named a number the package had not carried
for some time** — 28, 29, 31 or 32, against the 33 it ships:

* the root `README`, the first page a visitor and the packaged listing both show;
* `packages/justdummies.en.md`, which stated the count three times in one file with three different
  numbers, only the middle one right;
* `guides/getting-started.en.md`, the page a newcomer follows;
* the `dum` specification's analyzer inventory, in both locales, which also had never gained the row
  for the thirty-third rule.

Two of them were disagreements between a page and its French twin, and the pair disagreed in **both**
directions: 31 and 28 in English against 33 and 29 in French. The specification drifted the other way
— its two halves stated the same wrong pair of numbers.

[ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) established a test
suite over this corpus: samples compile against the shipped packages, the shipped analyzers run over
them, every page is held to structural parity with its twin, and links resolve. **None of those
contracts reads a number.** The suite already references the analyzers assembly as a plain library
and reflects over it to run the rules, so the count is available where the pages are read.

A digit-sequence comparison between a page and its twin was the mechanism the defect report proposed.
Measured over this corpus, it flags 18 pairs to surface 2 real ones. The noise has three sources: the
French thousands separator, which writes `1 000 000` where English says "a million"; a token spelled
as code on one side and as plain text on the other, which survives one strip and not the other; and a
reference written in two forms across a pair.

The upstream repository carried a `tools/analyzer-count-check` guarding this fact. The extraction
deliberately did not port it, recording that `README.nuget.md` made no such claim — which was true of
that file and false of the root `README`.

The decision base, the release notes and the migration log all state counts that were correct on the
day they were written. The pages wrap at a hundred columns, and every count in the corpus is at least
two words long.

## Decision

A count the documentation states about the shipped product is held by the build to the value read
from the shipped assembly rather than to the value its translation states, on every page except those
recording what was true when they were written.

## Rationale

**A twin is not a source.** Two prose statements can disagree without either being authoritative, and
here both halves were wrong and wrong differently — so even a parity failure that fired would have
named a disagreement without naming the truth, leaving a maintainer to go and find the real number
anyway. Where the two halves drift in step, as the specification's did, a comparison between them
reports nothing at all. The assembly has no such failure mode: it is the thing the sentence is about.

**The source is already in the room.** ADR-0055 put the shipped analyzers in this suite's hands to run
the rules over samples; asking the same objects how many identifiers they raise adds no dependency, no
tooling and no second place to keep in step. The alternative mechanisms all need something new.

**Scope by default is what the defect argues for.** The count drifted because nothing watched prose,
and an inclusion list would extend that condition to every page written after the guard. Holding the
whole corpus and naming the exemptions one at a time — as this base already does for the grandfathered
rule pages — puts a page written tomorrow under the contract on the day it exists.

**A record edited to agree with today's code has stopped recording.** ADR-0055 says the product ships
28 rules and was right in August; the migration log explains a port made when a different number was
current. Their value is that they say what was believed then, so they are exempt by the same reasoning
that makes the live pages in scope.

The cost is that the contract must recognise a count written in prose, in two languages, which is
inference where the comparison to the assembly is exact. That cost buys a check that survives being
wrong in both locales at once, which is the failure the corpus actually produced.

## Alternatives Considered

### Compare the digit sequences of a page and its twin

Considered because it needs no knowledge of what any number means, extends a contract that already
exists, and would catch drift in any fact rather than this one alone.

Rejected on measurement and on principle. At 18 pairs flagged to find 2, it is a check that gets
suppressed and then deleted, and the three noise sources are all legitimate translation — a guard that
calls correct French a defect teaches the maintainer to stop reading it. On principle it is blind
exactly where this corpus failed: the specification's halves agreed with each other and disagreed with
the code.

### Check that a page does not contradict itself

Considered because one page stated the count three times with three values, which needs no translation
to be visibly wrong, and because it would have caught two of the seven statements on their own merits.

Rejected as subsumed. Comparing every statement to the assembly checks the three independently and
also catches the case this misses — a page perfectly consistent with itself and wrong throughout,
which is what the root `README` and the specification each were.

### Restore the upstream shell check

Considered because it existed, guarded this exact fact, and its absence is recorded as deliberate.

Rejected because it guarded one file against one phrasing, while the drift reached five pages in two
locales; and because a script outside the solution reads the packed output rather than the assembly,
so it would have to be told the number instead of asking for it.

### Stop stating the count in prose

Considered because a fact never retyped cannot drift, and the rule index is one link away on every
page that states it.

Rejected because the number is what makes the sentence useful: a reader deciding whether to install
wants the scale of what ships, not an invitation to go and count. Protecting the documentation by
making it less informative trades the defect for a worse page.

## Consequences

### Positive

* The count cannot drift unnoticed again, in either locale, on any page in scope.
* A rule added without a documentation page, or a page left behind by a rule that stopped shipping,
  fails the build — the identifier range is held as a set, not only as a total.
* A page written after this decision is under the contract from the day it exists.
* The failure names the file, the line, both numbers and the offending text, so the fix needs no
  investigation.

### Negative

* The contract must recognise the shapes in which a count is written, in two languages. A phrasing
  nobody has used yet escapes it until the patterns learn it, which makes detection inference where
  the comparison itself is exact.
* Four pages are named as exempt rather than derived, so a fifth kind of record would have to be added
  by hand.

### Risks

* A count can hide from the patterns. It already did once: a hard wrap put the number at the end of
  one line and its noun at the start of the next, and a line-at-a-time scan missed it. Reading
  paragraphs rather than lines answers that case and not the general risk.
* The exemption list is a place where a live page could be parked to silence a failure. It is small,
  and each entry carries its reason, which is the only defence a list of this kind has.

## Follow-up Actions

* Consider whether other facts the code owns and the prose repeats — the supported framework floor,
  the number of published packages — deserve the same treatment.
* The migration log records `tools/analyzer-count-check` as worth re-adding if the README ever
  advertised the count. It does, and the invariant is back in another form; the log stays as written.

## References

* [ADR-0055](0055-hold-the-user-documentation-to-contracts-the-build-checks.md) — the suite this contract joins.
* [ADR-0019](0019-split-the-justdummies-test-bed-between-example-and-property-suites.md) — where a test of this kind belongs.
* [Issue #120](https://github.com/Reefact/just-dummies/issues/120) — the defect report, and the parity mechanism it proposed.
