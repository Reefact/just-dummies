# Regular notes — 35 DNA variants
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `MODEL.md`, the whole `Domain/` (11 files), `PROJECT-README.md` (skimmed), `guides/getting-started.en.md`,
  `packages/justdummies-cli.en.md` (what the last column means, guard table, "When a guard ties two parameters", "Adjusting a parameter"),
  `packages/justdummies-xunit.en.md`, `generators/strings.en.md` (alphabet, `WithChars`, patterns), `guides/composition.en.md` (`Fixed`, `DifferentFrom`).
- Project `Tests.Regular/DnaVariants.Tests.Regular.csproj` from the template, `dotnet restore`.
- `any` commands (run from `Tests.Regular`, one call):
  `any generate Accession NucleotideSequence Allele Variant ReferenceSequence --namespace DnaVariants.Tests.Regular.Generators`
  (first attempt failed with "this project has not been restored" until I ran `dotnet restore`).
- Generators completed by hand:
  - `AnyAccession`: regex guard marked `to verify` -> `Any.StringMatching(@"[A-Z]{2}_[0-9]{6,9}\.[0-9]{1,3}")`, retyped `AnyString` to `AnyPattern` in the 4 places the CLI page lists.
  - `AnyNucleotideSequence`: `Any.String().WithChars(NucleotideSequence.Alphabet).WithLengthBetween(1, 200)`.
  - `AnyAllele`: `Any.String().WithChars("ACGT").WithMaxLength(50)` (replaces the scaffolded `WithMaxLength(Allele.MaxLength)`, which the tool itself suggested shrinking).
  - `AnyVariant`: removed the two `TODO` pairs; `Generate()` draws the alternate with `_alternate.DifferentFrom(reference)` (the "second value that differs" form of the CLI page).
  - `AnyReferenceSequence`: kept as scaffolded (nothing to verify).
- Tests (8 test files, `[assembly: Reproducible]` in `GlobalUsings.cs`): 
  `AccessionTests`, `NucleotideSequenceTests`, `AlleleTests`, `ReferenceSequenceTests`, `VariantTests`, `NormalizedVariantTests`,
  `VariantNormalizerTests`, `VariantCatalogTests` (+ `Fixtures`). 113 test methods, 199 cases once theories are expanded:
  value-object guards and equality; `Slice`/`ReverseComplement`/`GcContent`; `Verify` (matching, wrong accession, past the end, wrong base, `N`);
  `Variant.Kind/End/LengthChange/Overlaps` (a 14-row overlap table); `ToHgvs` for every kind; normalisation (trim, left/right shift, dup vs ins,
  padded deletions/insertions/substitutions, equivalence both ways, refusals); catalog (`Record`, equivalence, overlap, full at 10 000, `Contains`, `Remove`, `ApplyAll`, whole-deletion refusal).
  Sequences and alleles that matter are literals (the model's `GGCACACATTT`); accession, reference name, unrelated sequences are drawn.
- Result: `dotnet test --project Tests.Regular`: 199 total, 196 passed, 3 skipped (3 domain defects, below), 0 failed; then 12 more runs (fresh seeds each), all green with the same counts.
  The 3 skipped tests were each un-skipped once to confirm they really fail.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` -> `MSBUILD : error MSB1001: Unknown switch ... --project`, and plain `dotnet test Tests.Regular` ->
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later`.
   The workspace has no `global.json` opting into the MTP runner; the docs only say `dotnet test --project ...` in the maintainer sections and the xunit page does not mention
   that a consumer on SDK 10 needs `{"test":{"runner":"Microsoft.Testing.Platform"}}`. I created `W/global.json` with it. Expected: the xunit package page to say it (the template makes the project `OutputType Exe`, i.e. MTP, without telling why or what the SDK needs).
2. `any generate Accession ...` before `dotnet restore`: `✗ Accession: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, fix is one command; I only note that the "Install"/first-run flow of the CLI page does not say the project must be restored first.
3. The `NucleotideSequence` guard is `upper.AsSpan().IndexOfAnyExcept(Alphabet) >= 0` with `Alphabet` a public const string, and `Allele` has the same shape with the literal `"ACGT"`. The CLI page lists `p.AsSpan().IndexOfAnyExcept("ACGT") >= 0` -> `.WithChars("ACGT")` as a read shape, but both came out as `to verify, unread guards` ("`bases: unread guard at NucleotideSequence.cs, line 18: if (invalid >= 0) ...`"), because the span comes from the local `upper = bases.ToUpperInvariant()` and not from the parameter. Expected `WithChars("ACGTN")` / `WithChars(NucleotideSequence.Alphabet)` (the page says a public constant is referenced by name). Cost: two hand edits; and the recap row reads `Any.String().NonEmpty()` for `NucleotideSequence`, which would draw 'x' and fail, so the TODO stop was right, but the message ("unread guard ... if (invalid >= 0)") does not tell that the culprit is the case-folding local: I had to read the constructor.
4. `Allele`: the scaffolded recipe is `Any.String().WithMaxLength(Allele.MaxLength)` and the tool adds a note that the 10 000 ceiling steers every default draw to that size. Good hint; I replaced it with a short pool.
5. `Accession` (regex guard): the doc's four retype places are exact and the compiler guided the rest, but the XML-doc remark on `WithValue(Func<AnyPattern, AnyPattern>)` still says `Func<AnyString, AnyString>` after the edit (cosmetic; the doc comment is not in the four places).
6. `Variant` (`reference.Equals(alternate)` tie): marked `to verify` for both alleles and the build stops at both, as documented. The "Draw a second value that differs" form worked first time: `_alternate.DifferentFrom(reference).Generate()` inside `Generate()`. Note this makes `WithAlternate(pin)` still work but `WithAlternate(pin equal to the drawn reference)` can fail with a bounded-redraw error; I did not need it.
7. The `Variant` recipe `position: Any.Int32().Positive()` (guard) draws up to `int.MaxValue` one draw in ten, which is right for the guard but means any test building a variant on a *real* reference has to pin position/alleles anyway. Nothing to fix; it is why almost every `Variant` in my tests is a literal.
8. Reading a `NucleotideSequence` generator that is also `N`-free (needed so that an allele can be read from it): `new AnyNucleotideSequence().WithBases(b => b.WithoutChars("N").WithMinLength(5))` worked as the strings page promises (`WithChars("ACGTN")` then `WithoutChars("N")`). The refining lambda is on the base of the sequence, so the `N`-free narrowing is not nameable once: I'd like a `WithoutUnknownBases()` helper but that is domain-specific, so I left it inline in one test.
9. `Fixtures.ReferenceReading(...)` builds `new AnyReferenceSequence().WithSequence(new NucleotideSequence(...))`: each call draws a fresh accession. My first version of the shift test called a helper twice, got two different accessions and `Assert.Equal` on the two normalised variants failed (`EY_28466391.486:g.7_8del` vs `YC_2015033.4:g.7_8del`): my bug, but the failure message prints only ToString of `NormalizedVariant`, i.e. the accession dominates the diff. No library issue.
10. xunit output when a test fails prints the seed line: `[JustDummies] These arbitrary values were seeded with 1520306576. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 1520306576)].` Exactly what the page promises; I used it as a sanity check only (all my first failures were wrong literals of mine, not drawn values).
11. Domain defects found by tests (kept, skipped with `Skip = "domain defect: ..."`):
    - `Accession` accepts `"NC_000001.11\n"`: the regex ends with `$`, which .NET matches before a trailing `\n`; the value is stored with the newline. Test `A_trailing_newline_is_not_part_of_an_accession`.
    - `VariantCatalog.ApplyAll` is order dependent when an insertion and another variant share the same `Position`: on `ACGT`, record `2:-/TT` then `2:C/G` gives `AGTCGT` instead of `ATTGGT` (the later-applied substitution overwrites the inserted bases); the reverse recording order gives the right `ATTGGT`. Test `An_insertion_recorded_first_next_to_a_substitution_at_the_same_position_applies_both` (its sibling with the other order is green).
    - `Variant.End` overflows at `Position = int.MaxValue` with a 2-base reference allele (`End = int.MinValue`). Low impact.
    - Observations not asserted: an insertion before the first base normalises to `g.0_1insA` (not a valid HGVS position); a tandem insertion is a `Duplication` when right aligned but an `Insertion` when left aligned, so `Kind`/HGVS of the same change depends on the convention.

## What worked well
- `any` plus the `to verify` stop: the three hand-written recipes (regex, alphabet pool, differing alleles) were each one documented form away; the build error at the exact line is a real work list.
- `Any.String().WithChars("ACGT")` example for DNA is literally in the strings page; `WithoutChars("N")` narrowing a shared base worked.
- `DifferentFrom` on generators (`new AnyAccession().DifferentFrom(anyAccession)`) made "another accession" one line, with the redraw bounded.
- `[assembly: Reproducible]` and the seed line on failure; 12 repeat runs stayed green, and the test project built with zero warnings (no analyzer noise).
- Splitting "what the test is about" (literal sequences, positions, alleles) from "what it needs" (accession, name, unrelated sequence) was natural with the `any` naming convention.

## Fluency: 4 / 5
Daily use is smooth: scaffold, read the recap, fix three TODOs from the CLI page's own recipes, write tests. Points lost for the SDK 10 / Microsoft.Testing.Platform test-runner set-up that no page tells a consumer about (frictions 1), and for two guards (`IndexOfAnyExcept` through a case-folding local) the scaffolder did not read although the page lists that shape, which cost hand edits and a read of the constructor.
