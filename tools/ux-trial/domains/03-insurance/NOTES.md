# 03 — Insurance (car policy)

## Domain model (`Domain/`)

| Type | Guards (shape) |
|---|---|
| `PolicyNumber(string)` | `ThrowIfNull` + `Regex.IsMatch(^POL-\d{4}-[A-Z]{2}\d{6}$)` |
| `CoveragePeriod(DateOnly start, DateOnly end)` | cross-field `end <= start` and `end > start.AddYears(1)` (if/throw) |
| `Deductible(decimal)` | `ThrowIfNegative`, `ThrowIfGreaterThan(amount, Ceiling)`, whole-cents check `decimal.Round(amount, 2) != amount` |
| `BonusMalus(double)` | delegated to a private `Validate`: not NaN, 0.50..3.50 |
| `Claim(Guid, DateOnly, decimal, Responsibility)` | `id == Guid.Empty`, `ThrowIfNegativeOrZero(damage)`, switch expression with a throwing default arm |
| `Driver(string, DateOnly, int)` | `ThrowIfNullOrWhiteSpace`, `ThrowIfNegative`, `ThrowIfGreaterThan(years, 80)` |
| `CarPolicy(...9 params...)` | `ThrowIfNull` x6, relational pattern `basePremium is <= 0m or > 20_000m`, `foreach` claim must lie in the period |
| enums | `Responsibility`, `VehicleCategory`, `CoverageLevel` |

Behaviour: `AnnualPremium()` (coefficient, young-driver surcharge), `OwnDamagePayout(claim)` (level,
responsibility share, deductible), `DeclareClaim` (period check), `Renew()` (next period, bonus-malus
update, clean history), `BonusMalus.AfterYear` (bonus 5%, malus 25%/12.5%, floor/ceiling).

## CLI recaps (verbatim)

```
$ any generate PolicyNumber CoveragePeriod Deductible BonusMalus Claim Driver CarPolicy --entry-point any
  value  string  Any.String().NonEmpty()  to verify, unread guards
  start  DateOnly  Any.DateOnly()  to verify, unread guards
  end    DateOnly  Any.DateOnly()  to verify, unread guards
  amount  decimal  Any.Decimal().Between(0m, 5000m)  to verify, guard, unread guards
  coefficient  double  Any.Double()  to verify, unread guards
✗ Claim: several types answer to that name.
  Insurance.Domain.Claim
  System.Security.Claims.Claim
  Name the one you mean in full.
  fullName       string    Any.String().NotBlank()     guard
  birthDate      DateOnly  Any.DateOnly()
  yearsLicensed  int       Any.Int32().Between(0, 80)  guard
  number       PolicyNumber          new AnyPolicyNumber()        AnyX
  driver       Driver                new AnyDriver()              AnyX
  vehicle      VehicleCategory       Any.Enum<VehicleCategory>()
  level        CoverageLevel         Any.Enum<CoverageLevel>()
  period       CoveragePeriod        new AnyCoveragePeriod()      to verify, AnyX, unread guards
  deductible   Deductible            new AnyDeductible()          AnyX
  bonusMalus   BonusMalus            new AnyBonusMalus()          AnyX
  basePremium  decimal               Any.Decimal()                to verify, unread guards
  claims       IReadOnlyList<Claim>  Any.ListOf(new AnyClaim())   to verify, unread guards
✓ AnyCarPolicy.cs — 9 of 9 parameters inferred, 3 to verify.
exit=1

$ any generate Insurance.Domain.Claim --entry-point any
  id              Guid            Any.Guid().NonEmpty()       guard
  occurredOn      DateOnly        Any.DateOnly()
  damage          decimal         Any.Decimal().Positive()    guard
  responsibility  Responsibility  Any.Enum<Responsibility>()
✓ AnyClaim.cs — 4 of 4 parameters inferred.

$ any generate Responsibility
  constructor Responsibility()
✓ AnyResponsibility.cs — no constructor parameters to infer.     # emits `return new Responsibility();`
```

Also ran: `--dry-run` (CoveragePeriod), `--format json --dry-run` (Driver Deductible: counts and
`requiresVerification` match the rows), a re-run without `--force` (refused, clear), and
`--entry-point static:Arbitrary --force --dry-run`.

Verdict: every recap was honest. Every guard the tool did not fully read was flagged `to verify`, and
the emitted files failed to compile only on the deliberate `TODO_verify_*` lines. The tool read
`ThrowIfNegative`/`ThrowIfGreaterThan`/`ThrowIfNegativeOrZero`/`ThrowIfNullOrWhiteSpace`/`Guid.Empty`
correctly. It did not read the relational pattern `is <= 0m or > 20_000m` (new shape, not the `||` of
K2) or the private `Validate` method in the same type.

## Fixed by hand

* `AnyPolicyNumber`: `Any.StringMatching(@"POL-\d{4}-[A-Z]{2}\d{6}")`. This changes the base type from
  `AnyString` to `AnyPattern`, so the `With…(Func<AnyString, AnyString>)` signature had to change too.
* `AnyDeductible`: added `.WithScale(2)`. `AnyBonusMalus`: `Any.Double().Between(Floor, Ceiling)`.
* `AnyCarPolicy.basePremium`: `GreaterThan(0m).LessThanOrEqualTo(20_000m).WithScale(2)`.
* `AnyCoveragePeriod`: **rewritten**. `start < end <= start + 1y` ties two parameters together, but
  the scaffolded file draws each parameter on its own, from a static parameterless `AnyValid…()`.
  Changed it to `start` + `lengthInDays` (1..365), and `WithEnd` became `WithLengthInDays`.
* `AnyCarPolicy.claims`: the rule that claims must lie inside the period crosses parameters too.
  `Generate()` now draws the period first, then moves the drawn claims into it (pinned claims are
  left as they are).
* Harness: all 7 resolved generators pass 500 draws (`ScaffoldedGeneratorTests`).

## Findings

1. **bug / high, edge**: `Any.Decimal().GreaterThanOrEqualTo(7.9e27m).WithScale(2)` returns scale-1
   values such as `7900000000000000000001454003.8` where it should refuse. No decimal above about
   7.92e26 can carry 2 decimal places. `Between(MaxValue-1, MaxValue).WithScale(2)` returns scale 0.
   See `Probes.Bug_WithScale_2_near_decimal_MaxValue_returns_values_with_fewer_decimal_places`.
2. **bug / medium**: `Any.Decimal().Between(0m, 5000m).WithScale(2).Negative()` fails with
   "Cannot apply Negative() because it forbids every WithScale(2) value between 0 and 0." It does
   not name `Between(0, 5000)`. Without `WithScale` the message does name it. See
   `Probes.Bug_Negative_on_a_scaled_deductible_range_names_neither_Between_nor_the_real_conflict`.
3. **cli / medium**: `any generate <enum>` emits `return new Responsibility();`, which always draws
   the zero member. See `ScaffoldedGeneratorTests.Bug_Scaffolded_AnyResponsibility_draws_more_than_the_zero_member`.
4. **friction / medium**: the scaffolded shape cannot express cross-parameter invariants
   (date ranges, child dates within a parent period). The docs place every rule in a static
   `AnyValid…()`, which cannot see the other parameters.
5. **analyzer / medium**: JD007 and JD009 do not fire on a scaffolded generator, even when it is
   spelled `Any.Claim()` through the CLI's own `--entry-point any`. They do fire on `Any.Int32()…`.
6. **doc / medium**: the xUnit adapter page never says `[Reproducible]` lives in namespace
   `JustDummies.Xunit`. The first build failed with CS0246.
7. **cli / low**: `Claim` is ambiguous with `System.Security.Claims.Claim` from the shared framework,
   which a test project never means. The refusal is clear.
8. **cli / low**: guard shapes not read: a relational pattern `is <= 0m or > 20_000m`, and a private
   `Validate(...)` in the same type. Both were honestly flagged.
9. **missing-capability / low**: an overnight `TimeOnly` window (22:00 to 02:00) is refused as
   reversed bounds. The refusal is clear. `double` has no scale/step, so a two-decimal coefficient
   has to be drawn as a decimal and converted.
10. **analyzer / low**: nothing flags an `any…` value that reaches `Assert.Equal`
    (`AnalyzerProbes.DrawnValueInTheAssertion`), although the getting-started page names it as the
    one misuse.

Analyzers that fired correctly: JD014 (transposed `Between` on int and double: "look transposed",
`WithScale(29)`, an undeclared enum value, a count above 1,000,000), JD016 (4 distinct values out of
3), JD015 (`StartingWith("POL-").WithLength(3)`). JD028 is silent on
`Any.ListOf(Any.Claim()).Distinct()`, which its page documents.

Positives: conflict messages name both sides (double, DateOnly, scaffolded-base refinements, enum
`Except`, distinct cardinality). Regex refusals name the construct and its position. The
`Reproducibly` report gives a seed that replays a whole `CarPolicy`. The adapter scope and
`Reproducibly` agree for the same seed. The pool inspection gives a clean survivors/rejections split.

## Fluency: 4 / 5

Value-object tests read very well: `Any.CarPolicy().WithLevel(CoverageLevel.Comprehensive).WithDeductible(new Deductible(300m))`
says exactly what matters, and the lambda refinements (`WithDriver(d => d.WithYearsLicensed(y => y.GreaterThanOrEqualTo(3)))`)
are pleasant. I took one point off because the aggregate generators needed real hand-rewriting:
both cross-parameter rules of this domain (date range, claims within the period) fall outside the
scaffold's per-parameter model. The missing `using JustDummies.Xunit;` was another small papercut.
