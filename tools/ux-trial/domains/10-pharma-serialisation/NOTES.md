# 10 — Pharmacy / traceability: pack serialisation

## Domain model (`Domain/`)

| Type | Kind | Guards (shape) |
| --- | --- | --- |
| `Gtin` | value object | `ThrowIfNull`, `^\d{14}$` regex, GS1 mod-10 check digit — all in a private `Validate` |
| `BatchNumber` | value object | `ArgumentException.ThrowIfNullOrWhiteSpace`, `Length > 20`, `^[A-Z0-9-]+$` |
| `SerialNumber` | value object | a `switch` expression: `null`, `{ Length: < 8 or > 20 }`, `_ when !Alphabet.IsMatch` |
| `ReimbursementPrice` | value object | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(amount, 100_000m)`, `decimal.Round(amount, 2) != amount` |
| `ShelfLife` | value object | relational pattern `months is < 1 or > 60` |
| `ExpiryMonth` | value object | private ctor, `static Of(year, month)` with four `ThrowIfLessThan/GreaterThan` |
| `SerialisedPack` | entity | `?? throw` null checks; status transition `Decommission` |
| `Batch` | aggregate | cross-field `expiresOn <= manufacturedOn`, `ThrowIfNegativeOrZero` / `ThrowIfGreaterThan(500_000)` |
| `Shipment` | aggregate | combined `reference is null \|\| !Regex.IsMatch(^SHP-\d{8}$)`, non-empty list, `Distinct().Count() != Count` |
| `RecallNotice` | entity | `ThrowIfLessThan(effectiveFrom, issuedOn)` (cross-parameter), `Enum.IsDefined`, `ThrowIfNullOrEmpty` + `Length > 500` |
| `PackStatus`, `VerificationOutcome`, `RecallClass` | enums | |

Behaviour: commissioning serials with uniqueness and a planned-quantity cap, verification
(authentic / unknown / expired / already decommissioned), dispensing (state transition), recall,
remaining shelf-life ratio, active stock value, GS1 element string, check-digit computation,
month-end expiry, shipment suspicion check against a batch.

## CLI recaps (verbatim)

`any generate Gtin --dry-run` then
`any generate Gtin BatchNumber SerialNumber ReimbursementPrice SerialisedPack Batch Shipment --entry-point static:Arbitrary`:

```text
  value  string  Any.String().NonEmpty()  to verify, unread guards                                   (Gtin)
  value  string  Any.String().NotBlank().WithMaxLength(20)  to verify, guard, unread guards          (BatchNumber)
  value  string  Any.String().NonEmpty()  to verify, unread guards                                   (SerialNumber)
  amount  decimal  Any.Decimal().Positive().LessThanOrEqualTo(100000m)  to verify, guard, unread guards

  gtin       Gtin          new AnyGtin()          AnyX                                               (SerialisedPack)
  batch      BatchNumber   new AnyBatchNumber()   AnyX
  expiresOn  DateOnly      Any.DateOnly()
  serial     SerialNumber  new AnySerialNumber()  AnyX
✓ AnySerialisedPack.cs — 4 of 4 parameters inferred.

  gtin             Gtin                new AnyGtin()                                     AnyX       (Batch)
  number           BatchNumber         new AnyBatchNumber()                              AnyX
  manufacturedOn   DateOnly            Any.DateOnly()                                    to verify, unread guards
  expiresOn        DateOnly            Any.DateOnly()                                    to verify, unread guards
  plannedQuantity  int                 Any.Int32().Positive().LessThanOrEqualTo(500000)  guard
  price            ReimbursementPrice  new AnyReimbursementPrice()                       AnyX
✓ AnyBatch.cs — 6 of 6 parameters inferred, 2 to verify.

  reference  string                       Any.String().NonEmpty()                       to verify, unread guards        (Shipment)
  serials    IReadOnlyList<SerialNumber>  Any.ListOf(new AnySerialNumber()).NonEmpty()  to verify, guard, unread guards
  shippedOn  DateOnly                     Any.DateOnly()
```

`any generate ShelfLife RecallNotice --entry-point static:Arbitrary`:

```text
  months  int  Any.Int32()  to verify, unread guards

  batch          BatchNumber  new AnyBatchNumber()                        AnyX
  issuedOn       DateOnly     Any.DateOnly()                              to verify, unread guards
  effectiveFrom  DateOnly     Any.DateOnly()                              to verify, unread guards
  recallClass    RecallClass  Any.Enum<RecallClass>()
  reason         string       Any.String().NonEmpty().WithMaxLength(500)  guard
```

`any generate ExpiryMonth`:

```text
✗ ExpiryMonth: nothing here constructs it.
  Generate() needs a public instance constructor whose parameters are all passed by value,
  or one public static Create/From/Of/Parse taking a single argument and returning the type.
```

Also tried: a re-run without `--force` (refused, exit 1, "Whatever you changed in it is lost"),
`--force` (rewrote the file, reintroducing the `TODO_verify` line I had resolved; restored by hand),
`--entry-point any --dry-run` (C# 14 `extension(Any)` file), `--format json --dry-run` on two types
(one document, `requiresVerification` per row, summary `parametersToVerify: 3`).

Every recap was honest: each guard it could not read was flagged `to verify`, every guard it did
read was turned into the right constraint, and the only build errors were the deliberate
`TODO_verify_the_generator_for_…` lines. All 10 generators (9 scaffolded + 1 hand-written) pass the
500-draw harness in `ScaffoldedGeneratorTests.cs`.

## What I fixed by hand

* `AnyGtin` — a check digit is not expressible by constraints. Replaced the base with
  `Any.StringMatching(@"\d{13}").As(body => Gtin.FromBody(body).Value)`, which required changing the
  return type of `AnyValidValue()` from `AnyString` to `IAny<string>` and **deleting** the
  `WithValue(Func<AnyString, AnyString>)` overload.
* `AnyBatchNumber` — added `.WithChars("A…Z0…9-")`.
* `AnySerialNumber` — `Any.String().AlphaNumeric().WithLengthBetween(8, 20)`.
* `AnyReimbursementPrice` — added `.WithScale(2)`.
* `AnyShipment` — reference `StartingWith("SHP-").Numeric().WithLength(12)`; serials `+ .Distinct()`.
* `AnyBatch`, `AnyRecallNotice` — cross-field date rules expressed as two **disjoint** ranges
  (manufacture 2020–2024, expiry 2025–2030), since parameters are drawn independently.
* `AnyShelfLife` — `Between(1, 60)`.
* `ExpiryMonth` — refused by `any`; wrote `Arbitrary.ExpiryMonth()` with `Any.Combine(..., ExpiryMonth.Of)`.

## Findings

1. **friction / medium** — a to-verify base that has to be a pattern or a computed value does not fit
   the scaffolded shape (`AnyValidValue()` returns `AnyString`, the refine overload takes
   `Func<AnyString, AnyString>`). See `Tests/AnyGtin.cs`.
2. **friction / medium** — cross-parameter invariants (expiry after manufacture, effect after issue)
   have no place in the generated shape; the disjoint-range workaround breaks as soon as one side is
   pinned outside its range (`Arbitrary.Batch().WithManufacturedOn(new DateOnly(2031, 1, 1))` always throws).
3. **friction / low** — `--force` throws away every hand-resolved `AnyValid…()` (documented, but the
   docs also present `AnyValid…()` as *the* place for the rules the tool could not read).
4. **cli / low** — guard shapes not read (all flagged honestly): relational pattern `is < 1 or > 60`,
   `switch` property pattern `{ Length: < 8 or > 20 }`, `ThrowIfLessThan(a, b)` between two parameters,
   `Distinct().Count() != Count`, `decimal.Round(x, 2) != x`.
5. **missing-capability / low** — two-argument static factory (`ExpiryMonth.Of(year, month)`) is
   refused; the refusal is clear.
6. **doc / low** — the distinct-cardinality gate: `Any.Char().Numeric()` × 11 is a
   `ConflictingAnyConstraintException`, but `Any.String().Numeric().WithLength(1)` and
   `Any.StringMatching(@"\d")` × 11 (same 10 values) are an `AnyGenerationException` after the draw
   budget, and even the gated case throws at `Generate()`, not at declaration. Both clear and bounded.
7. **analyzer / low** — JD016 fires for 3 distinct `Any.Boolean()` but not for 11 distinct
   `Any.Char().Numeric()`.
8. **doc / low** — the regex escape table omits `\xNN` / `\uNNNN`, which work (GS1 group separator `\x1D`).
9. **doc / low** — unsupported-regex messages end with a fixed list ("lookarounds, backreferences,
   word boundaries and Unicode categories") even when the construct is a late `(?i)` or a class subtraction.
10. **doc / low** — `collections.en.md` cites ADR-0029 for the million cap, while the run-time message
    ("A declared bound steers the draw") and `strings.en.md` follow ADR-0076.
11. **missing-capability / low** — no analyzer flags an `any…` value reaching an assertion
    (`Tests/AnalyzerProbes.cs`); none is documented either.

No bug found: every drawn value satisfied its constraints, every contradiction I wrote failed fast
naming both sides, and every seed replayed (scope, `[Reproducible]`, and even a seed reported by a
failure outside any scope).

## Analyzer diagnostics seen (all in `AnalyzerProbes.cs`, deliberate)

JD015 (prefix vs length, also inside a lambda), JD023 (crossed int bounds), JD017 (all enum members
excluded), JD006 (discarded generator), JD032 (bound twice), JD028 (Distinct over reference equality,
only via `Any.Combine` — not via `new AnySerialisedPack()`, as documented). Not fired: decimal scale
lattice and crossed `DateOnly` bounds (outside the documented scope), 11 distinct numeric chars (JD016
miss), drawn value in an assertion (no such rule). Info-level rules never show in `dotnet build`.
No diagnostic fired on the domain tests or the scaffolded files.

## Fluency: 4 / 5

Writing the tests read naturally: `Arbitrary.Batch().WithPlannedQuantity(q => q.GreaterThanOrEqualTo(3))`,
`WithRecallClass(c => c.DifferentFrom(RecallClass.ClassI))`, `Any.DateOnly().Between(batch.ManufacturedOn, batch.ExpiresOn)`
and `Any.ListOf(...).WithCount(3).Distinct()` say exactly what a test needs. Every guess I made at
an API name was right. What it costs a point: the GTIN check digit and the two cross-field date rules
needed hand edits of the generated signatures or arbitrary disjoint ranges, and `--force` erases those edits.
