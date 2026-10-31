# NOTES — regular developer, 35-dna-variants

## What I did
- Created `Tests.Regular`, restored, and scaffolded six types in one call:
  `any generate Accession NucleotideSequence Allele ReferenceSequence Variant NormalizedVariant --entry-point static:Arbitrary`.
- Kept five generators (deleted `AnyNormalizedVariant`: the normalizer builds those, tests never need one drawn).
- Refined the bases: `AnyAccession` -> `Any.StringMatching(@"^[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}$")` (AnyString -> AnyPattern,
  two lines retyped as the CLI page says); `AnyNucleotideSequence` -> `WithChars("ACGTN").WithLengthBetween(1, 200)`;
  `AnyAllele` -> `WithChars("ACGT").WithMaxLength(12)` (may be empty); `AnyVariant.Generate()` redraws the alternate
  (bounded, 20 tries) until it differs from the reference allele.
- `[assembly: Reproducible]`. 36 test methods / 42 cases (one 6-row HGVS theory): sequences & alleles, variant shape
  and overlap, normalisation (trim, left/right shift, dup vs ins, equivalence, mismatch refusals), HGVS, catalog.
  All green, re-run 5 times with fresh seeds.
- Domain fit: the reference *bases* are what normalisation tests are about, so they stay literals (`GGCACACATTT`);
  the accession, the reference name and, where the rule holds for both, the convention are dummies.

## Frictions and surprises, in order
1. `AnyAllele` was scaffolded as `Any.String().NonEmpty().WithMaxLength(10000)` and the recap tagged it `guard`.
   Only the max length comes from a guard; `NonEmpty()` is the tool's default, yet an empty allele is the most
   common value in this domain (indels). Silently narrower than the type; I only noticed because I know the domain.
2. The alphabet guards (`IndexOfAnyExcept("ACGT")`) and the accession regex are `to verify`, as documented. Fine,
   but every DNA-ish type costs a manual edit; reading `IndexOfAnyExcept("<literal>")` as `WithChars` would be nice.
3. `Variant` guards `reference.Equals(alternate)`: a rule *between* parameters. The CLI page's three recipes
   (start+duration, derive in Generate, check digit) do not cover "these two must differ". I wrote a bounded redraw
   in `Generate()` — which `--force` will erase. With `WithMaxLength(12)` two empty alleles collide ~1/169 draws, so
   leaving it was not an option.
4. Narrowing an alphabet in a refinement is refused. `WithSequence(s => s.WithBases(b => b.WithChars("ACGT")))` on a
   base of `WithChars("ACGTN")` throws at run time:
   `JustDummies.ConflictingAnyConstraintException : Cannot apply WithChars("ACGT") because WithChars("ACGTN") is already defined.`
   "ACGT" is a strict subset, i.e. a pure narrowing — exactly what a refinement is supposed to be. No `WithoutChars`
   exists (only `WithoutAlpha/WithoutNumeric`). Workaround: draw `Any.String().WithChars("ACGT")...` separately and
   pin it, which bypasses the base (the length rule is restated by hand). No analyzer warned (lambda, as documented).
5. Generators are emitted in `DnaVariants.Domain` (the target type's namespace) inside the test project. Documented,
   but it puts test-only types into the production namespace; I'd want `--namespace` defaulting to the project's.
6. Small: a reference sequence that must agree with a variant's accession meant threading `anyReference.Accession`
   everywhere (an `out` helper). Not the library's fault, just noting that pins dominate this domain.

## What worked really well
- One command for six types, an entry-point root built across all of them, and a recap that says exactly which
  parameter needs eyes and why (with file and line of the unread guard).
- The `TODO_verify_...` compile stop: impossible to forget a recipe.
- `WithChars` for a domain alphabet; `Any.StringMatching(...).DifferentFrom(...)` inside `WithValue(v => ...)` for
  "another accession" read naturally.
- `Any.Char().OneOf('A','C','G','T').DifferentFrom(referenceBase)` for "any other base": exactly the sentence.
- Error message for the conflicting pool names both sides; the failing-test seed line is copy-pasteable.

## Fluency: 4/5
Fast and readable once generators are refined; lost time on the cross-parameter rule and on the pool that cannot
be narrowed, both of which pushed me to code that either `--force` erases or that bypasses the base.
