# 06 — Warehouse: pallet receipt

## Domain model (`Domain/`)

| Type | Guard shape |
| --- | --- |
| `Sscc(string)` | null check, `Regex.IsMatch(^[0-9]{18}$)` on a static field, GS1 mod-10 check digit |
| `Ean13(string)` | everything delegated to a private `Validate` (length, digits, check digit) |
| `BinLocation(char, int, int)` | `aisle < 'A' \|\| aisle > 'Z'`, `ThrowIfLessThan`/`ThrowIfGreaterThan` on rack, a **switch expression with a throw arm** on level, assigned to state |
| `Quantity(int)` | `ThrowIfNegative` + `if (units > Maximum) throw` |
| `LotNumber(string)` | `ThrowIfNullOrWhiteSpace` + max length 20 |
| `TemperatureRange(decimal, decimal)` | combined bounds `-40..40`, cross-field `min < max`; `Zone` is a switch expression |
| `ReceiptLine(Ean13, Quantity, Quantity, LotNumber, DateOnly, decimal)` | `ThrowIfNull` x4, `unitWeightKg <= 0` |
| `PalletReceipt(Sscc, string, DateTime, TemperatureRange?, IReadOnlyList<ReceiptLine>)` | supplier regex `^SUP-\d{5}$`, `Kind == Utc`, non-empty lines |
| `ReceiptStatus`, `StorageZone`, `PalletType` (numbered from 1) | enums |
| `Gs1` | static check-digit helper |

Behaviour: discrepancy and weight per line, the goods-in inspection policy (short delivery, overweight,
short shelf life), and the `Received → Quarantined / PutAway` state machine.

## CLI recaps (verbatim)

```
Analyzing Warehouse.Domain.Sscc
  constructor Sscc(string)
  value  string  Any.String().NonEmpty()  to verify, unread guards
✓ AnySscc.cs — 1 of 1 parameters inferred, 1 to verify.

Analyzing Warehouse.Domain.Ean13
  constructor Ean13(string)
  code  string  Any.String().NonEmpty()  to verify, unread guards
✓ AnyEan13.cs — 1 of 1 parameters inferred, 1 to verify.

Analyzing Warehouse.Domain.BinLocation
  constructor BinLocation(char, int, int)
  aisle  char  Any.Char()                                    to verify, unread guards
  rack   int   Any.Int32().Positive().LessThanOrEqualTo(99)  guard
  level  int   Any.Int32()
✓ AnyBinLocation.cs — 3 of 3 parameters inferred, 1 to verify.

Analyzing Warehouse.Domain.Quantity
  units  int  Any.Int32().Between(0, 10000)  guard

Analyzing Warehouse.Domain.LotNumber
  value  string  Any.String().NotBlank().WithMaxLength(20)  guard

Analyzing Warehouse.Domain.TemperatureRange
  minCelsius  decimal  Any.Decimal()  to verify, unread guards
  maxCelsius  decimal  Any.Decimal()  to verify, unread guards

Analyzing Warehouse.Domain.ReceiptLine   (--dry-run, then written)
  gtin          Ean13      new AnyEan13()            AnyX
  expected      Quantity   new AnyQuantity()         AnyX
  received      Quantity   new AnyQuantity()         AnyX
  lot           LotNumber  new AnyLotNumber()        AnyX
  bestBefore    DateOnly   Any.DateOnly()
  unitWeightKg  decimal    Any.Decimal().Positive()  guard

Analyzing Warehouse.Domain.PalletReceipt   (--entry-point static:Arbitrary, later --force)
  sscc                 Sscc                        new AnySscc()                                AnyX
  supplierCode         string                      Any.String().NonEmpty()                      to verify, unread guards
  receivedAt           DateTime                    Any.DateTime()                               to verify, unread guards
  requiredTemperature  TemperatureRange            new AnyTemperatureRange()                    AnyX
  lines                IReadOnlyList<ReceiptLine>  Any.ListOf(new AnyReceiptLine()).NonEmpty()  guard

Analyzing Warehouse.Domain.PalletType
  constructor PalletType()
✓ AnyPalletType.cs — no constructor parameters to infer.        (emits: return new PalletType();)

✗ Gs1: nothing here constructs it.                               (exit 1, clear)
✗ NoSuchType: no type by that name in this compilation, and nothing close to it.
```

`--format json` (on `ReceiptStatus`) gave one clean document on stdout; `--entry-point any` emitted a C# 14
`extension(Any)` file; re-running without `--force` was refused with exit 1 and a clear message.

## Fixed by hand

* `AnySscc` / `AnyEan13`: `Any.String().Numeric().WithLength(17|12).As(body => body + Gs1.ComputeCheckDigit(body))`.
  The `.As` changes the base's type to `IAny<string>`, so the generated `WithValue(Func<AnyString, AnyString>)`
  overload no longer compiles and had to be deleted.
* `AnyBinLocation`: aisle `Any.Char().Alpha().InUpperCase()`; level `Between(1, 20)` — **not flagged** by the tool.
* `AnyTemperatureRange`: both bases bounded, and `Generate()` rewritten to draw the maximum above the minimum
  (a cross-parameter rule has no per-parameter home).
* `AnyPalletReceipt`: supplier `Any.String().StartingWith("SUP-").Numeric().WithLength(9)`, receivedAt bounded
  with UTC bounds (every drawn `DateTime` turned out to be UTC anyway — undocumented).

## Findings

1. **High, CLI** — a guard placed after the first assignment to state is skipped *silently* (no `to verify`).
   `Level = level switch { >= 1 and <= 20 => level, _ => throw … }` gave `level int Any.Int32()` with an empty
   column; moving `ThrowIfGreaterThan(rack, 99)` below `Aisle = aisle;` turned rack into
   `Any.Int32().Positive()  guard` — upper bound lost, recap still says `guard`. The same switch placed first is
   marked `to verify`. Test: `Bug_Scaffolded_level_recipe_for_BinLocation_ignores_its_guard`.
2. **High, CLI** — an enum scaffolds to `return new PalletType();`: always `(T)0`, which is an undeclared member when
   the enum is numbered from 1, and a constant otherwise. Tests: `Bug_Scaffolded_AnyPalletType_draws_an_undeclared_member`,
   `Bug_Scaffolded_AnyReceiptStatus_never_varies`.
3. **Medium, analyzer** — JD028 fires on `Any.ListOf(Any.String()….As(v => new LotNumber(v))).Distinct()` but not on
   `Any.ListOf(new AnyLotNumber()).Distinct()` — the shape the scaffolder produces (`AnalyzerProbes.cs`).
4. **Medium, message** — a distinct set over a scaffolded generator says "some values come from a generator that does
   not draw from this source, so they are not reproducible from this seed alone", but the draws do replay
   (`Probe_the_opaque_set_failure_replays_from_its_seed_although_its_message_says_it_may_not`).
5. **Medium, doc** — no page shows `using JustDummies.Xunit;`; `[Reproducible]` failed with CS0246 until found in the
   public API file.
6. **Medium, friction** — a check digit (or any `.As`) cannot be the base of a scaffolded parameter without deleting its
   refinement overload; a cross-parameter rule (`min < max`, `received == expected`) needs a hand-written `Generate()`
   or an `Any.Combine` that rebuilds the object.
7. **Low, doc** — the drawn `DateTime.Kind` (always UTC, even with Local bounds) is not documented; a
   `Regex` carrying `RegexOptions.IgnorePatternWhitespace` is refused with a plain `ArgumentException`, not listed on the
   strings page.
8. **Low, analyzer** — nothing flags a drawn value reaching an assertion (`AnalyzerProbes.DrawnValueInTheAssertion`).

Clear refusals (not bugs): lookahead, `\b`, `\p{Lu}`, `[A-Z-[IO]]` subtraction, `(?x)` — each names the construct and
its position; `TimeOnly.Between(22:00, 06:00)` refused naming both ends; crossed int bounds and an empty decimal scale
window name both sides. `\d` draws ASCII digits only. Seeds replay through `[Reproducible(Seed = …)]` and
`Any.Reproducibly(seed, …)` (checked twice on a failing test: same value 1490664377).

Analyzer diagnostics seen: JD023, JD013, JD025, JD027, JD004 (error), JD016, JD017, JD015 x2, JD028 — all on purpose in
`AnalyzerProbes.cs`, all accurate. None fired on the real tests.

## Fluency: 4 / 5

The core DSL reads like the domain (`StartingWith("SUP-").Numeric().WithLength(9)`, `Between(1, 20)`), refusals are
fast and name both sides, the pattern subset covers every warehouse format I needed and refuses the rest by name, and
`.As(...)` made check digits a one-liner. The CLI was honest on every guard it could not read — except the two silent
cases above, which are exactly the ones that cost most. Points lost for the scaffolded generators' rigidity (type-locked
refinement lambdas, no place for cross-field rules) and for the missing adapter namespace in the docs.

## After #331

The CLI fixes merged in [#331](https://github.com/Reefact/just-dummies/pull/331) were re-checked on this domain by re-scaffolding with `--dry-run`. `any generate PalletType` and `any generate ReceiptStatus` are now refused, naming `Any.Enum<…>()` (#298). `BinLocation` now reads `rack  int  Any.Int32().Positive().LessThanOrEqualTo(99)  guard` and marks `level  int  Any.Int32()  to verify, unread guards` (#297). The three enum generators (including `AnyStorageZone.cs`), the replayed `level` recipe and the matching tests are removed.
