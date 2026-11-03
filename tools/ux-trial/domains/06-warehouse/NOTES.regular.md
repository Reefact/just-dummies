# Regular notes — 06 warehouse
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read, in order: `docs/PROJECT-README.md`, `packages/justdummies-cli.en.md` (the guard table, "to verify", "When the replacement is another kind of generator"), `packages/justdummies-xunit.en.md`, `generators/strings.en.md` (Patterns), `generators/dates-and-times.en.md` (skimmed), the headings of the other generator pages. The domain: `MODEL.md` and every file in `Domain/`.
- Created `Tests.Regular/Warehouse.Domain.Tests.Regular.csproj` from the template, `dotnet restore`, then
  `any generate <Type> --output Generators` for Sscc, Ean13, LotNumber, Quantity, TemperatureRange, BinLocation, ReceiptLine, PalletReceipt (one call each).
- Generators completed by hand:
  - `AnySscc`, `AnyEan13`: `Any.StringMatching("[0-9]{17}"/"[0-9]{12}").As(d => d + Gs1.ComputeCheckDigit(d))`; retyped the field/ctor to `IAny<string>` and deleted the `Func<AnyString,AnyString>` overload as the CLI page says.
  - `AnyBinLocation`: aisle `Any.Char().Alpha().InUpperCase()`, level `Any.Int32().Between(1, 20)`.
  - `AnyPalletReceipt`: supplier code `Any.StringMatching(@"SUP-\d{5}")` (retyped `AnyString` to `AnyPattern` on 4 lines), removed the sentinel on `receivedAt` (a `DateTime` draw is always Utc).
  - `AnyReceiptLine`: bounded the unit weight (see friction 4).
  - Left as scaffolded: `AnyLotNumber`, `AnyQuantity`, `AnyTemperatureRange` (the tool wired the min/max order itself with `Any.OrderedPair`).
- Hand-written support: `Support/Calm.cs` (a receipt line / receipt that raises no inspection reason, to break one rule at a time), `GlobalUsings.cs`, `AssemblyInfo.cs` (`[assembly: Reproducible]`).
- Tests (145 methods, 224 cases): `Gs1Tests`, `SsccTests`, `Ean13Tests`, `LotNumberTests`, `QuantityTests`, `TemperatureRangeTests` (bounds, order, zones by maximum only), `BinLocationTests` (ctor, ToString, reach truck, equality, Parse round-trip and refusals), `ReceiptLineTests` (discrepancy, weight), `PalletReceiptConstructionTests` (arguments, supplier format, Utc, lines copied, totals), `PalletReceiptInspectionTests` (each reason, 1200 kg and 30-day boundaries, order, once each, status untouched), `PalletReceiptLifecycleTests` (Quarantine/PutAway transitions and refusals), `EnumTests`.
- Result: green, 224 cases, 0 failed, 5 skipped (6 cases, all `domain defect`, each confirmed to fail once un-skipped). After the last fix: 17 consecutive green runs (14 + 3), fresh seed each (assembly-wide `[Reproducible]`).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command given in my brief) fails on the .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch ... --project Tests.Regular`. And `dotnet test Tests.Regular` fails with
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later... opt-in to the new dotnet test experience`.
   xunit.v3 4.0.2 pulls Microsoft.Testing.Platform, so the opt-in is mandatory. I fixed it with a `global.json` at the workspace root
   (`{"test":{"runner":"Microsoft.Testing.Platform"}}`). Nothing in the JustDummies docs (xunit page, getting-started) tells a consumer this; the template csproj alone is not enough on a .NET 10 SDK.
2. `any generate` before `dotnet restore`: `✗ Sscc: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, one extra step. Fine.
3. The recap does not say that the TemperatureRange pair was ordered: the rows read `Any.Decimal().GreaterThanOrEqualTo(-40m)` / `LessThanOrEqualTo(40m)`, as if min and max were independent (a reader would think `min >= max` could be drawn). Only the generated file shows `Any.OrderedPair(..., strict: true)`. Not a bug, but a recap line such as "ordered pair" would save a read of the file (the doc has a section for it, the recap is silent).
4. A drawn value that was not a dummy for the domain: `Any.Decimal().Positive()` (what `any` inferred from `unitWeightKg <= 0`) reaches decimal's own limit, and `ReceiptLine.WeightKg = Received.Units * UnitWeightKg` then throws.
   Failures seen, about every second run: `PalletReceiptConstructionTests.The_net_weight_matches_the_lines_it_holds` and `PalletReceiptInspectionTests.Inspecting_twice_gives_the_same_answer`:
   `System.OverflowException : Value was either too large or too small for a Decimal.` at `ReceiptLine.get_WeightKg() ReceiptLine.cs:33`, seed -113536279 for the second.
   My reading: that is a domain defect (no upper bound on a unit weight; later computations overflow), not flakiness. I kept a deterministic test (`decimal.MaxValue`, 2 units), skipped with the reason, and bounded the generator's base to `<= 1_000m` with a comment, since the constructor itself has no such invariant. The "guard" tag on that row suggests the draw is domain-valid, while the open upper end is exactly where the library goes to the limit (documented, but it bites on any decimal parameter that is later multiplied).
5. The Sscc/Ean13 `.As(...)` replacement erases the refining overload, so the scaffolded `WithValue(Func<...>)` disappears. Documented ("delete the refining overload"), but it leaves `AnySscc` with only a pin: I could not write `new AnySscc().WithValue(v => v.StartingWith("0"))` for an extension digit, and had to derive a second code by hand in `Two_codes_with_different_values_are_not_equal`. Would have liked a doc recipe for "check-digit-bearing values: keep a refinable base" (e.g. a `WithExtensionDigit` partial).
6. Retype ritual for `AnyPattern` is four edits per generator (field, factory, ctor parameter, Func) and the compiler error is `CS0029`. Documented and mechanical; with PalletReceipt having 5 parameters I did it by a small script. An `any generate --pattern supplierCode=...` or a regex-guard conversion would remove a recurring chore (the domain's guards are regexes for 3 of 8 types).
7. `Any.Decimal().Between(-40m, 39m)` etc. worked first time; but `new AnyTemperatureRange().WithMaxCelsius(-40m)` (my theory case "max = -40 is Frozen") throws at declaration: `ConflictingAnyConstraintException : Cannot apply WithSecond(-40) because no first value lies below -40: GreaterThanOrEqualTo(-40) already requires values greater than or equal to -40.` Correct and clearly worded (a max of -40 is unreachable since min >= -40 and min < max); the error stack points at the generated `MinCelsiusAndMaxCelsius()` rather than my line, but the test frame is in the trace. I fixed my test (-39.99).
8. My own mistake worth noting: hand-computed SSCC literals with wrong check digits; the domain's message `'906104250021234563' does not carry a valid GS1 check digit.` found them immediately.

## What worked well
- `any` got 6 of 8 types fully right or with a precise `to verify` (the messages quote the line and the guard). The guard-to-recipe mapping (`Between(0, Quantity.Maximum)` referencing the public constant, `NotBlank().WithMaxLength(20)`, `Positive().LessThanOrEqualTo(99)`, the ordered pair, `Any.ListOf(...).NonEmpty()`) saved real time, and the non-compiling `_ = TODO_verify_...` sentinel is a good forcing function.
- `[assembly: Reproducible]` plus the seed printed with the failure: the two overflow failures were diagnosed from the stack alone, and the seed line gave me a replay for the notes. `With…(lambda)` over a scaffolded graph (`new AnyBinLocation().WithLevel(l => l.GreaterThan(4))`) reads well.
- `Any.StringMatching` with `As` for check-digit values; `Except` / `DifferentFrom` on numbers for "any wrong check digit".
- The analyzers stayed silent on a 145-method suite (no false positive).

## Fluency: 4 / 5
Daily flow was smooth: scaffold, fix four generators, write tests with `Any` for the dummies and literals for what the assertions are about. The deductions: the `dotnet test --project` / MTP opt-in on .NET 10 cost real time and is undocumented from the consumer's side; the `AnyString` -> `AnyPattern`/`IAny<string>` retype is a chore repeated per regex guard and drops the refinement overload; and the unbounded decimal draw makes a domain with an unchecked multiplication fail on about half the runs (honest, but I needed to read the stack to conclude that).
