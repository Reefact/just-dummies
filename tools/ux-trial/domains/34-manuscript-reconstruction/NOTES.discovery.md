# Discovery notes — 34 manuscript reconstruction
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `docs/README.md` (the index), `guides/getting-started.en.md`,
`guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (all of it, twice: first the front, then the
"Adjusting a parameter", "When a guard ties two parameters" and options sections when I met `to verify`),
`packages/justdummies-xunit.en.md` (first half), `generators/strings.en.md` (the "Patterns" section only, to
write the siglum recipe), `guides/composition.en.md` (for `.As`, `OrderedPair`, `.Then`, `DifferentFrom`).
I then read `MODEL.md` and every file of `Domain/`.

`any` commands run, from `Tests.Discovery/`:
1. `any generate WitnessSiglum PassageReference Variant Lacuna CopyRelation AttestedReading Witness EditorialHypothesis Tradition --output Generators --entry-point any`
   (first attempt exit 2: "this project has not been restored"; after `dotnet restore` it wrote 9 generators + 9 `*.Entry.cs`).
   Recap: 3 generators complete (`PassageReference`, `Witness`, `Tradition`), 6 with `to verify` (`WitnessSiglum`, `Variant`, `Lacuna`, `CopyRelation`, `AttestedReading`, `EditorialHypothesis`).

Generators completed by hand (build had 11 `CS0103 TODO_verify_the_generator_for_*` errors, one per marked parameter):
- `AnyWitnessSiglum`: regex guard, replaced by `Any.StringMatching("[A-Z][A-Za-z0-9.]{0,11}")`, retyped `AnyString` -> `AnyPattern` on the field, factory, constructor and `Func` as the TODO comment said.
- `AnyVariant`: kind/text/hand tie (omission <=> no text, correction never by hand 1). `text` base made `NotBlank()`;
  `Generate()` rewritten to draw kind first, then text (empty for an omission), then hand (>1 for a correction).
  I also had to add `_kindPin` / `_handPin` to the copy constructor and to the four `With…` methods, otherwise a
  pinned `WithHand(3)` was ignored for a correction (see friction 6).
- `AnyLacuna`: `from <= to`; `Generate()` draws two passages and puts them in order.
- `AnyCopyRelation`: exemplar != copy; `Generate()` draws the copy, then `_exemplar.DifferentFrom(copy)`.
- `AnyAttestedReading`: witnesses base `.NonEmpty().Distinct()`.
- `AnyEditorialHypothesis`: witnesses base `.NonEmpty().Distinct()`; `Generate()` draws `isConjecture` first and
  cites no witness for a conjecture. A hand-written `AnyEditorialHypothesis.Custom.cs` adds `AsConjecture()` and `CitingWitnesses(params string[])`.
- `Witness` needed nothing (the tool paired `datedFrom`/`datedTo` with `Any.OrderedPair` by itself, see friction 5).

Support: `Support.cs` (`[assembly: Reproducible]`, a `Fixture` class with `At("1.2.3")`, `Siglum("A")`, `TraditionOf("A","B")`,
`Reads("A","1.1.1","text", hand)`, `Proposes(...)`), `GeneratorsTests.cs`.
I added a `global.json` at the workspace root to make `dotnet test --project` work (friction 1).

Tests written (176 `[Fact]`/`[Theory]` attributes in 14 files, 269 test cases): 
- `WitnessSiglumTests` (valid sigla, 9 malformed rows, null, equality/hash, case, trailing newline), 
- `PassageReferenceTests` (bounds, 7 out-of-range rows, ToString, Parse, 13 format-error rows, 4 range-error rows, leading zeros, ordering, equality), 
- `VariantTests` (normalisation, omission, kind/text pairs, first hand vs correction, hand bounds, 500/501 characters, unknown kind, nulls), 
- `LacunaTests` (ctor refusals, `Covers` on the closed range, `Overlaps` symmetric/boundary rows, different witnesses), 
- `CopyRelationTests` (self-copy, confidence in (0,1] incl. epsilon, NaN, infinities, count), 
- `AttestedReadingTests` (sorted ordinal witnesses, normalisation, omission, empty/duplicate/null witnesses), 
- `WitnessTests` (year range, span order, folios, repository, `IsCertainlyOlderThan` boundaries incl. touching spans), 
- `EditorialHypothesisTests` (conjecture rules, id, editor, rationale 2000/2001, normalisation, `Reject`), 
- `TraditionRecordingTests` (AddWitness, lacunae overlap/adjacency/readings inside, variants refusals, hands and corrections, `FinalReadingOf`), 
- `TraditionAlignmentTests` (grouping, largest group first, ordinal tie-break, omission, final reading only, lacuna), 
- `TraditionHypothesisTests` (Propose against final readings, competing hypotheses, Accept/Withdraw/Reject lifecycle, one accepted per passage, `CompetingReadingsAt`), 
- `TraditionStemmaTests` (DeclareCopy: unknown witness, older copy, touching dating, one exemplar, 2- and 4-cycle, branching, `AncestorsOf` nearest first), 
- `TraditionSharedErrorsTests` (agreement in error, order, accepted-only, withdrawn, omission, correction, a == b, unknown), 
- `GeneratorsTests` (500 draws of every generator never trip a constructor; variants reach all four kinds).

Result: `dotnet test --project Tests.Discovery` -> total 269, succeeded 268, skipped 1, failed 0. The one skip is the
domain defect below. Then 40 more runs stayed green (25 on the suite before `GeneratorsTests` was added, 267 cases, and 15 on the final suite, 269 cases; `--no-build`, fresh seeds each run).

Domain defect found (kept as a skipped test, `WitnessSiglumTests.A_siglum_cannot_end_with_a_line_break`):
`new WitnessSiglum("A\n")` and `new WitnessSiglum("Vat.3225\n")` are accepted. `[GeneratedRegex("^[A-Z][A-Za-z0-9.]*$")]`
matches before a final `\n` in .NET, so a siglum may end with a line break although MODEL.md says it matches
`^[A-Z][A-Za-z0-9.]*$` on 1-12 characters. Literal inputs, deterministic, no seed involved.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on this SDK.** Command:
   `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`. Plain
   `dotnet test` in the project folder then said: `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   I expected the getting-started to say how to run an xUnit v3 project. It does not mention running tests at all, nor `global.json`
   (only the project README's contributor section says the repo pins .NET 10 in `global.json`). I created `W/global.json` with
   `{"test":{"runner":"Microsoft.Testing.Platform"}}`, after which `--project` worked. Not a library bug, a docs gap for a newcomer on .NET 10 + xunit.v3.
2. **`any generate` before `dotnet restore`.** First run exit 2: `✗ WitnessSiglum: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is clear and told me exactly what to do; I only note that the docs' "Run it from your test project" never says the project must be restored first.
3. **The `Any.WitnessSiglum()` spelling needs `--entry-point any`, which the getting-started never mentions.** The getting-started shows
   `Any.OrderReference()` backed by a hand-written `AnyEntry` extension; I only learned the flag by reading the whole CLI page (and `any generate --help`).
   Without it I would have written `new AnyWitnessSiglum()` everywhere. Expected: a pointer from the getting-started to `--entry-point`.
4. **A `to verify` row is not a cheap thing to resolve when the rule ties several parameters.** `Variant` (kind/text/hand) and `EditorialHypothesis`
   (isConjecture/supportingWitnesses) got `to verify` and a `TODO(any)` that points at the right line and says "ties kind to hand" — very good.
   But the docs' four forms handle two-parameter ties (a start and a duration, `DifferentFrom`); three parameters with a conditional rule
   (empty text iff omission, hand > 1 iff correction) has no recipe, so I wrote a `Generate()` by hand. It took the longest of the whole job.
5. **The recap hides the `OrderedPair` the tool wrote.** For `Witness` the recap shows two independent rows:
   `datedFrom  int  Any.Int32().GreaterThanOrEqualTo(Witness.EarliestYear)  guard` and `datedTo  int  Any.Int32().LessThanOrEqualTo(Witness.LatestYear)  guard`.
   I assumed `datedFrom > datedTo` was unread (no `to verify`, no mention) and nearly added my own ordering; only opening `AnyWitness.cs` showed
   `Any.OrderedPair(_datedFromRecipe, _datedToRecipe)`. The CLI page says this happens, but the recap line does not. Expected a `paired with datedTo` hint.
6. **Deriving a parameter in `Generate()` silently disables its pin.** The CLI page warns about it
   ("`WithEnd(…)` sets a field your `Generate()` does not read"), and I met exactly that: my first `Variant.Generate()` ignored `WithHand(3)`
   for a correction (it redrew a hand above 1), compiled fine and would have produced wrong fixtures. Nothing (no analyzer, no comment in the
   generated file) flags a pinned field that `Generate()` no longer reads. I fixed it by adding pin fields to four generated `With…` methods and
   the copy constructor — an invasive edit of scaffolded code, which `--force` will wipe.
7. **`Lacuna.from <= to` on a `PassageReference` is recognised as a tie but not paired.** The recap says `to verify, AnyX, unread guards`
   and `ties from to to`, because `OrderedPair` exists on the library's own ordered builders only; `PassageReference` is mine and `IComparable`.
   I drew two passages and sorted them, which breaks the meaning of `WithFrom(...)` / `WithTo(...)` pins (a pinned end can swap sides).
   Tests that care build the lacuna with `new Lacuna(...)` and literals. Idea: `Any.OrderedPair` over any `IAny<T> where T : IComparable<T>`.
8. **JD030 on scaffolded strings, as documented.** With `dotnet_analyzer_diagnostic.severity = warning` in an `.editorconfig` the build lists 12 times
   `JD030: This string dummy declares no length: it draws 1 to 1025 characters` / `0 to 1024`. Nothing at default severity (Info), which is what the
   CLI page says. No other JD rule fired on my tests.
9. **A `.Distinct()` I had to add by hand.** The recap flags `witnesses` of `AttestedReading` as `to verify` with the exact
   guard (`witnesses.Distinct().Count() != witnesses.Count`); the base `Any.ListOf(...).NonEmpty()` would have drawn duplicates. Fixing it was one
   call (`.Distinct()`, found in the CLI page's `Combination` sample), good.
10. **The first `Fact` I wrote with `anyPassage` in the assertion was a property.** I wrote "parse the text of a drawn passage gives back the passage"
   and "a drawn passage lies within the bounds"; the core-concepts page's rule ("if an `anyXxxx` reaches your assertion, it is not a dummy")
   made me delete both and keep literals. The rule is easy to apply and well worded; noting it because it changed my tests.
11. **My own mistakes caught by the first green run** (not the library): a 13-character "twelve characters" siglum, and
   `Assert.Throws<ArgumentException>` for a null `cause` (the BCL throws `ArgumentNullException` from `ThrowIfNullOrWhiteSpace`). Both were
   found quickly thanks to the seed line printed in each failure.

## What worked well
- The `TODO_verify_the_generator_for_x` mechanism: 11 compile errors, each at the line, each with a comment naming the guard in the domain file. I never had to hunt.
- `Any.StringMatching("[A-Z][A-Za-z0-9.]{0,11}")` was a one-line replacement for the siglum regex, and the retype instructions in the comment (field, factory, constructor, `Func`) were exact.
- `.DifferentFrom(copy)` on an `IAny<WitnessSiglum>` made `exemplar != copy` a one-liner.
- The `Reads(...)`-style helpers on top of `Any.Variant().WithWitness(...).WithPassage(...)…` read like the scholar's vocabulary; literals stay for sigla, passages and texts (what the test is about), and the dummies are repository, folios, dates, work title, editor, rationale.
- `[assembly: Reproducible]` plus the seed line in the failure output (`Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = ...)]`) in the MTP "Standard output" block.
- Generated code is ordinary, readable and was easy to extend with a `partial` file (`AnyEditorialHypothesis.Custom.cs`).
- 40 repeated runs, 0 failures; the 500-draw generator test never tripped a constructor.

## Fluency: 4 / 5
Getting from zero to a first scaffolded file was fast, and the tool's recap plus the build errors told me what to do at every step; the docs
answered each question I had when I went looking. I lose a point for three things: the missing path from the getting-started to `--entry-point any`
and to running an xUnit v3 project on .NET 10 (`global.json`), the lack of any help for rules tying three parameters (I rewrote `Generate()` and
patched the pin plumbing by hand), and a recap that does not show the `OrderedPair` it had already written.
