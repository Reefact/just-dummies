# Discovery notes — 37 batch genealogy
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`; `guides/getting-started.en.md`; `packages/justdummies-cli.en.md`
(the whole "what the last column means" part, "Through a graph of aggregates", "Adjusting a parameter",
"When a guard ties two parameters"); `generators/strings.en.md` (Patterns only), `generators/dates-and-times.en.md`
(DateOnly part), `generators/numbers.en.md` (bounds, floating point), `generators/enums-and-choices.en.md`,
`generators/collections.en.md` (count vocabulary), `guides/composition.en.md` (As, Combine, OrderedPair, Fixed,
DifferentFrom), `packages/justdummies-xunit.en.md`, `packages/justdummies.en.md` (analyzer severities) and
`analyzers/JD036.en.md` when an analyzer fired. I read the domain (`Domain/*.cs`, `MODEL.md`) before scaffolding.

`any` commands run (all with the full path of the tool, from `Tests.Discovery`):
- `any generate LotNumber --output Generators` (first attempt: refused, project not restored; after `dotnet restore` it worked)
- `any generate LotNumber MaterialCode Quantity LotQuantity Lot Transformation ProvenanceLink TraceEntry BatchGenealogy --output Generators --entry-point any`
  (9 generators + 9 `*.Entry.cs`, so the tests read `Any.Lot()`, `Any.Quantity()`...)

Generators completed by hand (the build stopped on each `TODO_verify_the_generator_for_...` line, as announced):
- `AnyLotNumber`, `AnyMaterialCode`, `AnyTransformation.id`, `AnyBatchGenealogy.siteCode`: regex guards ->
  `Any.StringMatching(...)`, retyping `AnyString` to `AnyPattern` on the four lines the TODO names.
- `AnyLotQuantity`, `AnyLot.initialQuantity`: `quantity.IsZero` guard -> `new AnyQuantity().WithAmount(a => a.Positive())`.
- `AnyLot` dates: the ordered pair was inferred, the 3653-day shelf-life cap was not; I gave both dates one window
  (2020-01-01..2029-01-01, 3288 days) so any strictly ordered pair respects the cap by construction.
- `AnyProvenanceLink`: `source.Equals(target)` guard -> `_target.DifferentFrom(source)` in `Generate()`.
- `AnyTransformation`: the default is a valid Process (1 input in grams 1000-5000, 1 output in grams 1-999, id `TRF-[0-9]{6}`).
- Hand-written beside them: `AnyLotNumber.Custom.cs` (`SubLot()`), `AnyTransformationId.cs` (`Any.TransformationId()`),
  `Support/Scenario.cs` (builders that register released/quarantined/produced lots around a drawn day).

Tests written (189 methods, 275 cases with theory rows; `[assembly: Reproducible]` on the whole assembly):
- `LotNumberTests`, `MaterialCodeTests`: formats (accepted/refused rows), sub-lot index 01-99, `SubLot(i)`, equality/hash.
- `QuantityTests`: bounds (0, 1e9, 1e9+0.01, negative, undeclared unit), mass vs volume, base amount, ConvertTo, Add/Subtract
  (unit of the left operand, never negative, > max), Scale, equality across units, CompareTo, ToString (culture-invariant).
- `LotQuantityTests`, `ProvenanceLinkTests`, `TraceEntryTests`: guards (zero quantity, self provenance, share (0,1], depth >= 1, nulls).
- `LotTests`: construction, expiry/shelf-life boundaries (3653 ok, 3654 refused), the status machine, every `Consume` refusal and boundary.
- `TransformationTests`: arity per kind (11 rows), id format, distinct lots, same dimension, split sub-lots, balance in base units,
  TotalInput/TotalOutput/Loss/Yield/ShareOf, defensive copy.
- `BatchGenealogyRecordTests`: site code, Register/Get, blend/split/process success, 15 refusals each asserting "nothing changed" (snapshot).
- `BatchGenealogyTraceTests`: ShareOf (direct, chained, unrelated, two paths), TraceUpstream/Downstream (share, depth, exposed quantity, order), Contain.

Result: green with `dotnet test --project Tests.Discovery`: 270 passed, 0 failed, 5 skipped (5 documented domain defects). I ran the
suite 12 times, then 11 more times after the last change, always green with fresh seeds (23 green runs). I also un-skipped the 5 tests
temporarily to confirm each defect is real (all 5 fail as described), then restored the `Skip`.

## Frictions and surprises, in order
1. **Running the tests.** `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... --project`. Plain
   `dotnet test Tests.Discovery` -> `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK
   and later ... opt-in to the new dotnet test experience`. Neither the README nor getting-started say anything about running tests; they
   only show `dotnet add package`. I had to add `{"test":{"runner":"Microsoft.Testing.Platform"}}` as `global.json` at W. Not a library bug,
   but a newcomer on .NET 10 + xunit.v3 hits it before seeing a single dummy; one line in the xUnit adapter page would save ten minutes.
2. **`any` before restore.** `any generate LotNumber` -> `x LotNumber: this project has not been restored ... Restore it, then run the command
   again: dotnet restore`. The message was clear and actionable (good), but the docs never say the project must be restored first.
3. **README/getting-started show `any generate Order` with no flag**, while the CLI page says `--output` defaults to the current directory and
   `--project` to the only `.csproj` here. I ran it from `Tests.Discovery` with `--output Generators`; fine. I only found `--entry-point any`
   (which gives `Any.Lot()`) on the CLI page, deep in "Reaching it as Any.Order()". The getting-started page already uses `Any.OrderReference()`
   without telling how it comes to exist, so a newcomer cannot connect the two.
4. **Four regex-guarded value objects = four hand retypings.** For `LotNumber` the tool wrote `Any.String()` + a `TODO` that quotes the pattern
   `^TRF-[0-9]{6}$` and says "retype AnyString to AnyPattern here, on the recipe field, in the recipe constructor and in the Func of WithId".
   I followed it literally (a global replace of `AnyString` by `AnyPattern` in the file, then `Any.StringMatching(...)`), it compiled first try.
   That is accurate, but it is a mechanical edit the tool already knows how to describe; it could offer it (even `--regex-as-pattern`) when the
   pattern is inside the supported subset. Also, I had to translate `^...$` myself: the recipe `Any.StringMatching("[A-Z]{2,4}-[0-9]{6}")`
   says nothing about the optional `.NN` suffix of `LotNumber`'s regex; the tool cannot know and I chose base lots only.
5. **A custom variant desynchronises the recipe field.** I added `AnyLotNumber.SubLot()` in a partial file by setting `copy._value =
   Any.StringMatching("...\\.(0[1-9]|[1-9][0-9])")`, as the CLI page shows for `_declaredValue`. Later `Any.LotNumber().SubLot().WithValue(v => v.OneOf("ABC-123456.01"))`
   threw `ConflictingAnyConstraintException: Cannot apply OneOf("ABC-123456.01") because StringMatching("[A-Z]{2,4}-[0-9]{6}") allows none of its values.`
   Reason: the lambda starts from `_valueRecipe` (the base), not from my replacement. The message is precise, but it names the base pattern
   without saying "you replaced `_value` in a custom method, `WithValue(Func)` still refines the base recipe". The docs say the overload
   "replaces the base" and "the recipe field is left as it is, so a lambda written after your overload starts from it" - it is documented, but
   I read it as a feature and only understood the consequence when it threw. I dropped the `OneOf` and built the sub-lot with `source.SubLot(i)`.
6. **The `TODO` comment on dates.** For `Lot` the tool correctly paired `manufacturedOn`/`expiresOn` (`Any.OrderedPair(..., strict: true)`) and marked
   the pair `to verify` because of the 3653-day cap, which it cannot express. No error, just a sentinel on each of the two parameters. My way out
   (one 9-year window for both) is a trick, and it has a cost I only found while writing tests: pinning `WithManufacturedOn` or `WithExpiresOn` outside
   the window is refused by the pair, so every test's dates have to live in 2020-2029. A max-gap constraint on `OrderedPair` would be the real fix (idea).
7. **Aggregates with cross-parameter rules are where the scaffold stops helping.** `Transformation` got `Any.ListOf(new AnyLotQuantity())` for inputs
   and outputs with `TODO` x4 ("and 5 more"): arity per kind, same dimension, split sub-lots, output <= input. The docs warn about this (collection element
   refinement is not reachable through the list lambda), so I wrote a fixed Process default (grams, 1 -> 1) and built every other shape through
   `Scenario.Blend/Split/Process` helpers in the tests, with literal quantities (they are what the assertions are about). It worked, but I wrote more
   helper code than generator code. No complaint, just the honest cost for this kind of domain.
8. **A failure on a drawn value that was my fault (not flaky).** `BatchGenealogyTraceTests.Tracing_downstream...` failed once with
   `Lot GOJ-201891 expired on 2026-08-21` (seed -210875260). My `Scenario` drew input lots with `expiresOn.After(day)`, but the test used lot B again two steps
   later. The expiry date was not a dummy for that scenario; I constrained it (`After(day.AddDays(200))`) in the scenario builder. The seed was printed in
   the standard output with the exact `[JustDummies.Xunit.Reproducible(Seed = ...)]` snippet, which is how I knew immediately it was a draw, not noise.
9. **`Any.Decimal().Negative()` and `GreaterThan(Quantity.MaximumAmount)` fire JD036 (Info)** once I raised analyzers to warning
   (`dotnet_analyzer_diagnostic.severity = warning` in a temporary `.editorconfig`): "This floating-point dummy declares no range: one draw in ten spans the
   whole type". Correct and useful (the test is "any negative amount is refused", and the draw really reaches decimal.MinValue), but the word
   "floating-point" for `Any.Decimal()` reads odd; the JD036 page does say decimal explicitly. Also the scaffolded `Any.String().NotBlank()` of
   `AnyProvenanceLink.transformationId` fires JD030 (1 to 1025 characters), expected per the CLI page. Nothing fired at default severity, so a newcomer
   sees zero analyzer output; I only saw them because the docs explained how to raise them.
10. **Name collision (domain, not library).** The domain's namespace `BatchGenealogy` and its aggregate class `BatchGenealogy.Domain.BatchGenealogy` make
   `BatchGenealogy` resolve to the namespace in my test namespace `BatchGenealogy.Tests.Discovery`; I used `using Genealogy = BatchGenealogy.Domain.BatchGenealogy;`.
   The scaffold itself handled it (`AnyBatchGenealogy`, `Any.BatchGenealogy()`).
11. **Domain defects found by the tests** (each kept as a skipped test with the reason):
   - `LotNumber`, `MaterialCode` and `Transformation.Id` accept a trailing line break: `new LotNumber("API-123456\n")`, `new MaterialCode("MAT-12345\n")`,
     `new Transformation("TRF-123456\n", ...)` do not throw (regex ends with `$`, which matches before a final `\n`; `\z` or `RegexOptions.None`+`\z` is needed). Literal inputs, no seed.
   - Shares are decimals, so a lot rebuilt from three equal sub-lots of an ancestor has `ShareOf(ancestor, lot) == 0.9999999999999999999999999999`, not 1; consequently
     `Contain(ancestor, 1m)` does not quarantine a lot made entirely from the rejected material (`share >= minimumShare` is false). Literal quantities (3 x 300 g), no seed.

## What worked well
- The `TODO_verify_...` sentinel is a very good idea: every guard the tool could not read was a compile error at the exact line, with the guard quoted. I never had
  to guess which parameters needed my attention, and the 4 regex ones were fixed in minutes following the sentence the tool wrote.
- The recap table (`AnyX`, `guard`, `to verify, unread guards`) made the scaffold understandable at a glance; `AnyX` composition (`new AnyLotNumber()`
  inside `AnyLot`) just worked, with the generation order I wrote the types in (all in one `any generate` call).
- `[assembly: Reproducible]` plus the printed seed + replay snippet made the one failure on a drawn value trivial to classify.
- `Any.StringMatching`, `OneOf` on enums, `DifferentFrom` on a typed builder and on an `IAny<T>`, and the `With...(Func)` refinements read like the test intent
  (`Any.Lot().WithManufacturedOn(m => m.Before(day)).WithExpiresOn(e => e.After(day.AddDays(200)))`).
- Conflict messages name both sides (`Cannot apply OneOf(...) because StringMatching(...) allows none of its values`).
- Draws that reach limits (huge negative decimals, `int.MinValue` depths) never destabilised a test, and made me think about which values were really dummies.

## Fluency: 4 / 5
The path from README to a green suite was smooth once the environment ran tests: the scaffold, its sentinels and the generator API taught me what I needed, and I spent
most of my time on the domain, not on the library. Points lost for (1) nothing in the docs about running tests on .NET 10 / MTP, (2) the manual, mechanical regex retyping
and the subtle recipe/`_value` split when adding a custom variant (friction 5), and (3) the lack of a way to bound the distance between two ordered dates, which forced a
window trick (friction 6).
