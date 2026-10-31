# Manuscript reconstruction — domain model

Namespace `ManuscriptReconstruction.Domain`. Readings are normalised (trimmed, inner whitespace collapsed).

## Value objects
- **WitnessSiglum** — 1–12 chars, `^[A-Z][A-Za-z0-9.]*$` (e.g. `A`, `B2`, `Vat.3225`). Equality by value.
- **PassageReference(book, chapter, line)** — book 1–99, chapter 1–999, line 1–9999; ordered; `Parse("3.12.4")` throws `FormatException`.
- **Variant(passage, witness, text, kind, hand)** — hand 1–9 (1 = original scribe), text ≤ 500 chars.
  `Omission` ⇔ empty text; every other kind needs text; a `Correction` is never by hand 1.
- **Lacuna(witness, from, to, cause)** — from ≤ to, cause non-blank; `Covers(passage)`, `Overlaps(other)`.
- **CopyRelation(exemplar, copy, confidence, sharedErrorCount)** — exemplar ≠ copy, confidence in (0, 1], count ≥ 0.
- **AttestedReading(passage, text, witnesses)** — one alignment column: ≥ 1 distinct witness (provenance), sorted.

## Entities
- **Witness(siglum, repository, datedFrom, datedTo, folioCount)** — years −800..1600, from ≤ to, folios ≥ 1.
  `IsCertainlyOlderThan(other)`: its span ends before the other's begins.
- **EditorialHypothesis(id, passage, proposedReading, editor, supportingWitnesses, isConjecture, rationale)** —
  non-empty id, editor and rationale (≤ 2000); witnesses distinct; a conjecture cites none, otherwise ≥ 1.
  Status `Proposed` → `Accepted` (via the tradition) → `Proposed` (withdraw); `Reject(reason)` only from `Proposed`.

## Aggregate: Tradition(workTitle)
- `AddWitness` — refuses a duplicate siglum.
- `RecordLacuna` — unknown witness, overlapping lacuna, or readings already inside the gap are refused.
- `RecordVariant` — refuses an unknown witness, a passage inside the witness's lacuna, a second reading by
  the same hand, and a correction with no earlier hand's reading to correct.
- `FinalReadingOf(witness, passage)` — the latest hand's reading, or null.
- `Align(passage)` — groups final readings into `AttestedReading`s, largest group first.
- `Propose` — every cited witness must actually read the proposed text there. Competing hypotheses coexist.
- `Accept(id)` / `Withdraw(id)` — at most one accepted hypothesis per passage; the others stay legitimate.
- `CompetingReadingsAt(passage)` — every non-rejected hypothesis at the passage.
- `SharedErrors(a, b)` — passages with an accepted reading where both witnesses agree on the same
  other reading (conjunctive errors, the evidence for filiation). Refuses a == b.
- `DeclareCopy(relation)` — refused when the copy is certainly older than its exemplar, already has an
  exemplar, or the relation closes a cycle. `AncestorsOf(witness)` walks the stemma upward.
- Unknown witness or hypothesis → `KeyNotFoundException`; rule violations → `InvalidOperationException`;
  bad constructor input → `ArgumentException` / `ArgumentOutOfRangeException` / `ArgumentNullException`.
