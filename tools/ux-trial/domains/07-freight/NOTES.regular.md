# Regular notes — 07 Freight
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Read MODEL.md and the whole `Domain/` first (9 small files), then skimmed the docs as a reminder, in this order:
  `PROJECT-README.md` (diagonally), `guides/getting-started`, `packages/justdummies-cli` (read closely: the guard table,
  "unread guards", "When a guard ties two parameters", "Adjusting a parameter"), `packages/justdummies-xunit`,
  `generators/numbers`, `dates-and-times`, `strings` (patterns), `enums-and-choices`, `collections`, `guides/composition`.
- Test project `Tests.Regular/Freight.Domain.Tests.Regular.csproj` from the template, plus `W/global.json` (see friction 1).
- `any` command (one run, from `Tests.Regular`):
  `any generate UnLocode ContainerCode Weight Volume FreightRate Leg Shipment --output Generators --entry-point any`
  (`AnyContainerType` is not needed: `Any.Enum<ContainerType>()`).
- Generators completed or rewritten by hand (all in `Generators/`):
  - `AnyUnLocode`: regex guard -> `Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}")`, retyped `AnyString` -> `AnyPattern` on 4 lines as the docs say.
  - `AnyFreightRate`: `Any.Decimal().Between(0.01m, 10_000m).WithScale(2)` and `Any.StringMatching("[A-Z]{3}")` (same retype).
  - `AnyContainerCode`: `Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(firstTen => firstTen + ContainerCode.CheckDigitOf(firstTen))`;
    factory/recipe field/ctor parameter retyped `IAny<string>`, refining overload deleted (docs, "replacement ends in `.As`").
  - `AnyLeg`: rewritten almost entirely. The scaffold paired departure/arrival with `Any.OrderedPair` but could not know the 90-day maximum
    and origin != destination. Now: departure drawn in 2020-2030 with an offset between -12h and +14h, a drawn duration (1 minute to 90 days),
    arrival = departure + duration at another drawn offset; `WithDeparture`/`WithArrival`/`WithDuration` pins; destination `.DifferentFrom(origin)`.
  - `AnyItinerary` (new, hand written, `IAny<IReadOnlyList<Leg>>`): N distinct ports, chained legs with a layover from 0 to 10 days.
  - `AnyShipment`: legs recipe retyped to `AnyItinerary`; cargo bases valid whatever the type (weight <= smallest payload, volume <= smallest
    capacity, both computed from `ContainerTypeSpecifications`); `AnyWeight`/`AnyVolume` scaffolds untouched (`guard` rows were right).
- Tests (143 methods, 245 cases, files in `Tests/`): `UnLocodeTests` (format, country, null/blank, equality, ToString),
  `ContainerCodeTests` (ISO check digits incl. remainder 10 -> 0, format vs check digit order, `Create`, `CheckDigitOf` reading 10 characters,
  equality), `WeightTests`, `VolumeTests` (boundaries with `double.BitIncrement`, NaN/infinities/negative zero, equality, ToString),
  `ContainerTypeTests`, `FreightRateTests` (bounds, scale, check order, currency), `LegTests` (90-day boundary to the tick, instants vs local
  clocks, check order), `ShipmentConstructionTests` (every guard in the documented order via `ParamName`, per-type payload/capacity
  boundary, itinerary rules), `ShipmentComputationTests` (arrival, transit, W/M, utilisation, offset conversion, quote rounding away from
  zero), `ShipmentLifecycleTests` (the whole state machine, `IsLateAt`).
- Result: green, 239 passed + 6 skipped = 245 cases (the 6 skipped are domain defects, below). Repeated: 3 runs during development,
  then 12 more consecutive full runs (fresh seeds each, `[assembly: Reproducible]`), all green. No run ever failed on a drawn value.
- Domain defects kept as skipped tests (verified red by temporarily removing the `Skip`): trailing line feed accepted (3 tests), `Create` with a
  short owner (1), culture-dependent `ToString` (2).

## Frictions and surprises, in order
1. `dotnet test --project Tests.Regular` (the command of the brief) fails on a plain .NET 10 SDK:
   `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`. Using the positional form instead gives
   `Microsoft.Testing.Platform.MSBuild.targets(395,5): error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. ... opt-in to the new dotnet test experience`.
   The fix is a `global.json` with `{ "test": { "runner": "Microsoft.Testing.Platform" } }`; I had to know it. The xUnit adapter page says
   nothing about it (it is only the library repo that pins it). A consumer on the .NET 10 SDK hits this on day one. Added `W/global.json`.
2. The scaffold recap is accurate but a check-digit type looks innocent: `ContainerCode` shows `value string Any.String().NotBlank() to verify, guard, unread guards`
   and "first unread guard ... line 12 ..., and 1 more". Nothing says that the 'more' is the check digit which no per-parameter recipe can
   honour, so I opened the constructor to find out. The docs do cover the case ("Draw the body, then derive the check digit") and `.As` solved it
   in one line, but the recap could name it.
3. `Leg`: the recap marks all four parameters `to verify`, and the generated `AnyLeg` already pairs departure/arrival with `Any.OrderedPair(strict: true)`
   (good), yet the other tie (`arrival - departure <= 90 days`) cannot be expressed on that pair, so I ended up rewriting the file
   (about 150 lines, including the private copy constructor and every `With...`). The docs' "start and a duration" recipe is the right one but
   "`Generate()` is ordinary code" is not enough here: the pins (`WithArrival`) must keep working, so the whole machinery has to change.
   Cost: high for a very common shape (period with a maximum length). An `OrderedPair` variant bounded by a maximum gap would remove it.
4. Unconstrained scaffold of `DateTimeOffset` (`Any.DateTimeOffset()`) spans year 1 to 9999, which overflows as soon as a duration is added;
   documented in `dates-and-times` but the scaffold does not warn, it only says `to verify`. I bounded it to 2020-2030 in the base.
5. `AnyValidContainerType` has a tie to weight and volume (`to verify`) while the generated shape (independent draws) cannot honour it.
   I chose "cargo valid for every type" (smallest payload/capacity computed from the domain's own table) instead of deriving the cargo from
   the drawn type, because derivation breaks the `With...` pins (docs warn about it). This works but means drawn shipments never carry a big
   cargo; a test about big cargo has to pin both type and cargo. Fine, but it is a decision the doc could present as the default.
6. `dotnet test` after restoring an older copy of a source file (mtime older than the last build output) silently reused the stale assembly:
   skipped tests ran as failures. Not a library issue (my `cp -r`/`mv` dance), noted because the symptom looked like a Skip not being honoured.
7. xUnit analyzer `xUnit1025` fired on my `[InlineData(0.0)] [InlineData(-0.0)]` (duplicates since `0.0 == -0.0`), so negative zero needs its own `[Fact]`. Not JustDummies.
8. `JD036` (raised to warning to see it): `Any.Double().Negative()` in "a negative weight is refused" says "declares no range: one draw in ten spans the whole type".
   Correct and useful in general; here the width is exactly the point (any negative is refused), so I kept it. I cannot tell JD036 "this one is on
   purpose" except by suppression; a range would have changed the intent of the test. Low.
9. Info diagnostics (JD030, JD036) are invisible in `dotnet build`; I only saw them after adding an `.editorconfig`. Documented in the analyzers
   README, so only a reminder; I removed the file afterwards.

## What worked well
- The scaffold got the plain value objects right unattended: `Weight`/`Volume` came out as `Any.Double().Positive().LessThanOrEqualTo(Weight.MaxGrossKilograms)`
  referencing the public constant, `Shipment.id` as `Any.Guid().NonEmpty()`, enums as `Any.Enum<>()`.
- `TODO`/`to verify` stops the build exactly where my attention was needed; every guard I would have forgotten (regex, 90 days, contiguity) was flagged.
- `[assembly: Reproducible]` plus the seed line in the failing output: the four times a test was red (my own wrong expectations) I had the seed immediately;
  none of the runs failed because of a drawn value.
- `.DifferentFrom(x)` on scaffolded generators and typed builders (`Any.ContainerCode().DifferentFrom(code)`, `code.DifferentFrom(anyOrigin.Code)`) made the
  "two different values" tests one-liners.
- Refining lambdas (`WithKilograms(k => k.LessThanOrEqualTo(...))`, `WithOrigin(o => o.WithCode(c => c.DifferentFrom(...)))`) read well.
- Pinning a value I care about (`WithDeparture(...)`, `WithGrossWeight(new Weight(26_700))`) next to arbitrary everything else made the tests say what they are about.

## Fluency: 4 / 5
Everything I needed was in the docs and the tool's output, and the green/red cycle was quick. I lost most of the time on `Leg` (a rewritten generator
for a period with a maximum duration) and on finding the MTP opt-in for `dotnet test`. A regular user would still be productive; a period-with-maximum
helper and a note in the xUnit page would take it to 5.
