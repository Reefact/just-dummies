# Discovery notes — 35 DNA variants

## What I did

- Read MODEL.md and the domain, then `guides/getting-started`, `packages/justdummies-cli`,
  `generators/strings`, `guides/composition`, `guides/core-concepts` and `packages/justdummies-xunit`.
- Created `Tests.Discovery`, restored, and ran one command:
  `any generate Accession NucleotideSequence Allele ReferenceSequence Variant --entry-point any`.
- Resolved the `to verify` rows, added `[assembly: Reproducible]`, wrote 34 test cases (31 methods, one
  4-case theory) across accession, sequence, allele, variant shape and overlap, reference verification,
  normalisation (left/3' shift, trimming, duplication, HGVS) and the catalog. All pass, five runs in a row.

## Frictions and surprises, in order

1. The recap was excellent: every guard it could not read was named with file and line
   (`value: unread guard at Accession.cs, line 11: if (!Format().IsMatch(value))`), and the file refused to
   compile until I looked. I knew exactly what to do.
2. Accession: the guard is a `[GeneratedRegex]`. The CLI page says a regex is never translated and shows
   the exact three-line edit (`AnyString` -> `AnyPattern` on the factory and on the `Func<…>`). Followed it,
   compiled first time. The pattern fits the supported subset, so `Any.StringMatching` worked unchanged.
3. Allele: the scaffolded base was `Any.String().NonEmpty().WithMaxLength(10000)`. `NonEmpty()` comes from the
   string base table, not from a guard — but an empty allele is valid and meaningful here (insertions,
   deletions). It is only narrower, never invalid, but a reader of the recap could believe "NonEmpty" was
   inferred from my constructor. The `guard` word on the row covers the max length only.
4. NucleotideSequence/Allele alphabet: I hesitated between a pattern and `WithChars`; the strings page
   literally names "a DNA sequence" as the `WithChars` use case. Nice.
5. Variant: `reference.Equals(alternate)` was flagged on both alleles (good). The page's section
   "When a guard ties two parameters" gives three forms (start+duration, derive in `Generate()`, check
   digit) — none fits "the two must differ". I wrote a bounded redraw of the alternate inside `Generate()`
   (equal pins still reach the constructor). I am not sure this is the idiom the authors want.
6. HGVS tests: the accession appears in the output string. I hesitated whether to keep it drawn (then an
   `any` value reaches the assertion) or to pin it. I pinned a literal accession with `WithAccession(...)`.
   The docs discuss pins versus dependence well, but not this "echoed through" case.
7. First real failure: a test needing a sequence without `N` (it deletes the whole reference, and an allele
   cannot hold `N`). I wrote `s.WithBases(b => b.WithChars("ACGT"))`, a pure narrowing of the base
   `WithChars("ACGTN")`. Result:
   `ConflictingAnyConstraintException : Cannot apply WithChars("ACGT") because WithChars("ACGTN") is already defined.`
   Clear about what, silent about what to do. The strings page says "every constraint narrows" the
   alphabet, and a subset pool *is* a narrowing; only `WithoutAlpha()`/`WithoutNumeric()` subtract, so
   there is no way to remove `N` from a custom pool. (I first guessed a `WithoutChars("N")`; it does not
   exist — CS1061.) Workaround: a lambda ignoring its argument, `_ => Any.String().WithChars("ACGT")...`,
   which the CLI page documents as replacing the base.
8. The seed line printed by `[Reproducible]` on that failure was right there in the output. Good.

## What worked well

- `any` + entry points: `Any.Variant().WithReference(...).Generate()` read naturally from the first test.
- Refining lambdas: `Any.Accession().WithValue(v => v.DifferentFrom(other.Value))` drew a second, distinct
  accession for the "another accession" test with no hand-made literal.
- `Any.Enum<NormalizationConvention>()` made "this holds under either convention" visible as a dummy.
- No analyzer warnings, no surprises at build once the TODO lines were resolved.

## Fluency: 4 / 5

Scaffold-then-resolve was smooth and the documentation answered almost every question at the moment I had
it. Lost points: no idiom for a "must differ" rule between two parameters, a custom character pool that
cannot be narrowed, and a `NonEmpty()` default that looks inferred.
