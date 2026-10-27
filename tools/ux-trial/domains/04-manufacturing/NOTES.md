# 04 — Manufacturing (MES work order)

## Domain model (`Domain/`)

| Type | Guard shape |
|---|---|
| `BatchCode(string)` — `PPP-YYWW-NNNN`, ISO week 01–53 | `ArgumentException.ThrowIfNullOrWhiteSpace`, then `Match match = Format.Match(value); if (!match.Success) throw` |
| `Cadence(int unitsPerHour)` — 1..10 000 | `ArgumentOutOfRangeException.ThrowIfNegativeOrZero` + `ThrowIfGreaterThan(x, const)` |
| `CycleTime(TimeSpan)` — 1 s..1 h, whole seconds | two `if (< static readonly)` guards + `Ticks % TicksPerSecond != 0` |
| `Tolerance(nominal, minus, plus)` (decimal mm) | delegated to `private static Validate(...)`; cross-field (`minus < nominal`, band not empty), `Scale > 3` |
| `Machine(string machineId, MachineState)` | `Regex.IsMatch(id ?? "", @"^M-\d{3}$")`; switch expression throwing for non-commissionable states; `Fire(MachineEvent)` state machine |
| `ShiftWindow(DateTime start, DateTime end)` | cross-field `start >= end`, `end - start > 12 h` |
| `WorkOrder(BatchCode, sku, plannedQuantity, Cadence, Tolerance, ShiftWindow, IReadOnlyList<string> operators)` | `ThrowIfNull`, `ThrowIfNullOrEmpty`, `Length > 20`, `plannedQuantity is < 1 or > 100_000`, `Count == 0` |
| `MachineState`, `MachineEvent`, `ShiftKind` (members numbered from 1) | enums |

Behaviour: estimated duration (rounded up to the minute), fits-in-window, good/scrap recording against
the tolerance on a running machine, first-pass yield, completion.

## CLI recaps (verbatim)

```
$ any generate BatchCode Cadence CycleTime Tolerance Machine ShiftWindow
Analyzing Manufacturing.Domain.BatchCode
  constructor BatchCode(string)

  value  string  Any.String().NotBlank()  guard

✓ AnyBatchCode.cs — 1 of 1 parameters inferred.

Analyzing Manufacturing.Domain.Cadence
  constructor Cadence(int)

  unitsPerHour  int  Any.Int32().Positive().LessThanOrEqualTo(10000)  guard

✓ AnyCadence.cs — 1 of 1 parameters inferred.

Analyzing Manufacturing.Domain.CycleTime
  constructor CycleTime(TimeSpan)

  duration  TimeSpan  Any.TimeSpan()  to verify, unread guards

✓ AnyCycleTime.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Manufacturing.Domain.Tolerance
  constructor Tolerance(decimal, decimal, decimal)

  nominalMm  decimal  Any.Decimal()  to verify, unread guards
  minusMm    decimal  Any.Decimal()  to verify, unread guards
  plusMm     decimal  Any.Decimal()  to verify, unread guards

✓ AnyTolerance.cs — 3 of 3 parameters inferred, 3 to verify.
  The file will not compile until you resolve it. That is deliberate.

✗ Machine: several types answer to that name.
  Manufacturing.Domain.Machine
  System.Reflection.PortableExecutable.Machine
  Name the one you mean in full.

Analyzing Manufacturing.Domain.ShiftWindow
  constructor ShiftWindow(DateTime, DateTime)

  start  DateTime  Any.DateTime()  to verify, unread guards
  end    DateTime  Any.DateTime()  to verify, unread guards

✓ AnyShiftWindow.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.
exit=1

$ any generate Manufacturing.Domain.Machine --entry-point static:Arbitrary
  machineId     string        Any.String().NonEmpty()   to verify, unread guards
  initialState  MachineState  Any.Enum<MachineState>()  to verify, unread guards
✓ AnyMachine.cs — 2 of 2 parameters inferred, 2 to verify.
✓ AnyMachine.Entry.cs — entry point Arbitrary.Machine()

$ any generate WorkOrder --entry-point any
  batch            BatchCode              new AnyBatchCode()                              AnyX
  productSku       string                 Any.String().NonEmpty().WithMaxLength(20)       guard
  plannedQuantity  int                    Any.Int32()                                     to verify, unread guards
  cadence          Cadence                new AnyCadence()                                AnyX
  tolerance        Tolerance              new AnyTolerance()                              AnyX
  window           ShiftWindow            new AnyShiftWindow()                            AnyX
  operators        IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty()).NonEmpty()  guard
✓ AnyWorkOrder.cs — 7 of 7 parameters inferred, 1 to verify.
✓ AnyWorkOrder.Entry.cs — entry point Any.WorkOrder()

$ any generate MachineState          (same for ShiftKind)
Analyzing Manufacturing.Domain.MachineState
  constructor MachineState()

✓ AnyMachineState.cs — no constructor parameters to infer.

$ any generate Cadence               -> ✗ ... AnyCadence.cs already exists. Re-run with --force ... exit=1
$ any generate Cadence --force --entry-point any   -> rewritten, plus AnyCadence.Entry.cs
$ any generate WorkOrder Tolerance --format json --dry-run   -> one JSON document, files[].text holds the code
```

## What I fixed by hand

* `AnyBatchCode` — **not flagged**, yet wrong: replaced `Any.String().NotBlank()` by `Any.StringMatching(...)`,
  which also meant changing `AnyValidValue()`'s return type and `WithValue(Func<AnyString, AnyString>)` to `AnyPattern`.
* `AnyMachine` — `StringMatching(@"M-\d{3}")` (same `AnyString` -> `AnyPattern` signature change) and
  `Enum<MachineState>().OneOf(Idle, Stopped, Maintenance)`.
* `AnyCycleTime` — `Between(1 s, 1 h).WithGranularity(1 s)`.
* `AnyTolerance` — boxes chosen so that cross-field rules hold by construction: nominal `[1, 5000]`, minus `[0, 0.5]`, plus `[0.001, 0.5]`, all `WithScale(3)`.
* `AnyShiftWindow` — restructured: two independent recipes cannot say `start < end <= start + 12 h`; the generator now draws a start and a length (`WithLength` replaces `WithEnd`).
* `AnyWorkOrder` — `plannedQuantity` -> `Between(1, 100_000)`.
* `AnyMachineState` — `new MachineState()` replaced by `Any.Enum<MachineState>().Generate()`. `AnyShiftKind` left as emitted to demonstrate the bug.

## Findings

1. **High — CLI silently misses a guard on a local derived from the parameter.** `Match m = Regex.Match(value, ...); if (!m.Success) throw`, `bool ok = value.StartsWith("LOT-"); if (!ok) throw`, `int parsed = int.Parse(week); if (parsed < 1 ...) throw` — all recap as plain inferred (no `to verify`), the file compiles, and draws invalid values. Same guard written `if (!Regex.IsMatch(value, ...))` is correctly flagged. Test: `Bug_Scaffolded_AnyBatchCode_recipe_misses_the_format_guard_silently`.
2. **High — `any generate <Enum>` emits `return new TEnum();`.** Recap: "no constructor parameters to infer", ✓. It always draws `default`; for `ShiftKind` (members from 1) that is the undeclared `(ShiftKind)0`. Test: `Bug_Scaffolded_AnyShiftKind_draws_an_undeclared_member`.
3. Medium — `AnyGenerationException` quotes the drawn value raw: NUL, `\f`, `\r` reach the console unrendered, so the reported value cannot be read or copied (and `grep` called the test output a binary file).
4. Medium (doc) — no sample shows `using JustDummies.Xunit;`; `[Reproducible]` first gives CS0246.
5. Medium (doc) — `Any.Boolean().Except(true, false)` is documented twice; `AnyBoolean` has no `Except` (CS1061).
6. Medium (friction) — moving a scaffolded string to `Any.StringMatching` means editing two signatures (`AnyString` -> `AnyPattern`); cross-parameter invariants need a hand restructure of the generator.
7. Low — `plannedQuantity is < 1 or > 100_000` (relational pattern) is not read; honestly flagged `to verify`.
8. Low — a drawn `DateTime` is always `Kind = Utc`, even between `Local` bounds (ticks stay within the local bounds). Undocumented.
9. Low — JD014 and JD017 both fire on the same `Any.Enum<ShiftKind>().OneOf((ShiftKind)0)` argument.
10. Low — the constant-conflict rules (JD015/16/17/23) stay silent inside `Assert.Throws(() => ...)` — sensible, but undocumented; the same chains outside it fire (`AnalyzerFiringProbes.cs`).
11. Low — the scaffolder's own `Any.ListOf(Any.String().NonEmpty())` raises JD030 (info).
12. Low — enum except-all and distinct-cardinality conflicts surface at `Generate()`, bounds/granularity conflicts at declaration.
13. Low (missing capability, clear refusal) — `Any.TimeOnly().Between(22:00, 06:00)` for a night shift is refused; workaround: start + `TimeSpan` offset.

Analyzer diagnostics seen in the build: JD006 (warning, correct), JD030 (info x4), JD031 (info, correct), and in
`AnalyzerFiringProbes.cs` JD014, JD015, JD016, JD017, JD023 (all correct). No rule for a drawn value reaching an
assertion (expected: none documented). JD028 does not fire on `Any.ListOf(new AnyMachine()).Distinct()` (documented: only provably-new projections).

## Results

68 tests: 66 pass, 2 fail (the two `Bug_` tests above).

## Fluency: 4 / 5

Writing the behaviour tests was pleasant: `Any.WorkOrder().WithPlannedQuantity(1_200).WithCadence(new Cadence(600))`
reads exactly as the test means, `WithWindow(w => w.WithLength(...))` refines without restating rules, and
`[Reproducible]` plus the seed in the exception message made replay trivial. Conflict messages are excellent.
It loses a point for the scaffolder: one silently wrong recipe (BatchCode), a broken enum scaffold, and the
manual restructuring needed for regex formats and cross-field invariants.
