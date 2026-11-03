# Expert notes — 35 DNA variants
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md`, `guides/getting-started`, `guides/composition`
(`Combine`, `Fixed`, `DifferentFrom`, `Then`), `packages/justdummies-cli` (what the last column means, adjusting a
parameter, "when a guard ties two parameters"), `generators/strings` (alphabets, patterns), `packages/justdummies-xunit`,
the "Arbitrary does not mean dummy" section of `core-concepts`, and (late) the analyzer-severity section of `packages/justdummies`.
Then the whole `Domain/` and `MODEL.md`.

`any` commands (from `Tests.Expert`, after a `dotnet restore`; defaults kept in an `any.json`:
`{ "output": "./Generators", "namespace": "DnaVariants.Tests.Expert.Generators", "entryPoint": "any" }`):

    any generate Accession Allele NucleotideSequence ReferenceSequence Variant NormalizedVariant VariantNormalizer VariantCatalog

Eight generators scaffolded, `Any.Accession()` ... `Any.VariantCatalog()` entry points included. Completed by hand:
* `AnyAccession`: `Any.String()` replaced by `Any.StringMatching("[A-Z]{2}_[0-9]{6,9}\\.[0-9]{1,3}")`, `AnyString` retyped to
  `AnyPattern` in the four places the TODO named (sentinel lines deleted).
* `AnyAllele`: `Any.String().WithChars("ACGT").WithMaxLength(24)` (the scaffold had `WithMaxLength(Allele.MaxLength)`, whose
  10 000 ceiling would steer every default draw to huge alleles).
* `AnyNucleotideSequence`: `Any.String().WithChars("ACGTN").WithLengthBetween(1, 60)`.
* `AnyReferenceSequence`: name length bounded to 30 (JD030 once I raised the severity).
* `AnyVariant`: `Generate()` now draws the reference allele first and the alternate through
  `_alternate.DifferentFrom(reference)` (the `reference.Equals(alternate)` guard the tool could not read).
* `AnyNormalizedVariant`: the `kind` parameter is not an independent draw (a function of the variant), so I removed its
  recipe and made `_kind` an optional pin; `Generate()` uses `variant.Kind` unless pinned (about 40 lines of editing).
* `Generators/Scenario.cs` (hand-written, beside the scaffold): `ReferenceOf(bases)`, `VariantOn(...)`,
  `CalledReference()` (no N), `LongCalledReference(n)`, `SubstitutionOn(reference)` (a `Then`-based generator of a valid substitution).

Tests written (132 methods, 235 cases once theories expand), one class per type:
* `AccessionTests` (9 methods): prefix/version, 12 malformed forms, null, equality/hash, print; trailing-newline case (skipped, defect).
* `AlleleTests` (10): empty, upper-casing, N/other characters with offset, exactly 10 000 and 10 001 bases, null, equality, print.
* `NucleotideSequenceTests` (12): upper-casing, empty/null/offset of bad char, `Slice` (ok, empty, edges, refusals), `ReverseComplement`,
  `GcContent` (N in the denominator), equality.
* `ReferenceSequenceTests` (17): constructor guards, blank names, `BaseAt`, `Verify` (7 accepted shapes, accession/base/past-the-end
  mismatches, position `int.MaxValue`), identity by accession, print.
* `VariantTests` (22): guards, position 0/negative, no-change (also case-insensitive, both empty), `Kind` (9 shapes), `End`, `LengthChange`,
  `Overlaps` (13 rows, both directions), equality, print; `End` overflow (skipped, defect).
* `VariantNormalizerTests` (24): guards, trimming (suffix then prefix), left/right shifting through the CA/AC repeat and the poly-T tail,
  shift stops at both sequence edges, seven spellings of one deletion, equivalence / non-equivalence, idempotence, mismatches.
* `NormalizedVariantTests` (12): guards (kind vs shape, duplication only for insertions), equality, 8 HGVS shapes; `g.0_1ins` (skipped, defect).
* `VariantCatalogTests` (26): guards, record / re-record / equivalent record, canonical storage, `Contains`/`Remove` by equivalence,
  overlap refusal (state unchanged), neighbours allowed, mismatch refusal, ordering by position, the 10 000 cap (record the 10 001st;
  re-record when full), `ApplyAll` (deletion+substitution, insertion, around an insertion point, delete-everything refused); the
  insertion-then-substitution order case (skipped, defect).

Literals vs dummies: every position, allele and expected string is a literal wherever the assertion is about it; the reference
`GGCACACATTT` (a CA/AC repeat on 3-8, a poly-T tail on 9-11) is the same literal across the normalizer and catalog tests and each
expectation is derived by hand in the class summary, never from the code under test. Accessions, names, conventions (when the outcome
cannot depend on it: substitutions, non-shifting insertions, equivalence of already-equal spellings) and the reference of
catalog tests that only record a substitution are drawn. In `ToHgvs` tests the accession is a literal because it appears in the output.

Result: green with 231 passed, 4 skipped (4 domain defects, see below), 0 failed. Runs: 1 first green + 2 more while iterating +
12 consecutive fresh-seed runs with `--no-build`: all 15 green (about 13-16 s each, 10 000-variant catalog tests dominate).

Domain defects found (tests kept, `Skip = "domain defect: ..., seed ..."`):
1. `Accession` accepts `"NC_000001.11\n"`: the regex ends with `$`, which .NET also matches before a final `\n`.
2. `Variant.End` overflows silently for a position near `int.MaxValue` (`End` = -2147483648 for position 2147483647 and `"AC"`).
3. `NormalizedVariant.ToHgvs()` describes an insertion at position 1 as `g.0_1insA`.
4. `VariantCatalog.ApplyAll()` is order dependent: an insertion and the substitution/deletion of the base right after it share a
   position; applied in recording order, recording the insertion first gives `GGGTCACACATTT` instead of `GGTTGACACATTT`.
Domain observation (not skipped, documented by a passing test): `Classify` can never return `Duplication` under `LeftAligned`
(after the leftmost shift the preceding bases cannot equal the insertion), so the same change is `Insertion g.8_9insT` for VCF and
`Duplication g.11dup` for HGVS; `NormalizedVariant.Equals` ignores `Kind`, which hides it from `AreEquivalent`.

## Frictions and surprises, in order
1. `dotnet test --project Tests.Expert` (the command the brief gives) fails at once: `MSBUILD : error MSB1001: Unknown switch.
   ... Switch: --project`. The .NET 10 SDK only knows `--project` for the Microsoft.Testing.Platform runner, opted into with a
   `global.json` (`{ "test": { "runner": "Microsoft.Testing.Platform" } }`); nothing in the user docs mentions `dotnet test` or this
   switch (grep in `docs/` finds no `dotnet test`). Expected the documented flow to run as is. I added that `global.json` in the
   domain folder (inside W). The new `Test run summary` output after that is clear, and the failure prints the seed.
2. `any generate Accession ...` before restore: `✗ Accession: this project has not been restored, so its package references cannot be
   read. Restore it, then run the command again: dotnet restore`. Clear and actionable (no complaint), but the CLI page only says "run it
   from your test project".
3. The recap for `Allele`: `bases: the guard's ceiling of 10000 characters steers every default draw toward that size (ADR-0076); consider a
   smaller default in AnyValidBases(), such as .WithMaxLength(100)`. Useful advice, but it cites an ADR number a consumer cannot open (the
   docs I have do not ship the ADRs); the scaffold nonetheless wrote `WithMaxLength(Allele.MaxLength)`, a value the tool itself says is a bad default.
4. `Accession`, `Allele` and `NucleotideSequence` all come out `to verify`, and each build error is a one-line `TODO_verify_the_generator_for_...`.
   Expected for the regex (`Format().IsMatch(value)` through a `[GeneratedRegex]` method); the two alphabet guards are
   `upper.AsSpan().IndexOfAnyExcept("ACGT") >= 0` on a local derived from the parameter (after `ToUpperInvariant()`), so the documented
   `IndexOfAnyExcept("ACGT")` -> `.WithChars("ACGT")` row does not apply, and `NucleotideSequence` names a constant (`Alphabet`) rather than a
   literal. Right to stay silent, but the TODO text ("verify ... then delete these two lines") does not suggest `WithChars`; I only knew
   because I had read the strings page. For the regex the TODO is excellent: it quotes the pattern (as the C# string literal, backslash
   doubled, which I had to un-double) and lists the four retype sites.
5. `NormalizedVariant(variant, convention, kind)`: the tool marks `variant` and `kind` `to verify` ("ties kind to variant, and 1 more"). The
   documented answers (derive inside `Generate()`, `Then`, `DifferentFrom`) all work, but the scaffolded shape (a recipe field and a `With...`
   pair per parameter) fits a derived parameter badly: after deriving `kind` the pin/lambda pair becomes dead, which the docs do warn about.
   I deleted the recipe and kept a nullable pin. Wish: a documented recipe for "this parameter is a function of that one".
6. `Variant(reference, alternate)` throws when the alleles are equal. The tool reads `reference.Equals(alternate)` as a tie but offers nothing;
   `a > b` gets an automatic `OrderedPair`, `!=`-style guards get a `TODO`. `DifferentFrom` on the `IAny<Allele>` inside `Generate()` was a
   one-line fix and the docs' `Crossing` sample pointed straight at it. The new subtlety is the one the docs name: `WithAlternate(x)` pinned
   to the drawn reference would now fail on every run.
7. `Any.ReferenceSequence().WithSequence(s => s.WithBases(b => b.WithoutChars("N").WithLength(10001)))` throws
   `ConflictingAnyConstraintException : Cannot apply WithLength(10000) because WithLengthBetween(1, 60) already caps the length at 60.` Correct and
   documented ("a refinement never widens the base"), and the message names the base's constraint, but not the way out. For the two catalog-size
   tests I wrote `Scenario.LongCalledReference(n)`, which restates the alphabet `ACGT` and pins a `NucleotideSequence`: the domain rule "called bases"
   is now written twice (generator and helper).
8. First catalog-full test failed with `A catalog holds at most 10000 variants` when I expected `false`: my own mistake (the helper drew a new
   alternate base at each call, so "the same variant again" was a different variant). The lesson fits the library's philosophy: the variant that
   is re-recorded is the thing under test, so it must be a value held in a variable, not a second draw.
9. Raising analyzer severities: I first wrote `dotnet_analyzer_diagnostic.category-JustDummies.severity = warning` (silently ignored, no warning) because I
   had not yet read `packages/justdummies`; the right keys are the per-id ones or `category-JustDummies.Constraints`. With JD001-JD038 at warning only
   `JD030` fired (name of `ReferenceSequence`, 3 call sites); fixed with `.WithMaxLength(30)`.
10. Performance of the domain, felt through the tests: filling a 10 000-variant catalog costs about 6 s per test (each `Record` scans the list for
    `Contains` and overlaps), so the two cap tests are 80% of the run time.

## What worked well
* The scaffold's default recap is honest: every guard the tool cannot read is a compile error at the exact line, with the guard quoted;
  nothing plausible-but-wrong was drawn behind my back. `ReferenceSequence`, `VariantNormalizer`, `VariantCatalog` came out complete.
* `Any.Accession()` (C# 14 entry point) makes Arrange blocks read uniformly with `Any.Int32()`; `new AnyAccession().WithValue(v => v.DifferentFrom(...))`
  was the natural way to say "another accession".
* `.Then(...)` + `Any.OneOf(char[])` made the hand-written `SubstitutionOn(reference)` a five-line generator that cannot produce a variant the
  reference rejects.
* `[assembly: Reproducible]` plus the seed printed in the failing test's standard output: every one of the failures above (the four defects, my wrong
  `GG`-insertion test, my wrong catalog test) came with a seed, and the skipped defects carry theirs.
* The conflict exceptions and constraint names read well; `DifferentFrom` on `IAny<T>` and on builders was exactly where the docs said.
* Writing tests with draws forced the question "is this a dummy?" every time: the `Two_different_insertions_at_the_same_point_overlap` test failed
  because a drawn convention shifted `GG` through the G-rich start; the fix was to pick alleles that cannot slide, not to pin the convention.

## Fluency: 4 / 5
The path from docs to a green, repeatedly green suite was short: eight generators in one command, four of them needing a line or two, and every
failure was either my own mistake or a real domain defect with a replayable seed. Points lost for the unmentioned `dotnet test --project`
requirement (the very first command failed), for two scaffolds (`NormalizedVariant`, `Variant`) whose ties between parameters required manual
surgery in a file shaped for independent parameters, and for a recap that cites an ADR the consumer cannot read and writes a ceiling it just
called a bad default.
