# Expert notes — 37 Batch genealogy
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md` (index), `guides/getting-started`, `guides/core-concepts` (first half),
`guides/composition`, `packages/justdummies-cli` (what it produces, what the tool reads, adjusting a parameter, when a guard ties
two parameters), `packages/justdummies-xunit`, `generators/strings` (patterns), `numbers` (decimal), `dates-and-times` (DateOnly),
`collections`, `enums-and-choices`, and `packages/justdummies.en.md` (analyzer severities). Read the whole domain (820 lines) first.

Commands run:
* `dotnet restore` (the tool asked for it), then
  `any generate LotNumber MaterialCode Quantity LotQuantity Lot Transformation ProvenanceLink TraceEntry BatchGenealogy --output Generators --entry-point any`
  (9 types, 18 files, `Any.Lot()`-style entry points).
* `dotnet test --project Tests.Expert` (after adding `W/global.json`, see friction 1), 1 + 10 more runs through it, plus 15 runs of the
  test executable itself: 26 green runs, each with fresh seeds.
* `dotnet build` with `dotnet_diagnostic.JD001..JD038.severity = warning` in `Tests.Expert/.editorconfig`: 0 warnings in the end.

Generators completed or rewritten by hand (all in `Tests.Expert/Generators/`):
* `AnyLotNumber`, `AnyMaterialCode`, `AnyBatchGenealogy` (site code): `Any.String()` replaced by `Any.StringMatching(...)`, `AnyString`
  retyped `AnyPattern` in four places each. `AnyLotNumber.Recipes.cs` adds the named recipes `Base()` and `SubLot()`.
* `AnyLotQuantity`, `AnyLot`: quantity refined `.Positive()`.
* `AnyProvenanceLink`: `source != target` written in `Generate()` with `_target.DifferentFrom(source)`; transformation id retyped to a pattern.
* `AnyLot`: rewritten by hand (draw a manufacture date and a shelf life, see friction 4).
* `AnyTransformation`: rewritten by hand (kind, inputs and outputs are tied three ways; see friction 5). Default draw is a coherent
  transformation of any kind; `WithInputsAndOutputs(...)` pins both sides for the tests that need exact lots and kilos.
* `AnyQuantity`, `AnyTraceEntry`: untouched, straight from the tool.
* Test-only support: `Plant` (a site on a production day: registers released stock / produced lots, builds and records steps, releases outputs
  so they can feed the next one) and `Snapshot` (shows a refused operation changed nothing).

Tests written: 247 methods, 392 cases (6 skipped = 6 domain defects/inconsistencies, each verified red when un-skipped).
* `ValueObjects/LotNumberTests` (format theory, refusals, sub-lot construction, equality), `MaterialCodeTests`, `QuantityTests`
  (construction bounds, dimension, base amount, conversion, add/subtract/scale, ordering, equality + hash, ToString), `LotQuantityTests`,
  `ProvenanceLinkTests`, `TraceEntryTests`.
* `Entities/LotTests` (creation, shelf-life boundary 3653/3654, expiry, status machine, consumption boundaries),
  `TransformationTests` (arity per kind as a 13-row refused theory, id format, no lot twice / on both sides, dimension, split sub-lots,
  conservation across units, balance, yield, shares).
* `Aggregate/RegistryTests` (site code, Register/Get), `RecordTests` (happy paths per kind, links and shares, 17 refusals each proving the
  genealogy unchanged, atomicity with 3 inputs, re-recording once the blocker is lifted), `ShareOfTests` (chain, merge, two paths, sum to
  one, direction), `TraceTests` (upstream/downstream, depth, ordering, shortest depth with summed shares), `ContainTests` (threshold
  equality, hair below, 1, merged paths, already rejected/quarantined, idempotence, invalid thresholds, unknown lot).
* `Generators/GeneratorTests`: five checks that the hand-written generators still honour the domain.

Result: green, 381 passed / 6 skipped / 0 failed, and 26 repeated runs stayed green.

Dummy / literal policy: lot numbers, material codes, site code, ids, how old a stock lot is, and which units a mass may use are drawn; kilos,
shares, thresholds, boundary dates and every text whose format is under test are literals. A format test never feeds a drawn value to
its assertion.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not work out of the box.** First attempt: `MSBUILD : error MSB1001: Unknown switch ... --project`.
   Plain `dotnet test Tests.Expert` then gives `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer
   supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   The fix is a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` in an ancestor folder, which I added to W. Nothing in the
   JustDummies docs (nor the xunit page) says so; the PROJECT-README only shows `dotnet test --solution`. Expected the documented xUnit v3 setup to
   run with the command the trial prescribes.
2. **`any generate` before `dotnet restore`**: `✗ LotNumber: this project has not been restored, so its package references cannot be read. Restore it,
   then run the command again: dotnet restore`. The message is exact and actionable; no complaint, just a note that the getting-started page does not
   mention the step.
3. **A regex-guarded string costs a hand edit in five places, five times.** `LotNumber`, `MaterialCode`, the transformation id, the site code and
   the link's id all needed `Any.String()` replaced by `Any.StringMatching(...)` and then `AnyString` retyped `AnyPattern` on the recipe field, the
   private constructor, the `Func<>` of `WithX` (and its XML remark). The TODO comment says exactly that and the compiler (CS1503 / CS0029) finds
   anything missed, but it is mechanical work the tool could do once I tell it the pattern (idea: `--pattern value=...`, or reading the
   `[GeneratedRegex]` attribute the domain already carries).
4. **`Lot`: the scaffold's date pairing refuses ordinary pins, and does not hold the shelf-life limit.** `any` drew `manufacturedOn`/`expiresOn` through
   `Any.OrderedPair(..., strict: true)` (good: `manufacturedOn < expiresOn` holds, nothing to verify) but left `expiresOn.DayNumber - manufacturedOn.DayNumber
   <= 3653` as `to verify`. With the full `Any.DateOnly()` range the span would break the limit on virtually every draw. I bounded both dates to one ten-year
   window (2015-2024), which made every draw valid, then wrote a test pinning `WithManufacturedOn(new DateOnly(2026, 3, 10))` and got, on 8 of my tests:
   `JustDummies.ConflictingAnyConstraintException : Cannot apply WithFirst(03/10/2026) because no second value lies above 03/10/2026: Between(2015-01-01, 2024-12-31)
   already requires values less than or equal to 2024-12-31.` Two things hurt: the message speaks of `WithFirst` / "second value" (the `OrderedPair` internals) while
   I called `WithManufacturedOn`, and it formats the same kind of value two ways inside one sentence (`03/10/2026` and `2024-12-31`; the first is culture-dependent
   and ambiguous). The root cause is mine (a pin must lie in the other date's base window) but a base window is exactly what `to verify` pushes you to write.
   I then rewrote `AnyLot` to draw `manufacturedOn` + a shelf life in days (the "start and a duration" form of the CLI page), after which any date can be pinned.
5. **`Transformation`: the tool has nothing for a three-way tie, and a rewrite loses the per-parameter pins.** Recap: every one of `id`, `kind`, `inputs`,
   `outputs` is `to verify, unread guards`, with `first unread guard at Transformation.cs, line 31: EnsureArity(...);, and 5 more` (the five others are not
   named; I had to read the constructor). A scaffolded `Any.ListOf(new AnyLotQuantity())` for inputs and outputs can essentially never be a valid blend, split or
   process, so I rewrote the file: kind drives arity, a split derives sub-lots of one base lot, amounts are drawn on a scale of 3 and outputs are capped at
   `floor(total / n)`. The cost is that `WithInputs`/`WithOutputs` of the scaffold became a single `WithInputsAndOutputs`, and `any generate --force` would erase it.
   Expected for this kind of domain; worth saying that the "derive in Generate()" form works but is 100+ lines for a rich aggregate.
6. **Two overloads make `null` a pin that needs a cast**: `Any.ProvenanceLink().WithTransformationId(null)` is ambiguous between the pin and the lambda;
   `(string)null!` solves it (documented in the CLI page for reference types, and the error is a plain CS0121, so easy). Mild.
7. **The domain's type `BatchGenealogy` has the same name as its root namespace.** A test written in `namespace BatchGenealogy.Tests` cannot name the type:
   `error CS0118: 'BatchGenealogy' is a namespace but is used like a type`. The scaffolded files compile (they live in `BatchGenealogy.Domain`), and
   `Any.BatchGenealogy()` works; I put my tests in `BatchGenealogy.Domain.Tests` and used a `using Genealogy = BatchGenealogy.Domain.BatchGenealogy;` alias
   where needed. Not a library problem; it is a trap `any`'s output does not warn about.
8. **A decimal with no scale draws amounts with many decimals** (`Between(0m, Quantity.MaximumAmount)`, unit drawn). The base is faithful to the
   domain, and every arithmetic test uses literals, so it never mattered here; but a team asserting on converted draws would meet 20-digit fractions. The
   doc explains `WithScale`; the scaffold cannot know a business scale.
9. **Skipped tests**: all six verified red by temporarily un-skipping. `Skip = "domain defect: ..., seed ..."` has no seed to give when the
    inputs are literals; I wrote "no seed involved".

## What worked well
* `--entry-point any` gives `Any.Lot()`, `Any.Quantity()`, ... and the arrange blocks of the whole suite read uniformly next to `Any.Int32()`.
* The scaffolder read real guards: `ThrowIfGreaterThan(amount, MaximumAmount)` became `.Between(0m, Quantity.MaximumAmount)` (the constant, not its value),
  `depth < 1` and `share` bounds became `.Positive()` / `.LessThanOrEqualTo(1m)`, and `a > b` on dates became an `OrderedPair`. Failing to read a regex or a helper
  is reported honestly, by a line that does not compile, at the exact spot, with the guard quoted.
* Pinning the violating parameter and drawing everything else (`Any.TraceEntry().WithDepth(0).Generate()`) is the best way I have seen to write a
  constructor-refusal test: the test says which parameter is wrong, and nothing else can be.
* `DifferentFrom` on any generator (`Any.LotNumber().Base().DifferentFrom(other)`), `Any.StringMatching(...).Except(usedIds)`, `Any.ListOf(...).WithCount(n).Distinct()`
  made "n distinct lots" a one-liner and removed any chance of a birthday-collision flake in a suite full of lot numbers.
* `[assembly: Reproducible]` gives each theory case its own seed, stays silent when green, and prints a replay line naming the attribute when red.
* `Any.Enum<QuantityUnit>().OneOf(...)` through a lambda refinement kept the base rule while narrowing to mass or volume units.
* Every analyzer was silent on the final suite (all 38 rules at warning); JD030 was the only one that fired, on a scaffolded `Any.String().NotBlank()`, and its text
  told me exactly what to do.

## Fluency: 4 / 5
The core loop (scaffold, entry point, pin what the test is about, draw the rest) is fast and the resulting tests are the clearest I could have written, and the
reproducibility story is invisible until needed. What cost time was not the library but the shape of this domain: a rich aggregate with cross-parameter rules
(`Lot` shelf life, `Transformation` arity) turns the scaffold into a starting point for a hand-written generator, the regex-typed strings need a mechanical retype
five times, and the `dotnet test --project` prescribed here needs an undocumented `global.json`. One point off for those, and for the date-pair conflict message
that names the pair's internals and mixes two date formats.
