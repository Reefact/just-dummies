# Expert notes — 07 Freight
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
Pages read, in order: `docs/PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (the whole page, it is the long one), `packages/justdummies-xunit.en.md`,
`guides/composition.en.md`, then the generator pages `numbers`, `dates-and-times`, `strings` (first half),
`enums-and-choices` (first half) and `collections`. I did not open the analyzer pages: no diagnostic ever showed up.
Also read `MODEL.md` and all of `Domain/` first.

`any` commands run (from `Tests.Expert/`, after a `dotnet restore`):
- `any generate UnLocode ContainerCode Weight Volume FreightRate Leg Shipment --output Generators --namespace Freight.Tests.Expert.Generators --entry-point any`
  (one call, seven types, plus the `Any.X()` entry points through C# 14 extension members).
  Result: Weight and Volume were fully inferred (`Positive().LessThanOrEqualTo(Weight.MaxGrossKilograms)`, constant referenced by name);
  UnLocode, ContainerCode, FreightRate, Leg and Shipment came out with `to verify` and a non-compiling `TODO_verify_…` line each.

Generators completed by hand (all in `Generators/`):
- `AnyUnLocode`: `Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}")`, retyped `AnyString` -> `AnyPattern` on the four lines the TODO names.
- `AnyContainerCode`: `Any.StringMatching("[A-Z]{3}[UJZ][0-9]{6}").As(body => body + ContainerCode.CheckDigitOf(body))`; because `.As` returns an
  `IAny<string>`, the recipe field, the private ctor parameter and the `Func` of the refining overload were retyped `IAny<string>`.
- `AnyFreightRate`: `Any.Decimal().Between(0.01m, 10_000m).WithScale(2)` and `Any.String().Alpha().InUpperCase().WithLength(3)` (no retype needed).
- `AnyWeight`, `AnyVolume`: untouched.
- `AnyLeg`: rewritten. The scaffold paired departure/arrival with `Any.OrderedPair(strict: true)`, which cannot honour the 90-day ceiling, so a leg is now
  a departure (2000-2100, offset -12h..+14h) plus a duration `Between(1 min, Leg.MaxTransit)`, arrival re-expressed in another drawn port offset;
  destination is `_destination.DifferentFrom(origin)` inside `Generate()`; pins for departure, arrival, duration.
- `AnyItinerary` (new, hand-written `IAny<IReadOnlyList<Leg>>`): 1..4 contiguous legs, each leaving the previous destination after a 0..10 day layover.
- `AnyShipment`: TODO lines removed, legs recipe retyped to `AnyItinerary`; `Generate()` rewritten so the type is drawn first (excluding the types a *pinned*
  weight or volume would not fit in), then weight and volume are refined with `LessThanOrEqualTo(type's limit)`. Pins of type/weight/volume are tracked in
  nullable fields because a `Fixed` generator cannot be told from a recipe.

Tests written (`Tests/`, `[assembly: Reproducible]`), 119 test methods, 248 cases, 4 skipped:
- `UnLocodeTests` (country, accepted forms incl. digits 2-9, 8 malformed cases, null vs blank exception types, equality, 1 skipped defect)
- `ContainerCodeTests` (7 independently computed check digits incl. remainder 10 -> 0, only-first-ten-characters, short string -> IndexOutOfRange,
  owner/category, 8 malformed values, wrong check digit with message, null/blank, `Create` padding and malformed parts, equality, 2 skipped defects)
- `WeightTests`, `VolumeTests` (boundaries, NaN/infinities, -0.0, MetricTons, equality), `ContainerTypeSpecificationsTests` (table, undefined values)
- `FreightRateTests` (range, decimals, ceiling-before-decimals order, currency format, null, check order, 1 skipped defect)
- `LegTests` (same port, null ports, zero/negative/1-tick duration, exactly 90 days vs +1 tick, instants versus local clocks across offsets)
- `ShipmentConstructionTests` (every guard, per-type payload and capacity boundary equal/above, itinerary contiguity, equal-instant departure, copy of the legs)
- `ShipmentComputationTests` (EstimatedArrival, PlannedTransitTime, offsets, W/M revenue tons, utilisation, quotes incl. midpoint-away-from-zero cases)
- `ShipmentLifecycleTests` (every transition from every state, terminal states, state unchanged after a refusal, instant comparison, status checked before time)
- `ShipmentLatenessTests` (strict `>`, instants, four statuses)
- `GeneratorTests` (my generators are infrastructure: 300 shipments/legs drawn, pinned load picks a fitting container, pinned type bounds the load, leg count)

Result: green on the first full pass after one mistake of mine (a wrongly computed check digit in a literal I typed), then 12 further runs, all green
(244 passed, 4 skipped each time, fresh seed each time). Run with `dotnet test --project Tests.Expert` after adding a `global.json` (see friction 1).

Domain defects found, kept as skipped tests with the defect named: (a) `UnLocode("FRMRS\n")`, `ContainerCode("CSQU3054383\n")`, `FreightRate(…, "EUR\n")` are
accepted and keep the newline (`$` matches before a final line feed); (b) `ContainerCode.Create("CS", 'U', 305438)` throws `IndexOutOfRangeException`, not the
`FormatException` MODEL.md promises for malformed parts. These are literal inputs, so there is no seed to quote. I un-skipped them once to confirm each fails as described.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` does not work out of the box on the .NET 10 SDK.** First error: `MSBUILD : error MSB1001: Unknown switch ... Switch: --project`;
   then, with the project as a plain argument: `Microsoft.Testing.Platform.MSBuild.targets: error : Testing with VSTest target is no longer supported by
   Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.` The workspace had no `global.json`;
   I wrote `{ "test": { "runner": "Microsoft.Testing.Platform" } }` at W. The xunit adapter page lists `xunit.v3` 4.0.2 as the floor but says nothing about how to run
   the tests (the only `dotnet test --solution/--project` lines are in the contributing text for maintainers). A consumer following the install page lands here.
2. **`any generate` needs a restored project and says so well** (`✗ UnLocode: this project has not been restored, so its package references cannot be read. Restore it, then run the command again: dotnet restore`).
   No issue, noted because the docs' "run it from your test project" does not mention restore first.
3. **Default namespace of the generated files is the *domain's* namespace** (`Freight.Domain`), i.e. generators for test code would sit in the production namespace.
   The recap line says `in namespace ... (--namespace to change)` so I noticed and passed `--namespace`. I would have expected the test project's root namespace by default,
   or at least a mention in the CLI page's quick start. Low.
4. **A regex guard marks the parameter `to verify` and ends in a retype chore.** UnLocode: 4 lines (`AnyString` -> `AnyPattern`); ContainerCode: field, ctor, `Func` and the refining
   overload become nearly meaningless (`Func<IAny<string>, IAny<string>>` refines nothing). The tool's TODO comment is precise and the doc section "When the replacement is another kind
   of generator" has the full sample, so it is mechanical; but I did it with a search/replace, which is exactly the kind of edit a re-scaffold (`--force`) erases.
   Idea: when the guard is a recognisable regex the tool could offer the `Any.StringMatching` base as a suggestion in the TODO text or emit the retyped file.
5. **The ISO 6346 check digit is the case the docs name ("a check digit computed from the digits before it") and the doc's `.As(body => body + LuhnDigit(body))` recipe fits it
   perfectly.** I used the domain's public `ContainerCode.CheckDigitOf` there. For the ContainerCode tests themselves I computed seven check digits independently (Python) as literals,
   because a generator that calls the code under test cannot test it.
6. **Scaffold for `Leg` tied departure and arrival with `Any.OrderedPair(strict: true)` and the generated comment says the pair is drawn "as the constructor requires",
   but the guard right below (`arrival - departure > MaxTransit`) is not an order and cannot be expressed that way.** The recap did flag all four parameters `to verify`
   (`ties arrival to departure`) so the build stopped, correctly. The documented recipe "draw a start and a duration" is what I wrote. But keeping the scaffolded pair and
   deleting only the TODO lines would have compiled and produced legs over 90 days on most draws (the pair spans 100 years), failing loudly on the first run: safe, just not helpful.
   The tool could notice a second guard on the same two parameters and not claim the pair is sufficient.
7. **Three parameters tied through a property of an enum (weight <= MaxPayloadKilograms(type), volume <= CapacityCubicMetres(type))** are flagged `ties grossWeight to containerType`
   on all three, with `and 1 more`; nothing could be inferred, so I wrote the sequence in `Generate()` (type first, then weight and volume refined by `LessThanOrEqualTo(limit)`).
   That worked first time and the docs' warning came true exactly: a *pinned* weight no longer reaches the type draw. Pinning one of the tied values is the commonest thing a test does
   (`WithGrossWeight(new Weight(28_000))`), so I had to add pin-tracking fields and an `Except(...)` over the types that cannot carry it. The docs describe the problem
   ("pinning one side of a tie while the other is still drawn on its own holds the rule only on the draws where it happens to") but give no pattern for the three-way case.
   A documented "tied through a lookup" recipe, or a way to ask the scaffolded `WithX(value)` whether it was pinned, would have saved the field bookkeeping.
8. **The pin overload of a scaffolded generator stores `Any.Fixed(value)` and drops the knowledge that it is a pin.** This is the root of friction 7: from inside `Generate()` the only
   way to react to "the caller pinned this" is to keep a second field myself. (Related: a refinement such as `WithGrossWeight(w => w.WithKilograms(k => k.GreaterThan(30_000)))` combined with
   a drawn type would conflict on some seeds, naming both sides; that is correct and loud, but test authors must know to pin the type too.)
9. **Narrowing a base bound works as documented**: `AnyWeight.WithKilograms(k => k.LessThanOrEqualTo(28_200))` over a base `Positive().LessThanOrEqualTo(32_500)` just narrows.
   Good surprise; I had braced for "a bound declared twice".
10. **`DifferentFrom` is usable on every level** (`AnyPattern` inside a lambda in tests, `IAny<T>.DifferentFrom` inside `Generate()`), which made "two different ports"
    trivial. In tests I used it for *ports that the test does not care about*, which is an invariant of `Leg`, not an assertion; I deliberately did *not* use it to build "a different locode" in the
    inequality test (that would be a constraint restating the assertion), and used two literals instead.
11. **Entry points need C# 14, fine on net10.0**, no friction; `Any.Shipment().WithLegs(legs => legs.WithLegCount(3))` reads very well once `AnyItinerary` exists. Because I replaced the scaffolded
    `AnyList<Leg>` recipe with my own `AnyItinerary`, the signature is now `WithLegs(Func<AnyItinerary, AnyItinerary>)`, which a `--force` re-scaffold would silently revert (it would then not compile, as documented).
12. **No analyzer diagnostic fired at any point** (build output: 0 warnings, 0 errors, although the docs say scaffolded `Any.String()` rows raise JD030 at Info level, invisible by default).
    Neither a help nor a hindrance here; I mention it since the docs lean on the analyzers as a safety net and I never saw them work in this trial.
13. xUnit's own analyzer `xUnit2000` pointed at `Assert.Equal(ContainerType.HighCube40.CapacityCubicMetres(), Volume.MaxCubicMetres)`: not JustDummies, just a reminder that the
    constant should be the expected side (I dropped that redundant assertion).

## What worked well
- The recap table with provenance words (`guard`, `AnyX`, `to verify`, `unread guards`) was an accurate map of the work: every parameter it marked needed thought, every one it did not mark
  (Weight, Volume) was right. Naming the constant (`Weight.MaxGrossKilograms`) instead of copying 32500 is the detail I would have asked for.
- A non-compiling `TODO_verify_…` sentinel is an excellent contract: my build, not my memory, listed the places to review, and each comment named file, line and the guard.
- The scaffold's shape (`With…(value)` pin + `With…(Func)` refinement) made tests read as `Any.Shipment().WithContainerType(type).Generate()`,
  `Any.Leg().WithOrigin(first.Destination).WithDeparture(first.Arrival)`: only what the test is about is written, everything else is visibly arbitrary.
- `Any.TimeSpan().Between`, `Any.DateTimeOffset().WithOffsetBetween` and `.ToOffset` gave offset-diverse legs for free, and several tests (instant vs local clock) are better for it.
- `[assembly: Reproducible]` is a one-liner and the seed report was only ever printed once, on my one failing run, in a form I could paste (it was a wrong literal of mine, not a drawn value).
- `Any.Fixed`, `Any.Int32().OneOf(count)` and the documented "derive inside `Generate()`" licence: I never felt fought by the library when I edited generated code.

## Fluency: 4 / 5
The documentation is thorough and the tool earns its keep on simple value objects, but this domain is mostly *relationships* (weight tied to type, arrival tied to departure within 90 days, legs chained, locodes with a
regex, a check digit) and for those the tool correctly stops and the real design work is hand-written: about a third of the generator code (AnyLeg, AnyItinerary, AnyShipment.Generate) is mine. That is the documented
design and it worked, but a pinned value that must influence an earlier draw (frictions 7-8) cost the most time, and nothing in the docs covers it end to end. The `dotnet test` run command is the only thing that
blocked me outright, for a few minutes. Once the generators existed the 119 test methods were fast to write and each says what it is about; the suite was green and stable over 13 runs.
