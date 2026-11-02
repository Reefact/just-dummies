# Payroll — domain model

Namespace `Payroll.Domain` (`Domain/`). Employment contracts and the monthly payslip computed from hours worked, bonuses and deductions.

## Value objects
- `EmployeeId(string value)` — `null` refused (`ArgumentNullException`); `^EMP-\d{6}$` (`ArgumentException` otherwise). Equality by value.
- `HourlyRate(decimal value)` — (0, 500] euros (`HourlyRate.Ceiling` = 500; `ArgumentOutOfRangeException`), and in whole cents, i.e. `Round(value, 2) == value` (`ArgumentException`).
- `PayPeriod(int year, int month)` — year in [2000, 2100], month in [1, 12] (`ArgumentOutOfRangeException`). `FirstDay`, `LastDay` (last day of the month).
- `PayslipLine(string label, LineKind kind, decimal amount)` — label not null/blank (`ArgumentNullException`/`ArgumentException`), length ≤ 40
  (`PayslipLine.MaximumLabelLength`, `ArgumentException`); amount > 0 (`ArgumentOutOfRangeException`) and in whole cents (`ArgumentException`); `kind` is not checked at construction.
  `SignedAmount` = +amount for `Bonus`, −amount for `Deduction`, `InvalidOperationException` for an undefined kind.
- `ContractType` (Permanent, FixedTerm, Apprenticeship, Internship), `LineKind` (Bonus, Deduction), `PayslipStatus` (Draft, Issued) — enums.

Pattern matching uses .NET `Regex.IsMatch` as written (`\d` is not restricted to ASCII digits; `$` also matches before a final line feed).

## Entities
- `EmploymentContract(employee, type, hiredOn, endsOn, rate, weeklyHours)` — checks run in this order, before the null checks on `employee` and `rate`
  (which throw `ArgumentNullException` last):
  1. `type` defined (`ArgumentOutOfRangeException`);
  2. `hiredOn >= 1970-01-01` (`EarliestHiringDate`, `ArgumentOutOfRangeException`);
  3. weekly hours in [1, max] with max = 35 for `Apprenticeship` and `Internship`, 48 for the others (`ArgumentOutOfRangeException`);
  4. `Permanent` ⇒ `endsOn` must be null; every other type ⇒ `endsOn` required (`ArgumentException`);
  5. when present, `endsOn > hiredOn` (`ArgumentException`).
  `MonthlyContractualHours` = `Round(weeklyHours × 52 / 12, 2)` (banker's rounding). `CompletedYearsOfService(asOf)` = whole anniversaries elapsed, never below 0.
- `Payslip(contract, period, hoursWorked, lines)` — aggregate for one month; contract, period and lines non-null; hours in [0, 300] (`ArgumentOutOfRangeException`);
  `period.LastDay >= contract.HiredOn` (`ArgumentException`). Copies `lines` (elements not null-checked); starts `Draft`. No check against the contract's end date.
  - Derived: `OvertimeHours` = max(0, hours − contractual hours); `BasePay` = `Round((hours − overtime) × rate, 2)` half away from zero; `OvertimePay` =
    `Round(overtime × rate × 1.25, 2)` half away from zero (`Payslip.OvertimePremium`); `TotalBonuses`, `TotalDeductions` sums by kind; `GrossPay` = base + overtime + bonuses;
    `NetPay` = `max(0, GrossPay − TotalDeductions)`.
  - `AddLine(PayslipLine)` — null refused; refused with `InvalidOperationException` once `Issued`. `Issue()` — Draft → Issued; refused if already issued (`InvalidOperationException`).
- `SeniorityBonusPolicy` (static) — `RateFor(contract, asOf)`: 0 for `Apprenticeship`/`Internship`; otherwise by completed years: ≥ 10 → 0.06, ≥ 3 → 0.03, else 0.
  `BonusLineFor(payslip)`: rate evaluated at `Period.LastDay`; amount = `Round(BasePay × rate, 2)` half away from zero; returns a `PayslipLine("Seniority bonus", Bonus, amount)`
  or `null` when the amount is 0. The line is not added to the payslip.
