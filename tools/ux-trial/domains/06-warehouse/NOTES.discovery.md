# Discovery notes — 06 warehouse
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `PROJECT-README.md`, `README.md` (index), `guides/getting-started.en.md`, `guides/core-concepts.en.md`, `packages/justdummies-cli.en.md` (the whole page, it is long),
`packages/justdummies-xunit.en.md`, `guides/composition.en.md`, `generators/README.md`, then on demand `generators/dates-and-times.en.md`, `generators/strings.en.md` (the `Char` part),
`generators/collections.en.md` (head), `generators/numbers.en.md` (a grep for `Positive`), `analyzers/README.md` and `analyzers/JD036.en.md`.
I did not read reproducibility or errors-and-conflicts pages: the getting-started page and the xunit page were enough to start.

Commands (the tool is called by its full path, written `any` below):
- `any --help`, `any generate --help`
- `any generate Sscc Ean13 LotNumber Quantity TemperatureRange BinLocation ReceiptLine PalletReceipt --entry-point any`
  (first run refused: project not restored; after `dotnet restore` it wrote 8 generators + 8 `.Entry.cs`).
  Recap: Sscc, Ean13 `to verify`; BinLocation aisle and level `to verify`; PalletReceipt supplierCode and receivedAt `to verify`; the rest inferred.
- `dotnet build`, `dotnet test --project Tests.Discovery`, and the test exe directly for the repeated runs.

Generators completed by hand:
- `AnySscc`, `AnyEan13`: `Any.StringMatching("[0-9]{17}"/"[0-9]{12}").As(body => body + Gs1.ComputeCheckDigit(body))`, retyped `AnyString` -> `IAny<string>` (factory, recipe field, recipe constructor, `Func<…>` of the refining overload) as the CLI page explains.
- `AnyBinLocation`: aisle `Any.Char().Alpha().InUpperCase()`, level `Any.Int32().Between(1, 20)`.
- `AnyPalletReceipt`: supplier code `Any.StringMatching("SUP-[0-9]{5}")` (retyped `AnyString` -> `AnyPattern` everywhere); removed the TODO of `receivedAt` after finding on the dates page that `Any.DateTime()` is always `Kind.Utc`.
- `AnyReceiptLine`: unit weight `Any.Decimal().Between(0.001m, 100m).WithScale(3)` (see friction 6).
- `AnyTemperatureRange`: both ends `Between(-40m, 40m)` (see friction 7).

Tests written (97 methods, 188 cases; 181 run, 7 skipped on purpose, all in `Tests.Discovery/`, `[assembly: Reproducible]` in `AssemblyInfo.cs`):
`Gs1Tests` (check digits against published EAN/SSCC examples, empty string, non-digits), `SsccTests`, `Ean13Tests` (shape, check digit, Arabic-Indic digits, prefix, equality),
`LotNumberTests` (20/21 chars, blank, stored as given), `QuantityTests` (0, 10 000, -1, 10 001, equality), `TemperatureRangeTests` (ends, ordering, zone boundaries -18/-17.99/8/8.01),
`BinLocationTests` (ToString padding, every range edge, reach truck at 4/5, Parse ok/format errors/out-of-range, equality), `ReceiptLineTests` (guards with parameter names, discrepancy, weight),
`PalletReceiptTests` (construction guards, copied lines, totals, the three inspection reasons with their boundaries and order, quarantine, put-away, state machine).
Final result: green (181 passed, 0 failed, 7 skipped). After the one failure described in friction 8 was fixed: 25 + 3 consecutive green runs (the 40 earlier runs had 13 red ones, all from my own test bug).

Skipped on purpose, each verified red when un-skipped, each with the reason in `Skip = "domain defect: …"` (these tests draw nothing, no seed is involved):
1. `TemperatureRange`: a maximum above 40 is reported on `minCelsius`.
2. `BinLocation.Parse("A-12-03\n")` is accepted (`$` matches before a final line feed).
3. `PalletReceipt`: supplier code `SUP-12345\n` accepted.
4. `PalletReceipt`: supplier code with Arabic-Indic digits accepted (`\d` is Unicode).
5. `PalletReceipt`: a null list of lines throws `NullReferenceException`.
6. `PalletReceipt.Lines` returns the internal `List<T>`; casting it to `IList<ReceiptLine>` lets a caller add a line.
7. `ReceiptLine`: an enormous unit weight is accepted, then `WeightKg` (hence `NetWeightKg`, `InspectionReasons`) throws `OverflowException`.

## Frictions and surprises, in order
1. **`dotnet test --project …` does not work out of the box.** The brief (and what I assumed was the usual way) is `dotnet test --project Tests.Discovery`. On SDK 10.0.114 it answers
   `MSBUILD : error MSB1001: Unknown switch. … Switch: --project`. Without `--project`, `dotnet test Tests.Discovery` answers
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.`
   What fixed it: a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }` next to the projects. No page of the documentation says a word about running the suite; the xunit page lists the xunit.v3 version floor but not this SDK-10 requirement. I lost a few minutes. (Not a library bug; a docs gap for a newcomer on .NET 10.)
2. **`any generate` on a fresh test project is refused until `dotnet restore`**: `✗ Sscc: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`.
   The message is clear and gives the fix; I only expected the tool to restore for me (or the docs' "Install" to mention it). I ran `dotnet restore` and re-ran.
3. **Check digits are where the scaffolder stops, and the retype is by hand.** `Sscc` and `Ean13` come out `Any.String()  to verify, unread guards` with `Validate(code);` / `Format.IsMatch(value)` named as the unread guards.
   I knew from the CLI page ("Draw the body, then derive the check digit", `Any.StringMatching("[0-9]{15}").As(body => body + LuhnDigit(body))`) exactly what to write, which was excellent.
   But then the file does not compile (`CS0029`) until `AnyString` is retyped `IAny<string>` in four places (field, private constructor, the `Func<…>` of `WithCode`/`WithValue`). The page lists them and it worked on the first try, yet doing it twice by hand for two near-identical types felt mechanical.
   Wish: a dedicated hint for "pattern + derived suffix", or generate the refining overload as `Func<IAny<string>, IAny<string>>` when the TODO text mentions `.As`. (`idea`, low.)
4. **`WithGtin(null!)` is ambiguous (CS0121)** in a test that checks the null guard: `The call is ambiguous between 'AnyReceiptLine.WithGtin(Ean13)' and 'AnyReceiptLine.WithGtin(Func<AnyEan13, AnyEan13>)'`.
   The CLI page does explain the cast for a nullable parameter (`.WithAgent((Customer?)null)`) but I read that section as being about nullable parameters only; for guard tests on non-nullable ones I had to guess `(Ean13)null!`. A sentence in "Adjusting a parameter" would have saved the compile cycle.
5. **`Info` analyzers are invisible in `dotnet build`**, as the docs warn (README of the analyzers, JD036 page). I followed the documented `.editorconfig` snippet and it worked, which is a good sign for the docs. My first attempt at a category name was wrong and printed nothing at all (no "unknown category" warning), so for a minute I believed the code was clean. The correct form is on the page (`category-JustDummies.Constraints`).
6. **A scaffolded `decimal` with a one-sided guard overflows one draw in ten.** `unitWeightKg` was scaffolded `Any.Decimal().Positive()  guard`. I did not notice at first; then I wrote a throw-away loop (2 000 draws of `Any.ReceiptLine()`, calling `WeightKg`): `overflow=205 ok=1795 max=9769842989.05…`.
   The domain's `WeightKg` is `Received.Units * UnitWeightKg` and `decimal` overflows. This is documented behaviour of the library (JD036, ADR-0117) and it is the draw doing its job: the unit weight is open-ended in the domain, which is a domain defect (finding 7 above). What I did: bound the dummy to a realistic range in the generator (0.001–100 kg), and keep a skipped test for the domain.
   Friction: nothing in the scaffolder recap warned me (`guard` looks reassuring); only JD036, an Info rule, would have. I would expect the recap to mention "one-sided decimal" at least as `to verify`.
7. **The scaffolder's output triggers its own analyzer, and in one case wrongly.** With the Info rules raised to warnings: `AnyTemperatureRange.cs(32,16): warning JD036: This floating-point dummy declares no range: one draw in ten spans the whole type` (twice, for min and max).
   But `TemperatureRange` is drawn through `Any.OrderedPair` with the other end, so min is always below max <= 40 and the "whole type" is not reachable. The warning is noise there and the fix is to write `Between(-40m, 40m)` on both, which I did and which also reads better. Lone one-sided bounds of a pair could be skipped by JD036, or the tool could emit a closed range when the pair's other guard is a constant bound.
8. **The first green run was not green for long: the draw found a bug in my own test (13 failures in 40 runs).** `A_calm_pallet_has_nothing_to_inspect` failed with `Assert.Empty() Failure: Collection was not empty — Collection: ["overweight"]` and
   `[JustDummies] These arbitrary values were seeded with 584436450. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 584436450)].`
   My "calm" line drew its quantity freely (0 to 10 000) times 0.1 kg: up to 1 000 kg per line, so two lines passed 1 200 kg. The quantity was not a dummy for this test, the test was about the pallet being light. I constrained the quantity to 0–100 in the helper. The seed message is exactly what the docs promise and the `Seed = …` text is directly pasteable. This is the library working as described.
9. **`receivedAt` is `to verify`, and the answer lives on another page.** The tool stops the build on `if (receivedAt.Kind != DateTimeKind.Utc)`. Whether `Any.DateTime()` honours it I only found on `generators/dates-and-times.en.md` ("Every value `Any.DateTime()` generates carries `DateTimeKind.Utc`"). The tool could know this one (it reads the library by name) and either infer it or put the hint in the TODO text. (`idea`, low.)
10. **Entry point needs `using Warehouse.Domain;`.** `Any.Sscc()` is declared in the namespace of the domain type (default), so every test file needs that using. Natural, but the first compile error (`'Any' does not contain a definition for 'Sscc'`, I did not keep the exact text) made me think the entry point had not been generated. The generated file header says nothing about it.
11. **`[Theory]` + `decimal` literals.** C# attributes do not take `decimal`, so I wrote `double` parameters and cast `(decimal)tare`. Not a library matter; noted because `Any.Decimal()` made me expect no such detour.

## What worked well
- The getting-started page is excellent at the one thing that matters: the "does it reach the assertion?" rule. It governed how I wrote every test (literal for 10/9/1200/29 days/0.01 tare, dummies for sscc, bin, gtin, lot, supplier code…) and it stopped me from drawing the level in the reach-truck test.
- `any generate` for eight types in one command, with a recap column (`guard`, `AnyX`, `to verify`) that is actually informative; the generated `OrderedPair` for `min >= max` was a nice surprise (I would have written a tie by hand). Types composed through each other (`ReceiptLine` -> `AnyEan13`, `AnyQuantity`, `AnyLotNumber`) came out right.
- A build that stops at the `TODO` with the guard and line named: I never wondered what was left.
- The CLI page's "Draw the body, then derive the check digit" section was exactly my case (GS1 check digits) and its snippet worked unchanged.
- The `With…` lambda/pin duality reads well in tests: `Any.ReceiptLine().WithExpected(new Quantity(10)).WithReceived(new Quantity(9))`.
- `[assembly: Reproducible]` + the seed in the failure output: caught my own bug on the first repeated runs and the message is replayable as printed.
- Analyzers: JD036 teaches the right thing at the right place (the unbounded decimal I had suspected).
- Constraint conflicts are refused at the line that declares them (not hit here, but the `OrderedPair`/`WithMaxCelsius(-40)` case is the one I was wondering about).

## Fluency: 4 / 5
I wrote the whole suite in one sitting from the docs alone and the library mostly got out of the way; the scaffolder and the golden rule were the high points. Points lost for: the undocumented `dotnet test` setup on .NET 10 (friction 1), the manual retype on pattern-plus-check-digit generators (3), the null ambiguity (4), and the scaffolded one-sided `decimal` which silently carried a 10 % overflow into a domain that multiplies it (6, 7).
