# 11 — Telecom: a mobile line

## Domain model (`Domain/`)

| Type | Guards (shape) | Behaviour |
| --- | --- | --- |
| `Msisdn` | static `Regex` field `^[1-9][0-9]{7,14}$`, `if (!IsMatch) throw` | `IsFrenchMobile` (336/337), `ToString()` with `+` |
| `Imei` | delegated to a private `Validate`: `ThrowIfNull`, length/`char.IsAsciiDigit`, **Luhn check digit** | `TypeAllocationCode`, static `LuhnCheckDigit` |
| `Iccid` | inline `Regex.IsMatch(value, @"^89[0-9]{17,18}$")` | `CountryCode` |
| `Plan` | `ThrowIfNullOrWhiteSpace`, `ThrowIfGreaterThan(name.Length, 40)`, `ThrowIfNegative`, `decimal.Round(fee, 2) != fee`, `ThrowIfNegativeOrZero`, switch-expression guard on the tier enum | `OverageFor(mb)`: every started GB beyond the allowance, none on `Unlimited` |
| `DataBundle` | `megabytes < 100 \|\| megabytes > 100_000`, `megabytes % 100 != 0`, `ThrowIfLessThan/GreaterThan(price, …)`, `ThrowIfLessThan(validity, TimeSpan.FromDays(1))` | `PricePerGigabyte` |
| `BillingPeriod` | cross-field: `start >= end`, `end - start > 31 days` | `Contains(instant)` (end exclusive) |
| `UsageRecord` | `occurredAt.Kind != Utc`, `ThrowIfNegativeOrZero(quantity)`, cross-field `Sms ⇒ quantity == 1` | — |
| `MobileLine` (aggregate) | `ThrowIfNull` ×3, `activatedOn.Year < 2000` | state machine Active/Suspended/Terminated, `Record`, `SwapHandset`, `ChangePlan`, `BillFor(period)` |
| `PlanTier`, `LineStatus`, `UsageKind` | enums | |

## CLI recaps (verbatim)

```
$ any generate Msisdn
Analyzing Telecom.Domain.Msisdn
  constructor Msisdn(string)

  digits  string  Any.String().NonEmpty()  to verify, unread guards

✓ AnyMsisdn.cs — 1 of 1 parameters inferred, 1 to verify.
  The file will not compile until you resolve it. That is deliberate.
```

```
$ any generate Imei Plan BillingPeriod UsageRecord MobileLine --entry-point static:Arbitrary
Analyzing Telecom.Domain.Imei
  value  string  Any.String().NonEmpty()  to verify, unread guards
Analyzing Telecom.Domain.Plan
  name             string    Any.String().NotBlank()                 to verify, guard, unread guards
  tier             PlanTier  Any.Enum<PlanTier>()                    to verify, unread guards
  monthlyFee       decimal   Any.Decimal().GreaterThanOrEqualTo(0m)  to verify, guard, unread guards
  dataAllowanceGb  int       Any.Int32().Positive()                  guard
  overagePerGb     decimal   Any.Decimal().GreaterThanOrEqualTo(0m)  guard
Analyzing Telecom.Domain.BillingPeriod
  start  DateOnly  Any.DateOnly()  to verify, unread guards
  end    DateOnly  Any.DateOnly()  to verify, unread guards
Analyzing Telecom.Domain.UsageRecord
  occurredAt  DateTime   Any.DateTime()          to verify, unread guards
  kind        UsageKind  Any.Enum<UsageKind>()   to verify, unread guards
  quantity    long       Any.Int64().Positive()  to verify, guard, unread guards
Analyzing Telecom.Domain.MobileLine
  number       Msisdn    new AnyMsisdn()  AnyX
  handset      Imei      new AnyImei()    AnyX
  plan         Plan      new AnyPlan()    AnyX
  activatedOn  DateOnly  Any.DateOnly()   to verify, unread guards
(each followed by "✓ AnyX.cs — n of n parameters inferred, k to verify." and "✓ AnyX.Entry.cs — entry point Arbitrary.X()"; exit 0)
```

```
$ any generate Iccid --entry-point static:Arbitrary
  value  string  Any.String().NonEmpty()  to verify, unread guards

$ any generate DataBundle --entry-point static:Arbitrary
  megabytes  int       Any.Int32()                         to verify, unread guards
  price      decimal   Any.Decimal().Between(0.01m, 500m)  guard
  validity   TimeSpan  Any.TimeSpan()                      to verify, unread guards
```

Also run: `--dry-run` (file on stdout, nothing written), a re-run without `--force` (exit 1, "already exists.
Re-run with --force"), `--force --format json` (one clean JSON document, `parametersToVerify: 1`), and
`--entry-point any --dry-run` (C# 14 extension block, fine on net10.0).

**Verdict on the recaps: honest.** Every guard the tool did not read was flagged `to verify`; every row it
marked `guard` without `to verify` was right (`Positive()` for `ThrowIfNegativeOrZero`, `GreaterThanOrEqualTo(0m)`
for `ThrowIfNegative`, `Between(0.01m, 500m)` for the two `ThrowIfLessThan/GreaterThan`). No scaffolded recipe
drew a value its constructor rejects once the flagged lines were resolved (harness: 500 draws per generator).

## What I fixed by hand

* `AnyMsisdn`, `AnyIccid`: `Any.StringMatching(...)` — and had to change the lambda overload from
  `Func<AnyString, AnyString>` to `Func<AnyPattern, AnyPattern>`, because the base is no longer an `AnyString`.
* `AnyImei`: `Any.StringMatching(@"[0-9]{14}").As(body => body + Imei.LuhnCheckDigit(body))`; the base becomes an
  `IAny<string>`, so the lambda overload became `Func<IAny<string>, IAny<string>>` (of little use).
* `AnyPlan`: `NotBlank().WithMaxLength(40)`, `GreaterThanOrEqualTo(0m).WithScale(2)`, tier left as is.
* `AnyBillingPeriod`: reshaped — a start and a length in days (1–31) replace the independent end.
* `AnyUsageRecord`: `Generate()` forces quantity 1 for an SMS; `Any.DateTime()` is already `Kind=Utc`.
* `AnyMobileLine`: `AfterOrEqualTo(new DateOnly(2000, 1, 1))`. `AnyDataBundle`: `Between(100, 100_000).MultipleOf(100)`
  and `GreaterThanOrEqualTo(TimeSpan.FromDays(1))`.
* Every test file needed `using JustDummies.Xunit;` — no documentation sample shows it.

## Findings

1. **friction, medium (cli)**: replacing a `to verify` base with a pattern or `.As(...)` forces you to edit the
   `With…(Func<…>)` signature as well. I hit this three times (Msisdn, Imei, Iccid).
2. **doc, medium (xunit)**: `ReproducibleAttribute` is in `JustDummies.Xunit`; the docs never show the `using`, so
   the first build fails with CS0246.
3. **analyzer, medium**: JD007 and JD028 do not see draws through scaffolded or user-written `IAny<T>`s
   (`Arbitrary.Plan().Generate()` in a `[Reproducible]` constructor; `Any.ListOf(Arbitrary.Plan()).Distinct()`).
   Both fire on the built-in equivalents (`AnalyzerProbes.cs`).
4. **doc, medium (dates)**: a drawn `DateTime` is always `Kind=Utc`. That includes draws between `Local` bounds,
   while `OneOf` keeps the Kind. The user page does not say this; only the XML remark does. Under `TZ=Europe/Paris`,
   `Between(local 00:00, local 00:00:01)` returned `2025-01-01T00:00:00.46Z`, which is an hour after the bounds' instants.
5. **doc, low (numbers)**: the page says `WithScale` "fixes the number of decimal places". The XML doc says the value is
   "not padded with trailing zeros". With `WithScale(28)`, 179 of 2000 draws had `Scale < 28`. With `WithScale(2)`,
   all 2000 had `Scale == 2`.
6. **cli, low**: neither regex guard shape is read (a static `Regex` field, or an inline `Regex.IsMatch` with a literal).
   The maintainer spec says this is deliberate, but the user page does not. `unread guards` never says which guard.
7. **cli, low**: other shapes it did not read, each flagged correctly:
   * `x < 100 || x > 100_000` (probably the same root as K2)
   * `ThrowIfGreaterThan(name.Length, 40)`
   * `x % 100 != 0`
8. **friction, low (cli)**: cross-field guards (BillingPeriod, UsageRecord's SMS rule) cannot be expressed per
   parameter, so the generator had to be reshaped by hand. The `to verify` flag was honest.
9. **doc, low**: `Between(10, 1)` throws `ArgumentException`, which is not a `DummyException`. The errors page lists
   "bounds that cross" under `ConflictingAnyConstraintException`, so `catch (DummyException)` misses this case.
10. **doc, low**: the 65,536-character limit on pattern generation is not on the strings page. The refusal itself is clear.
11. **doc, low (scope edge)**: there is no check-digit constraint, and there should not be one: `.As(body => body + Luhn(body))`
    is clean. A one-line sample for this common case would help.

No `Bug_` test: I found no wrong value, no violated constraint and no failure to replay. Seed replay checked:
`Any.Reproducibly(seed)` twice gives the same values, and a reported seed replays the failed body exactly. A failing
`[Reproducible]` test printed `[JustDummies] These arbitrary values were seeded with -14870306. Reproduce this run with [Reproducible(Seed = -14870306)].`,
and pinning that seed reproduced `+19612588876`.

Harness note: `PublicAPI.Shipped.txt` is empty; the public surface is in `PublicAPI.Unshipped.txt`.

## Analyzer diagnostics seen

JD005 (error, generator interpolated), JD006, JD007 (built-in draw only), JD014 ×2, JD016 ×2, JD017, JD023,
JD025, JD028 (`.As` chain only). All were correct with clear messages. False negatives: see finding 3.

## Fluency: 4 / 5

The arrange blocks read the way the fundamentals want them to:
`Arbitrary.Plan().WithTier(t => t.DifferentFrom(PlanTier.Unlimited)).WithDataAllowanceGb(10)` says what is chosen
and what is arbitrary. Refusals named both sides, and regex refusals named the construct and its position. I lost
a point because resolving a `to verify` line often means rewriting more of the scaffold than the doc suggests
(signature changes, cross-field reshaping), and because of the missing `using` on first contact.
