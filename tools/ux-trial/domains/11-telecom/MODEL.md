# Telecom — domain model

Namespace `Telecom.Domain` (`Domain/`). A subscriber's mobile line: number, paired handset, plan, metered usage and monthly billing.

## Value objects
- `Msisdn(string digits)` — E.164 digits `^[1-9][0-9]{7,14}$` (country code, no leading zero, 8 to 15 digits); refused with `ArgumentException`
  (`null` throws `ArgumentNullException` from the regex call). `IsFrenchMobile` = starts with `336` or `337`. Equality by digits; `ToString()` = `+` + digits.
- `Imei(string value)` — `null` refused (`ArgumentNullException`); exactly 15 ASCII digits; the 15th digit must equal the Luhn check digit of the first 14
  (`ArgumentException` for length/characters and for a failing check). `TypeAllocationCode` = first 8 digits. Static `LuhnCheckDigit(body)`:
  doubles every second digit from the right (starting with the rightmost digit of `body`), result `(10 - sum % 10) % 10`. Equality by value.
- `Iccid(string value)` — `^89[0-9]{17,18}$` (prefix `89`, then 17 or 18 digits, so 19 or 20 digits in all); `ArgumentException` otherwise
  (`null` throws `ArgumentNullException` from the regex call). `CountryCode` = characters 2..3.
- `BillingPeriod(DateOnly start, DateOnly end)` — start inclusive, end exclusive; `start < end` (`ArgumentException`) and at most 31 days
  between them (`ArgumentException`). `Contains(DateTime)` compares the date part of the instant (`DateOnly.FromDateTime`, kind ignored) with `[Start, End)`.
- `DataBundle(int megabytes, decimal price, TimeSpan validity)` — megabytes in [100, 100 000] (`ArgumentOutOfRangeException`), then a multiple of 100
  (`ArgumentException`); price in [0.01, 500] (`ArgumentOutOfRangeException`; no scale check despite the "cents" remark); validity ≥ 1 day
  (`ArgumentOutOfRangeException`, no upper bound). `PricePerGigabyte` = `round(Price * 1024 / Megabytes, 2)`.
- `Plan(string name, PlanTier tier, decimal monthlyFee, int dataAllowanceGb, decimal overagePerGb)` — name not null/blank
  (`ArgumentNullException`/`ArgumentException`), length ≤ 40 (`ArgumentOutOfRangeException`, name kept as given); monthly fee ≥ 0 (`ArgumentOutOfRangeException`)
  and in whole cents (`ArgumentException`); allowance > 0 GB (`ArgumentOutOfRangeException`); overage per GB ≥ 0 (`ArgumentOutOfRangeException`, no scale check);
  tier must be a defined `PlanTier` (`ArgumentOutOfRangeException`, checked last).
  `OverageFor(long usedMegabytes)`: 0 for `Unlimited`; 0 when usage ≤ allowance × 1024 MB; otherwise every *started* gigabyte beyond the allowance × `OveragePerGb`.
- `UsageRecord(DateTime occurredAt, UsageKind kind, long quantity)` — `occurredAt.Kind` must be `Utc` (`ArgumentException`); quantity > 0
  (`ArgumentOutOfRangeException`); an `Sms` record must have quantity exactly 1 (`ArgumentException`). Quantity is megabytes for data, seconds for voice, 1 for SMS.
  `kind` is not checked against the enum's defined values.

Pattern matching uses .NET `Regex.IsMatch` as written: a string ending in a final line feed also satisfies `$`.

## Entity
- `MobileLine(number, handset, plan, activatedOn)` — the three references must be non-null (`ArgumentNullException`); `activatedOn.Year >= 2000`
  (`ArgumentOutOfRangeException`). Starts `Active`; `Number` and `ActivatedOn` never change. `Usage` is an append-only read-only list.
  - `Record(UsageRecord)` — null refused; only on an `Active` line (`InvalidOperationException` for `Suspended`/`Terminated`). No date check against `ActivatedOn`.
  - `Suspend()` — only from `Active`; `Resume()` — only from `Suspended`; `Terminate()` — from `Active` or `Suspended`, refused when already `Terminated`
    (all `InvalidOperationException`).
  - `SwapHandset(Imei)` — null refused; refused with `InvalidOperationException` when equal to the current handset; allowed in any status.
  - `ChangePlan(Plan)` — null refused; allowed in any status, no other check.
  - `BillFor(BillingPeriod)` — `Plan.MonthlyFee` + `Plan.OverageFor(sum of Data quantities whose OccurredAt falls in the period)`; only the current plan is used;
    voice and SMS are not billed; status is not consulted (a suspended or terminated line is still billed the fee).

## Enums
`LineStatus` (Active, Suspended, Terminated), `PlanTier` (Prepaid, Postpaid, Unlimited), `UsageKind` (Data, Voice, Sms).
