# Expert crafter notes — 35 DNA variants

## What I did

- `Tests.Expert`: 35 test methods (46 cases with theory rows), all green (`dotnet test --project Tests.Expert`).
- Scaffolded `Accession NucleotideSequence Allele ReferenceSequence Variant` in one call with
  `--output ./Arbitrary --entry-point any`, so arranges read `Any.Variant()…` next to `Any.Enum<…>()`.
- Resolved the four `to verify` rows by hand:
  - `AnyAccession`: `Any.StringMatching(@"[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}")`, retyping the factory and the
    refining `Func<…>` to `AnyPattern` exactly as the CLI page describes.
  - `AnyNucleotideSequence`: `WithChars("ACGTNacgtn").NonEmpty()` (lower case drawn on purpose: stored upper).
  - `AnyAllele`: `WithChars("ACGTacgt").WithMaxLength(Allele.MaxLength)` — empty allowed, no N.
  - `AnyVariant`: the `reference != alternate` tie lives in `Generate()` as a bounded redraw (16 attempts).
- Hand-written `AnyNucleotideSequence.Custom.cs` (`OfCalledBases(...)`) and `Recipes/VariantRecipes.cs`
  (`SubstitutionAt(reference, position)` via `Any.Char().OneOf("ACGT").DifferentFrom(base).As(...)`,
  `CalledReferenceOfLength(n)`), plus `[assembly: Reproducible]`.
- Literals where the assertion lives (the `GGCACACATTT` repeat, positions, alleles, HGVS strings); drawn:
  accession, reference name, the convention wherever both conventions must agree, positions and bases
  of substitutions whose placement does not matter, the inserted bases of an overlap check.

## Frictions and surprises, in the order met

1. `any generate Allele` proposed `Any.String().NonEmpty().WithMaxLength(10000)`. The `WithMaxLength` came
   from the guard (good), but `NonEmpty()` is the string default, while `Allele` explicitly accepts empty
   (an insertion's reference side). It was marked `to verify` because of the alphabet guard, so I caught
   it; had the alphabet guard been absent, the empty allele would have vanished from every draw silently.
2. `Variant` guards `reference.Equals(alternate)`: the tool flagged both parameters `to verify` — correct.
   But resolving it had no library primitive: a scaffolded `AnyAllele` carries no `DifferentFrom`, and
   there is no "draw two values from these generators that differ" composer. I wrote a bounded redraw in
   `Generate()` — exactly the mechanism the library prides itself on owning, now hand-rolled.
3. Narrowing the sequence alphabet from a test, the natural refinement
   `WithSequence(s => s.WithBases(b => b.WithChars("ACGTacgt")))` threw:
   `ConflictingAnyConstraintException : Cannot apply WithChars("ACGTacgt") because WithChars("ACGTNacgtn") is already defined.`
   The message is crisp, but the CLI page says a refinement "narrows a leaf" and lists the declare-once
   constraints as `OneOf, WithLength, MultipleOf, WithScale` — `WithChars` is not listed. A subset pool
   is a pure narrowing; there is no `WithoutChars("Nn")` either (only `WithoutAlpha/WithoutNumeric`).
   Workaround: a partial-class method that replaces the base pool by a subset (documented pattern, worked).
4. Two distinct accessions: `Any.SetOf(Any.Accession()).WithCount(2).Generate().ToArray()` — works
   because `Accession` has value equality, but a `HashSet` to index into reads indirect.
5. A different accession from a given one: `WithAccession(a => a.WithValue(v => v.DifferentFrom(x.Value)))`
   — two lambdas deep to say "any other accession". Works, but a `DifferentFrom(Accession)` on the
   scaffolded generator would read as the domain speaks.
6. JD030 (Info) on the sequence and the reference name: right call, the domain declares no length.

## What worked well

- The recap's `unread guard at Accession.cs, line 11: if (!Format().IsMatch(value))` and the build-stopping
  `TODO_verify_…` line: nothing could ship unread. `NotBlank()` inferred from `IsNullOrWhiteSpace`.
- Composition through owned generators (`new AnyAccession()` inside `AnyVariant`), C# 14 entry points,
  pin vs refine overloads: `Any.ReferenceSequence().WithSequence(new NucleotideSequence("GGCACACATTT"))`
  is the ideal shape for "the bases matter, the accession and name do not".
- `Any.Char().OneOf("ACGT").DifferentFrom(base).As(...)`: an anchored substitution recipe in one expression.
- `Any.Enum<NormalizationConvention>()` as a dummy where both conventions must agree: a real test of
  indifference (would expose a convention leaking into substitution/delins trimming).
- `[assembly: Reproducible]` + theories: one seed per case, printed on failure, nothing to wrap.

## Domain observations (not library findings)

- Left-aligned, an insertion that duplicates the following bases is classified `Insertion`
  (`g.2_3insCA`, as MODEL.md states): `Classify` only looks at the bases before the insertion point.
- `VariantCatalog` is O(n²) on record; the 10 000-variant cap test takes a few seconds.

## Fluency: 4 / 5

Scaffold → resolve TODOs → compose → test was smooth and the docs answered every question I had.
One point off for the two hand-rolled workarounds a domain with alphabets and "two alleles differ"
needs immediately: narrowing a `WithChars` pool, and a cross-generator distinctness tie.
