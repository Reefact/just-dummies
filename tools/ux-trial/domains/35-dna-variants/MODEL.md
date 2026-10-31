# DNA variant normalisation — domain model (`DnaVariants.Domain`)

Coordinates are 1-based on the reference. A variant replaces the reference allele starting at `Position`
by the alternate allele; an insertion (empty reference allele) inserts before `Position`.

## Types

| Type | Kind | Invariants (constructor guards) |
|---|---|---|
| `Accession` | value object | not null; format `^[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}$` (e.g. `NC_000001.11`) |
| `NucleotideSequence` | value object | not null, not empty; alphabet `ACGTN`, case-insensitive, stored upper-case |
| `Allele` | value object | not null; may be empty; alphabet `ACGT` (no `N`); at most 10 000 bases |
| `ReferenceSequence` | entity (id = `Accession`) | accession, name (not blank), sequence not null |
| `Variant` | value object | accession/alleles not null; `Position >= 1`; reference allele != alternate allele |
| `VariantKind` | enum | Substitution, Insertion, Deletion, Duplication, DeletionInsertion |
| `NormalizationConvention` | enum | LeftAligned (VCF), RightAligned (HGVS 3' rule) |
| `NormalizedVariant` | value object | defined convention and kind; kind agrees with the variant's shape (Duplication only for an insertion) |
| `VariantNormalizer` | domain service | reference not null; defined convention |
| `VariantCatalog` | aggregate root | one reference + convention; at most 10 000 variants; no overlaps |
| `ReferenceMismatchException` | exception | variant on another accession, out of bounds, or reference allele disagreeing with the reference |

## Behaviour

- `NucleotideSequence.Slice(start, length)`, `ReverseComplement()`, `GcContent`.
- `ReferenceSequence.Verify(variant)` throws `ReferenceMismatchException` unless the variant lies on it and its
  reference allele reads exactly what the reference holds at that position.
- `Variant.Kind`, `End`, `LengthChange`, `Overlaps(other)`; equality is textual (same position and alleles).
- `VariantNormalizer.Normalize(variant)`: verify, trim the shared suffix then prefix of the alleles, and for a pure
  insertion/deletion shift it through the repeat (leftmost or 3'-most); an insertion copying the bases just before
  it is classified `Duplication`. `AreEquivalent(a, b)` compares normalised forms: identity is the effect on the
  reference, so `3:CA>-` and `7:CA>-` in `GGCACACATTT` are the same deletion.
- `NormalizedVariant.ToHgvs()`: `g.9T>G`, `g.7_8del`, `g.7_8dup`, `g.2_3insCA`, `g.4_6delinsTT`.
- `VariantCatalog.Record(variant)` returns `false` for an equivalent already recorded, throws
  `InvalidOperationException` on overlap or when full; `Contains`, `Remove` by equivalence; `ApplyAll()` builds the
  alternate sequence (refuses one that would delete everything).
