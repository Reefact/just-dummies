# 13 — HR / payroll

## Domain model (`Domain/`)

| Type | Kind | Guards (shape) |
|---|---|---|
| `ContractType` | enum | Permanent, FixedTerm, Apprenticeship, Internship |
| `LineKind`, `PayslipStatus` | enums | |
| `EmployeeId` | value object | null check + compiled `Regex` `^EMP-\d{6}$` |
| `HourlyRate` | value object | `ThrowIfNegativeOrZero`, `ThrowIfGreaterThan(value, Ceiling)` (const 500), `decimal.Round(value, 2) != value` |
| `PayPeriod` | value object | `year < 2000 \|\| year > 2100`, `month is < 1 or > 12` |
| `PayslipLine` | value object | `ThrowIfNullOrWhiteSpace`, length `> 40`, `ThrowIfNegativeOrZero`, scale check; `SignedAmount` switch expression |
| `EmploymentContract` | entity | everything in a private `Validate(...)`: `Enum.IsDefined`, `hiredOn < EarliestHiringDate` (static readonly), max weekly hours from a switch on the type (35 for trainees, 48 otherwise), end date required unless permanent, forbidden if permanent, `end <= hiredOn` |
| `Payslip` | aggregate | `ThrowIfNull` ×3, `ThrowIfNegative`/`ThrowIfGreaterThan(300m)` on hours, cross-object `period.LastDay < contract.HiredOn` |
| `SeniorityBonusPolicy` | static policy | 0 % for trainees, 3 % from 3 years, 6 % from 10 years; `BonusLineFor(payslip)` |

Behaviour: base pay up to the contractual month (weekly × 52 / 12), overtime at 125 %, gross = base + overtime +
bonuses, net = gross − deductions floored at zero, Draft → Issued (an issued payslip refuses lines).

## CLI recaps (verbatim)

```
$ any generate EmployeeId
Analyzing Payroll.Domain.EmployeeId
  constructor EmployeeId(string)

  value  string  Any.String().NonEmpty()  to verify, unread guards

✓ AnyEmployeeId.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

$ any generate HourlyRate PayPeriod
Analyzing Payroll.Domain.HourlyRate
  constructor HourlyRate(decimal)

  value  decimal  Any.Decimal().Positive().LessThanOrEqualTo(500m)  to verify, guard, unread guards

✓ AnyHourlyRate.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Payroll.Domain.PayPeriod
  constructor PayPeriod(int, int)

  year   int  Any.Int32()  to verify, unread guards
  month  int  Any.Int32()  to verify, unread guards

✓ AnyPayPeriod.cs — 2 of 2 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.

$ any generate PayslipLine EmploymentContract Payslip
Analyzing Payroll.Domain.PayslipLine
  constructor PayslipLine(string, LineKind, decimal)

  label   string    Any.String().NotBlank().WithMaxLength(40)  guard
  kind    LineKind  Any.Enum<LineKind>()
  amount  decimal   Any.Decimal().Positive()                   to verify, guard, unread guards

✓ AnyPayslipLine.cs — 3 of 3 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Payroll.Domain.EmploymentContract
  constructor EmploymentContract(EmployeeId, ContractType, DateOnly, DateOnly?, HourlyRate, int)

  employee     EmployeeId    new AnyEmployeeId()          AnyX
  type         ContractType  Any.Enum<ContractType>()     to verify, unread guards
  hiredOn      DateOnly      Any.DateOnly()               to verify, unread guards
  endsOn       DateOnly?     Any.DateOnly().AsNullable()  to verify, unread guards
  rate         HourlyRate    new AnyHourlyRate()          AnyX
  weeklyHours  int           Any.Int32()                  to verify, unread guards

✓ AnyEmploymentContract.cs — 6 of 6 parameters inferred, 4 to verify.
  The file will not compile until you resolve it. That is deliberate.

Analyzing Payroll.Domain.Payslip
  constructor Payslip(EmploymentContract, PayPeriod, decimal, IReadOnlyList<PayslipLine>)

  contract     EmploymentContract          new AnyEmploymentContract()       to verify, AnyX, unread guards
  period       PayPeriod                   new AnyPayPeriod()                to verify, AnyX, unread guards
  hoursWorked  decimal                     Any.Decimal().Between(0m, 300m)   guard
  lines        IReadOnlyList<PayslipLine>  Any.ListOf(new AnyPayslipLine())

✓ AnyPayslip.cs — 4 of 4 parameters inferred, 2 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

Also run: `--dry-run` (PayslipLine), `--format json` with `--entry-point static:Arbitrary --force` (HourlyRate,
PayPeriod: valid JSON, empty stderr), `--entry-point any --force` on the other four, a re-run without `--force`
(`✗ …AnyPayPeriod.cs already exists. Re-run with --force…`, exit 1), an unknown type (exit 1, clear), and the static
policy class (`✗ SeniorityBonusPolicy: nothing here constructs it.`, exit 1, clear).

The first build of the scaffold failed only on the 11 designed `TODO_verify_…` lines — nothing else.

### Guard shapes, isolated (throw-away probe types, `--dry-run`)

| Guard | Recap |
|---|---|
| `if (month < 1) throw; if (month > 12) throw;` | `Any.Int32().Positive().LessThanOrEqualTo(12)  guard` |
| `if (year < 2000 \|\| year > 2100)` | to verify, unread guards (same root as K2) |
| `if (month is < 1 or > 12)` | to verify, unread guards — **new shape** |
| `hiredOn < new DateOnly(1970,1,1)`, `< StaticField`, `ThrowIfLessThan(hiredOn, new DateOnly(...))` | all three to verify, unread guards |
| `!Enum.IsDefined(type)` | `Any.Enum<ContractType>()`, nothing to verify (correct: the enum generator already guarantees it) |
| `decimal.Round(amount, 2) != amount` | to verify, unread guards |
| `end <= start` (cross-field) | both parameters to verify |

Every recap was honest: no guard was misread, and every parameter whose guard was not read was flagged.

## What I fixed by hand

* `AnyEmployeeId`: `Any.StringMatching(@"^EMP-\d{6}$")` — which changed the return type of `AnyValidValue()` to
  `AnyPattern` **and** forced me to rewrite the generated `WithValue(Func<AnyString, AnyString>)` signature.
* `AnyHourlyRate`, `AnyPayslipLine`: added `.WithScale(2)`.
* `AnyPayPeriod`: `Between(2000, 2100)`, `Between(1, 12)`.
* `AnyEmploymentContract`: `hiredOn` bounded to 1970..2100, `weeklyHours` `Between(1, 35)` (what every type accepts),
  and **`Generate()` rewritten** to reconcile `endsOn` with the type and the hiring date — no per-parameter base can
  express "null if permanent, after `hiredOn` otherwise".
* `AnyPayslip`: `AnyValidContract()` = `new AnyEmploymentContract().WithHiredOn(h => h.Before(2000-01-01))`, so every
  drawn period follows the hiring date. A test that *pinned* a contract (`WithContract(contract)`) then failed on
  some seeds (`A payslip cannot cover a month before the hiring date`), because pinning skips that base; switching to
  the lambda form (`WithContract(c => c.WithWeeklyHours(35)…)`) fixed it — exactly what the CLI page warns about, but
  I only understood it after a red run.

Harness: `ScaffoldedGeneratorTests` draws each of the six generators 500 times: all green, repeatedly.

## Findings

1. **Bug (medium, message)** — `Any.Decimal().Positive().Negative()` says *"Cannot apply Negative() because Positive()
   forbids 0, the only value Positive() and Negative() leave."* Same for `Negative().Positive()` and
   `Positive().Zero()`. Int32 says it correctly. `Positive().LessThan(-5m)` says Positive requires `>= 0`.
   → `Probes.Bug_Decimal_sign_conflict_message_claims_zero_is_left` (fails).
2. **Doc (medium)** — no user page and no package README shows `using JustDummies.Xunit;`. My first `[Reproducible]`
   gave `CS0246`.
3. **Friction / doc (medium)** — cross-parameter invariants (end date depends on type and hiring date; max hours on
   type; payslip period on contract hiring date) have no home in a scaffolded generator: `AnyValid…()` is per
   parameter, and the docs only cover single-parameter rules. I had to edit the generated `Generate()`.
4. **CLI (low)** — `month is < 1 or > 12` is not read (new shape); `year < 2000 || year > 2100` is K2's root. DateOnly
   guards are never read, even `ArgumentOutOfRangeException.ThrowIfLessThan(hiredOn, new DateOnly(1970, 1, 1))`.
   A scale check is not mapped to `WithScale`. All honestly flagged.
5. **Friction (low)** — replacing a `to verify` string recipe by `Any.StringMatching` changes `AnyString` →
   `AnyPattern`, so the generated `WithValue(Func<AnyString, AnyString>)` stops compiling.
6. **Doc (low)** — `WithScale`: the user page says it "fixes the number of decimal places"; IntelliSense says "a value
   lattice … not padded with trailing zeros". Observed: `WithScale(2)` always gives `decimal.Scale == 2` (2000/2000),
   while `WithScale(28)` over the default magnitude gives scale 22–23 and over `Between(0, 1)` 26–28. Neither text
   predicts both. (Diagnosed after reading `AnyDecimal.cs` XML doc.)
7. **Analyzer (low)** — not reported at build time although decidable from constants: decimal `Positive().Negative()`,
   decimal `Between(0.001m, 0.009m).WithScale(2)`, `DateOnly After(2026-01-01).Before(2025-01-01)`,
   `TimeOnly.Between(22:00, 06:00)`; `Distinct()` over a scaffolded `AnyPayslipLine` (no value equality) — JD028's
   documented limit. No rule for an `any` value reaching `Assert` (the getting-started page's main warning).
8. **Missing capability (low)** — a night shift `TimeOnly.Between(22:00, 06:00)` is refused clearly
   (`The start (22:00:00.0000000) must be at or before the end (06:00:00.0000000).`); no wrap-around form.

Analyzers seen: JD014 ×2, JD015, JD016, JD017, JD023 on `AnalyzerProbes.cs` (all correct, clear messages; JD014 even
says "the two arguments look transposed"); JD018 on an `Any.Reproducibly` inside a `[Reproducible]` class (correct,
helpful — moved that probe out); JD025 and JD014 (zero granularity) in a throw-away exploration file (correct).

Seed replay: a deliberately failing `[Reproducible]` test printed
`[JustDummies] These arbitrary values were seeded with 2079490112. Reproduce this run with [Reproducible(Seed = 2079490112)].`;
pinned, it drew `EMP-064670 01/04/1994 2013-7 166.87701182879446632864108834 2 71967.97` three times in a row.

## Fluency: 4 / 5

Writing the tests themselves was pleasant: `Any.Payslip().WithContract(c => c.WithWeeklyHours(35)).WithHoursWorked(100m)`
reads like the domain, collection expressions flow into `WithLines`, and conflict messages are mostly exact. Contradiction
handling, distinctness gating and seed replay all behaved as documented. One point off for the cross-field invariants
(two of my six generators needed hand-written logic in a "generated" method, and the pinned-vs-lambda trap cost a red
run), the missing `using JustDummies.Xunit;`, and the decimal conflict wording.
