# 07 — Freight (shipments, ISO 6346 containers, ETA across time zones)

## Domain model (`Domain/`)

| Type | Shape | Guards (shape used) |
|---|---|---|
| `ContainerCode` | ISO 6346 code, `Create(owner, category, serial)` computes the check digit | `ArgumentException.ThrowIfNullOrWhiteSpace`, static `Regex` field `IsMatch`, check digit in a private `Validate` |
| `Weight` | gross kg (double) | `if (!double.IsFinite)`, `ArgumentOutOfRangeException.ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(k, const)` |
| `Volume` | m3 (double) | `if (double.IsNaN)`, relational pattern `if (v is <= 0 or > MaxCubicMetres)` |
| `UnLocode` | 5-char port code | `ThrowIfNullOrWhiteSpace`, `Regex.IsMatch(code, "^[A-Z]{2}[A-Z2-9]{3}$")` |
| `Leg` | origin, destination, departure/arrival `DateTimeOffset` at each port's offset | null checks, cross-field `origin.Equals(destination)`, `arrival <= departure`, `arrival - departure > 90 days` |
| `FreightRate` | decimal per revenue ton + currency | switch expression with throwing arms (`<= 0`, `> 10_000`, scale > 2), then `ThrowIfNull` + static `Regex` on currency |
| `ContainerType` (enum) + `ContainerTypeSpecifications` | payload / capacity per type via switch expressions | |
| `ShipmentStatus` (enum) | Booked → InTransit → Delivered, or Cancelled | |
| `Shipment` (aggregate) | id, container, type, weight, volume, legs | `Guid.Empty`, `Enum.IsDefined`, cross-field payload/capacity vs type, `ValidateItinerary` (non-empty, contiguous, chronological) |

Behaviour: ETA (last leg arrival) and `EstimatedArrivalAt(offset)`, planned transit time across offsets,
W/M revenue tons, `QuoteWith(rate)` rounded to the cent, volume utilisation, `IsLateAt(now)`,
`Depart` / `Deliver` / `Cancel` state transitions.

## CLI recaps (verbatim)

`any generate Weight Volume UnLocode` (after `dotnet restore`):

```
Analyzing Freight.Domain.Weight
  constructor Weight(double)

  kilograms  double  Any.Double().Positive().LessThanOrEqualTo(32500d)  to verify, guard, unread guards

✓ AnyWeight.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Freight.Domain.Volume
  constructor Volume(double)

  cubicMetres  double  Any.Double()  to verify, unread guards

✓ AnyVolume.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Freight.Domain.UnLocode
  constructor UnLocode(string)

  code  string  Any.String().NotBlank()  to verify, guard, unread guards

✓ AnyUnLocode.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

(`Volume` was then rescaffolded with `--force` after splitting its guard so that the relational
pattern stood alone; the recap stayed `Any.Double()  to verify, unread guards`.)

`any generate ContainerCode Leg FreightRate Shipment --entry-point static:Arbitrary`:

```
Analyzing Freight.Domain.ContainerCode
  constructor ContainerCode(string)

  value  string  Any.String().NotBlank()  to verify, guard, unread guards

✓ AnyContainerCode.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyContainerCode.Entry.cs — entry point Arbitrary.ContainerCode()

Analyzing Freight.Domain.Leg
  constructor Leg(UnLocode, UnLocode, DateTimeOffset, DateTimeOffset)

  origin       UnLocode        new AnyUnLocode()     to verify, AnyX, unread guards
  destination  UnLocode        new AnyUnLocode()     to verify, AnyX, unread guards
  departure    DateTimeOffset  Any.DateTimeOffset()  to verify, unread guards
  arrival      DateTimeOffset  Any.DateTimeOffset()  to verify, unread guards

✓ AnyLeg.cs — 4 of 4 parameters inferred, 4 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyLeg.Entry.cs — entry point Arbitrary.Leg()

Analyzing Freight.Domain.FreightRate
  constructor FreightRate(decimal, string)

  perRevenueTon  decimal  Any.Decimal()            to verify, unread guards
  currency       string   Any.String().NonEmpty()

✓ AnyFreightRate.cs — 2 of 2 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyFreightRate.Entry.cs — entry point Arbitrary.FreightRate()

Analyzing Freight.Domain.Shipment
  constructor Shipment(Guid, ContainerCode, ContainerType, Weight, Volume, IReadOnlyList<Leg>)

  id             Guid                Any.Guid().NonEmpty()      guard
  container      ContainerCode       new AnyContainerCode()     AnyX
  containerType  ContainerType       Any.Enum<ContainerType>()  to verify, unread guards
  grossWeight    Weight              new AnyWeight()            to verify, AnyX, unread guards
  volume         Volume              new AnyVolume()            to verify, AnyX, unread guards
  legs           IReadOnlyList<Leg>  Any.ListOf(new AnyLeg())   to verify, unread guards

✓ AnyShipment.cs — 6 of 6 parameters inferred, 4 to verify.
  The file will not compile until you resolve it. That is deliberate.
✓ AnyShipment.Entry.cs — entry point Arbitrary.Shipment()
```

Also run: `--dry-run` (ContainerCode), `--format json --dry-run` (Shipment: summary
`{"scaffolded":1,"failed":0,"openParameters":0,"parametersToVerify":4}`), `--entry-point any --dry-run`
(UnLocode, emits a C# 14 `extension(Any)` file), and a re-run without `--force`:

```
✗ .../Tests/AnyWeight.cs already exists.
  Re-run with --force to overwrite it. Whatever you changed in it is lost.
```
(exit 1)

## What I fixed by hand

* `AnyWeight`: recipe correct; deleted the sentinel (the unread guard was `!double.IsFinite`, which no draw can hit).
* `AnyVolume`: `Any.Double()` → `.Positive().LessThanOrEqualTo(Volume.MaxCubicMetres)` (the `is <= 0 or > Max` pattern was not read).
* `AnyUnLocode`: replaced with `Any.StringMatching("[A-Z]{2}[A-Z2-9]{3}")` — which forced changing the base's return type
  `AnyString` → `AnyPattern` **and** the `WithCode(Func<AnyString, AnyString>)` overload.
* `AnyContainerCode`: the check digit cannot be a pattern; base rewritten as `Any.Combine(owner, category, serial, ...)`
  returning `IAny<string>`, and the refine overload `WithValue(Func<AnyString, AnyString>)` deleted.
* `AnyFreightRate`: rate → `.Positive().LessThanOrEqualTo(10_000m).WithScale(2)`; currency → `.Alpha().InUpperCase().WithLength(3)`
  (that guard was **not** reported — see the bug below).
* `AnyLeg`: departure → bounded window, `WithOffsetBetween(-12h, +14h)`, minute granularity. Arrival depends on the departure,
  which the one-independent-generator-per-parameter shape cannot say: added `_transit` / `_arrivalOffset` fields, made `_arrival`
  optional, rewrote the private constructor, every `With…`, and `Generate()`; destination redrawn (bounded) when equal to origin.
* `AnyShipment`: weight/volume narrowed to the smallest payload/capacity of all types (cross-field rule written in `AnyValid…`),
  legs → a hand-written `AnyItinerary` (contiguous, chronological chain), so `WithLegs(Func<AnyList<Leg>, …>)` became
  `Func<AnyItinerary, AnyItinerary>`. `containerType` (`Enum.IsDefined` + cross-field) kept as `Any.Enum<ContainerType>()`.

Every hand-resolved generator then passed its 500-draw harness test (`ScaffoldedGeneratorTests`).

## Findings

1. **High — CLI silently misses a guard written below a property assignment.** `FreightRate`'s constructor assigns
   `PerRevenueTon = …` first, then `ArgumentNullException.ThrowIfNull(currency); if (!CurrencyFormat.IsMatch(currency)) throw`.
   The recap shows `currency  string  Any.String().NonEmpty()` with **no** `to verify`, the file compiles, and the drawn
   currency is rejected on nearly every draw. Minimal repro: `ZzC(decimal rate, string currency) { Rate = rate; if (currency.Length != 3) throw …; }`
   → `currency  string  Any.String().NonEmpty()` (no mark), while the same guard without the assignment reads `.WithLength(3)  guard`.
   Demonstrated by `Bug_Scaffolded_currency_recipe_ignores_a_guard_written_below_a_property_assignment` (failing on purpose).
   Diagnosis (spec, not implementation): §5.3 stops the leading scan at the first field/property assignment; nothing below it is
   marked, and §9 does not list that as one of the "silent" shapes.
2. Medium friction — replacing a scaffolded string base by a pattern or a composition requires changing the base's return
   type and hand-editing (or deleting) the `With…(Func<…>)` overload; the TODO text says only "verify … or replace it".
3. Medium friction — dependent parameters (arrival after departure, weight vs container type, a chain of legs) have no seam:
   I had to restructure the generated constructor and every `With…`.
4. Medium analyzer — JD007 and JD009 do not fire on draws through scaffolded generators (`new AnyWeight().Generate()` in a
   static initializer, `new AnyVolume().Generate()` in a `[Reproducible]` class constructor); the same draws through
   `Any.Double()` fire. JD028 fires for `.As(d => new Leg(…))` but not for `Any.Combine(…, (…) => new Leg(…))` nor for a scaffolded `AnyLeg`.
5. Low doc — the xUnit adapter page never shows `using JustDummies.Xunit;`; `[Reproducible]` failed with CS0246 until I found the namespace in the PublicAPI file.
6. Low doc — `DateTimeOffset.WithGranularity` with a non-whole-hour offset snaps the **UTC** clock (`2025-12-23T09:30+05:30` for a 1 h grid); the page does not say which clock.
7. Low CLI — `Volume`'s relational pattern `v is <= 0 or > Max` is not read (honestly marked); `!double.IsFinite`/`double.IsNaN` are marked unread though no draw can violate them; the constant `Weight.MaxGrossKilograms` is emitted as the literal `32500d`.
8. Low missing-capability — a guard `Regex.IsMatch(p, "<constant pattern>")` could be scaffolded as `Any.StringMatching(pattern)`.
9. Low analyzer — nothing flags a drawn `anyX` reaching an assertion (`AnalyzerProbes.A_drawn_value_reaching_the_assertion`); constant crossed bounds on `Double` are not checked at build time (JD023 is integer-only, as documented).

## What worked well (Probes.cs)

Conflicts fail fast and name both sides (`Cannot apply LessThan(10) because GreaterThan(100) already requires values greater
than or equal to 100.00000000000001.`); an exclusive double range with one representable value draws it, one with none is
refused; instants at +14:00 near `DateTimeOffset.MaxValue` are drawn correctly or refused
(`Cannot apply WithOffset(14:00:00) because After(9999-12-31T21:59:59.9999999+00:00) already requires …`); bounds given at
different offsets compare as instants; sub-minute / >14 h offsets are refused clearly; `\p{Lu}` and lookahead are refused
with position; seeds replay (`Any.Reproducibly`, `Any.WithSeed`, and `[Reproducible(Seed = -170842253)]` reproduced the exact
failing currency string). A scaffolded refinement lambda outside the base range is refused naming both `After(2040…)` and
the base's `Between(2024…, 2034…)`.

## Fluency: 4 / 5

Writing the domain tests was fluent: `Arbitrary.Shipment().WithGrossWeight(new Weight(20_000)).WithVolume(new Volume(12))`
reads exactly as intended, the refine lambdas compose with the base, and everything compiled on the first try except the
missing `using JustDummies.Xunit;`. The point lost is the scaffolding of a real aggregate: three of seven generators needed
structural rewrites (pattern/combination bases, dependent parameters), and one guard was silently missed.
